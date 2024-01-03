using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class ClusterGenerator
{
    [DllImport(@"ClusterGen.dll", EntryPoint = "ClusterGen", CallingConvention = CallingConvention.Cdecl)]
    extern unsafe static bool ClusterGen(float* vertexBuffer, int vertexCount, int* indexBuffer, int indexCount, Bound3f bound, string desc, ref ClusterData* clusterDatas, ref int ClusterCount);
    [DllImport(@"ClusterGen.dll", EntryPoint = "ClusterDestory", CallingConvention = CallingConvention.Cdecl)]
    extern unsafe static bool ClusterDestory();

    struct Bound3f
    {
        public Vector3 Min;
        public Vector3 Max;
    }

    unsafe struct ClusterData
    {
        public IntPtr vertices;
        public IntPtr indices;
        public int vertexCount;     // 顶点数
        public int indexCount;      // 顶点索引数
    }

    struct VertexAttr
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 uv0;
    }

    public unsafe static void ClusterGenerate(ref Mesh instanceMesh, ref Mesh clusterMergeMesh)
    {
        instanceMesh.MarkDynamic();
        string attrDescsStr = "";
        VertexAttributeDescriptor[] attrDescs = instanceMesh.GetVertexAttributes();
        int numOfFloatPerVertex = 0;

        // todo: change this!!!
        for (int i = 0; i < attrDescs.Length; i++)
        {
            if (attrDescs[i].attribute == VertexAttribute.Position)
            {
                attrDescsStr += "P3";
                numOfFloatPerVertex += 3;
            }
            else if (attrDescs[i].attribute == VertexAttribute.Normal)
            {
                attrDescsStr += "N3";
                numOfFloatPerVertex += 3;
            }
            else if (attrDescs[i].attribute == VertexAttribute.Tangent)
            {
                attrDescsStr += "T4";
                numOfFloatPerVertex += 4;
            }
            else if (attrDescs[i].attribute == VertexAttribute.TexCoord0)
            {
                attrDescsStr += "UV0";
                numOfFloatPerVertex += 2;
            }
        }

        // WriteVertex(ref instanceMesh);

        // 生成mesh包围盒数据
        Bound3f bound = new Bound3f
        {
            Min = instanceMesh.bounds.min,
            Max = instanceMesh.bounds.max
        };

        // 生成顶点缓冲数组
        float[] vertexBuffer = new float[instanceMesh.vertexCount * numOfFloatPerVertex];
        VertexBufferGen(ref instanceMesh, ref vertexBuffer, numOfFloatPerVertex);

        // 生成返回的cluster数据 
        ClusterData* clusterDatas = null;
        int clusterCount = 0;

        // 调用c++ dll
        fixed (int* indexBufferToCpp = instanceMesh.GetIndices(0))
        {
            fixed (float* vertexBufferToCpp = vertexBuffer)
            {
                // todo：indexbuffer format is uint16
                ClusterGen(vertexBufferToCpp, instanceMesh.vertexCount, indexBufferToCpp, (int)instanceMesh.GetIndexCount(0), bound, attrDescsStr, ref clusterDatas, ref clusterCount);
            }
        }

        Mesh[] clusterMesh = new Mesh[clusterCount];    // 存放cluster生成的mesh数据

        // 根据cluster数据构建mesh
        BuildMesh(ref clusterDatas, ref clusterCount, ref clusterMesh, attrDescs);

        // 将clusterMesh进行合并
        MergeMesh(ref clusterMesh, ref clusterMergeMesh);
    }

    public static void MergeMesh(ref Mesh[] clusterMesh, ref Mesh clusterMergeMesh)
    {
        CombineInstance[] combineInstances = new CombineInstance[clusterMesh.Length];
        for (int i = 0; i < clusterMesh.Length; ++i)
        {
            combineInstances[i].mesh = clusterMesh[i];                  // 对于cluster Mesh进行合并
            combineInstances[i].transform = Matrix4x4.identity;         // 使用局部坐标系
        }
        clusterMergeMesh.CombineMeshes(combineInstances);               // 得到合并之后的mesh

        Debug.Log("MergeMesh");
    }

    public unsafe static void ClusterClear()
    {
        ClusterDestory();
    }

    public static void VertexBufferGen(ref Mesh instanceMesh, ref float[] vertexBuffer, int numOfFloatPerVertex)
    {
        // todo
        for (int i = 0; i < instanceMesh.vertexCount; i++)
        {
            // Position3D
            vertexBuffer[i * numOfFloatPerVertex + 0] = instanceMesh.vertices[i][0];
            vertexBuffer[i * numOfFloatPerVertex + 1] = instanceMesh.vertices[i][1];
            vertexBuffer[i * numOfFloatPerVertex + 2] = instanceMesh.vertices[i][2];

            // Normal3D
            vertexBuffer[i * numOfFloatPerVertex + 3] = instanceMesh.normals[i][0];
            vertexBuffer[i * numOfFloatPerVertex + 4] = instanceMesh.normals[i][1];
            vertexBuffer[i * numOfFloatPerVertex + 5] = instanceMesh.normals[i][2];

            // Tangent3D
            vertexBuffer[i * numOfFloatPerVertex + 6] = instanceMesh.tangents[i][0];
            vertexBuffer[i * numOfFloatPerVertex + 7] = instanceMesh.tangents[i][1];
            vertexBuffer[i * numOfFloatPerVertex + 8] = instanceMesh.tangents[i][2];
            vertexBuffer[i * numOfFloatPerVertex + 9] = instanceMesh.tangents[i][3];

            // UV2D
            vertexBuffer[i * numOfFloatPerVertex + 10] = instanceMesh.tangents[i][0];
            vertexBuffer[i * numOfFloatPerVertex + 11] = instanceMesh.tangents[i][1];
        }
    }

    unsafe static void BuildMesh(ref ClusterData* clusterDatas, ref int clusterCount, ref Mesh[] clusterMesh, VertexAttributeDescriptor[] attrDescs)
    {
        // 为每个cluster生成mesh
        for (int i = 0; i < clusterCount; ++i)
        {
            clusterMesh[i] = new Mesh();
            IntPtr indexBuffer = clusterDatas[i].indices;
            int vertexCount = clusterDatas[i].vertexCount;
            int indexCount = clusterDatas[i].indexCount;

            // todo
            int vertexDataLength = vertexCount * 12;

            float[] vertexManaged = new float[vertexDataLength];
            int[] indexManaged = new int[indexCount];

            Marshal.Copy(clusterDatas[i].vertices, vertexManaged, 0, vertexDataLength);
            Marshal.Copy(clusterDatas[i].indices, indexManaged, 0, indexCount);

            clusterMesh[i].name = "Cluster" + i;

            clusterMesh[i].SetVertexBufferParams(vertexCount, attrDescs);
            clusterMesh[i].SetVertexBufferData(vertexManaged, 0, 0, vertexDataLength);
            clusterMesh[i].SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            clusterMesh[i].SetIndexBufferData(indexManaged, 0, 0, indexCount);


            // 为每个顶点设置颜色
            Color colorData = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), 1.0f);
            Color[] colorDatas = new Color[vertexCount];
            for (int j = 0; j < vertexCount; ++j)
            {
                colorDatas[j] = colorData;
            }

            clusterMesh[i].SetColors(colorDatas);

            var subMesh = new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles);
            clusterMesh[i].SetSubMesh(0, subMesh);
        }
    }

    public unsafe static void WriteVertex(ref Mesh instanceMesh)
    {
        //string path = "tangents.txt";
        //if (!File.Exists(path))
        //{
        //    File.Create(path).Dispose();
        //}
        ////UTF-8方式保存
        //using (StreamWriter stream = new StreamWriter(path, false, Encoding.UTF8))
        //{
        //    for (int i = 0; i < instanceMesh.vertexCount; i++)
        //    {
        //        string lineStr = "";
        //        lineStr += instanceMesh.tangents[i][0].ToString() + " " + instanceMesh.tangents[i][1].ToString() + " " + instanceMesh.tangents[i][2].ToString() + " " + instanceMesh.tangents[i][3].ToString();
        //        stream.WriteLine(lineStr);
        //    }
        //}


        //string path2 = "indices.txt";
        //if (!File.Exists(path2))
        //{
        //    File.Create(path2).Dispose();
        //}
        ////UTF-8方式保存
        //using (StreamWriter stream = new StreamWriter(path2, false, Encoding.UTF8))
        //{
        //    for (int i = 0; i < instanceMesh.GetIndexCount(0); i++)
        //    {
        //        string lineStr = "";
        //        lineStr += indexBuffer[i].ToString();
        //        stream.WriteLine(lineStr);
        //    }
        //}

    }

    public unsafe static void WriteVertex(float* vertexBuffer, int vertexCount)
    {
        string path = "verticesGet.txt";
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        //UTF-8方式保存
        using (StreamWriter stream = new StreamWriter(path, false, Encoding.UTF8))
        {
            for (int i = 0; i < vertexCount; i++)
            {
                string lineStr = "";
                lineStr += vertexBuffer[i * 12 + 0].ToString() + " " + vertexBuffer[i * 12 + 1].ToString() + " " + vertexBuffer[i * 12 + 2].ToString();
                stream.WriteLine(lineStr);
            }
        }
    }

    public unsafe static void WriteIndex(UInt32* indexBuffer, int indexCount)
    {
        string path = "indicesGet.txt";
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
        //UTF-8方式保存
        using (StreamWriter stream = new StreamWriter(path, false, Encoding.UTF8))
        {
            for (int i = 0; i < indexCount; i++)
            {
                string lineStr = "";
                lineStr += indexBuffer[i].ToString();
                stream.WriteLine(lineStr);
            }
        }
    }
}
