using UnityEngine;

public class ClusterManagerForScene
{
    public void ClusterGenerateForScene()
    {
        // ȡ�ó����е�������Ҫ����cluster���ֵ�����
        MeshFilter[] meshFilters = GameObject.FindObjectsOfType<MeshFilter>();

        for(int i = 0; i < meshFilters.Length; ++i)
        {
            Mesh originMesh = meshFilters[i].sharedMesh;
            Mesh clusterMergeMesh = new Mesh();
            // originMesh��ԭʼmesh   clusterMergeMesh��cluster�ϲ�֮���mesh
            ClusterGenerator.ClusterGenerate(ref originMesh, ref clusterMergeMesh);
            meshFilters[i].sharedMesh = clusterMergeMesh;
        }
    }
}
