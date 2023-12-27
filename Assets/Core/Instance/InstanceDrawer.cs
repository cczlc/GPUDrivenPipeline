using UnityEngine;
using UnityEngine.Rendering;

public class InstanceDrawer
{
    ComputeShader instanceCull;

    public InstanceDrawer(ComputeShader instanceCull)
    {
        this.instanceCull = instanceCull;
    }


    // 如果 GPU buffer 未被创建，那么创建它
    public void CheckAndInit(InstanceData idata)
    {
        if (idata.matrixBuffer != null && idata.validMatrixBuffer != null && idata.argsBuffer != null) return;

        int sizeofMatrix4x4 = 4 * 4 * 4;
        idata.matrixBuffer = new ComputeBuffer(idata.instanceCount, sizeofMatrix4x4);
        idata.validMatrixBuffer = new ComputeBuffer(idata.instanceCount, sizeofMatrix4x4, ComputeBufferType.Append);
        idata.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

        // 传变换矩阵到 GPU
        idata.matrixBuffer.SetData(idata.mats);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 }; // 绘制参数
        if (idata.instanceMesh != null)
        {
            args[0] = (uint)idata.instanceMesh.GetIndexCount(idata.subMeshIndex);
            args[1] = (uint)0;
            args[2] = (uint)idata.instanceMesh.GetIndexStart(idata.subMeshIndex);
            args[3] = (uint)idata.instanceMesh.GetBaseVertex(idata.subMeshIndex);
        }
        idata.argsBuffer.SetData(args);
    }

    // 不做任何视锥剔除和遮挡剔除
    public void Draw(InstanceData idata, ref CommandBuffer buffer)
    {
        if (idata == null) return;
        CheckAndInit(idata);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        idata.argsBuffer.GetData(args);
        args[1] = (uint)idata.instanceCount;
        idata.argsBuffer.SetData(args);

        idata.instanceMaterial.SetBuffer("_validMatrixBuffer", idata.matrixBuffer);

        buffer.DrawMeshInstancedIndirect(idata.instanceMesh, idata.subMeshIndex, idata.instanceMaterial, -1, idata.argsBuffer);
    }

    // Todo：目前只支持main camera的剔除
    public void Draw(InstanceData idata, Camera camera, ref RenderTexture hizBuffer,ref CommandBuffer buffer)
    {
        if (idata == null) return;
        CheckAndInit(idata);

        // 清空绘制计数
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        idata.argsBuffer.GetData(args);
        args[1] = 0;
        idata.argsBuffer.SetData(args);
        idata.validMatrixBuffer.SetCounterValue(0);

        // 计算视锥体6个平面
        Plane[] ps = GeometryUtility.CalculateFrustumPlanes(camera);
        Vector4[] planes = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            // Ax+By+Cz+D --> Vec4(A,B,C,D)
            planes[i] = new Vector4(ps[i].normal.x, ps[i].normal.y, ps[i].normal.z, ps[i].distance);
        }

        // 计算 bounding box
        Vector4[] bounds = BoundToPoint(idata.instanceMesh.bounds);

        // 传参
        int kernelID = instanceCull.FindKernel("InstanceCull");
        instanceCull.SetBool("_isOpenGL", camera.projectionMatrix.Equals(GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
        instanceCull.SetInt("_instanceCount", idata.instanceCount);
        instanceCull.SetInt("_depthTextureSize", hizBuffer.width);
        instanceCull.SetVectorArray("_planes", planes);
        instanceCull.SetVectorArray("_bounds", bounds);
        instanceCull.SetMatrix("_vpMatrix", GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix);
        instanceCull.SetTexture(kernelID, "_hizTexture", hizBuffer);
        instanceCull.SetBuffer(kernelID, "_instanceMatrixBuffer", idata.matrixBuffer);
        instanceCull.SetBuffer(kernelID, "_cullResultBuffer", idata.validMatrixBuffer);
        instanceCull.SetBuffer(kernelID, "_argsBuffer", idata.argsBuffer);
        instanceCull.Dispatch(kernelID, 1 + (idata.instanceCount - 1) / 640, 1, 1);

        idata.argsBuffer.GetData(args);

        idata.instanceMaterial.SetBuffer("_validMatrixBuffer", idata.validMatrixBuffer);

        buffer.DrawMeshInstancedIndirect(idata.instanceMesh, idata.subMeshIndex, idata.instanceMaterial, -1, idata.argsBuffer);
    }

    // 计算包围盒的八个顶点
    static Vector4[] BoundToPoint(Bounds b)
    {
        Vector4[] boundingBox = new Vector4[8];
        boundingBox[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
        boundingBox[1] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
        boundingBox[2] = new Vector4(boundingBox[0].x, boundingBox[0].y, boundingBox[1].z, 1);
        boundingBox[3] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[0].z, 1);
        boundingBox[4] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[0].z, 1);
        boundingBox[5] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[1].z, 1);
        boundingBox[6] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[1].z, 1);
        boundingBox[7] = new Vector4(boundingBox[1].x, boundingBox[1].y, boundingBox[0].z, 1);
        return boundingBox;
    }

}