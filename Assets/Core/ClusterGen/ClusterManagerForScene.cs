using UnityEngine;

public class ClusterManagerForScene
{
    public void ClusterGenerateForScene()
    {
        // 取得场景中的所有需要进行cluster划分的物体
        MeshFilter[] meshFilters = GameObject.FindObjectsOfType<MeshFilter>();

        for(int i = 0; i < meshFilters.Length; ++i)
        {
            Mesh originMesh = meshFilters[i].sharedMesh;
            Mesh clusterMergeMesh = new Mesh();
            // originMesh：原始mesh   clusterMergeMesh：cluster合并之后的mesh
            ClusterGenerator.ClusterGenerate(ref originMesh, ref clusterMergeMesh);
            meshFilters[i].sharedMesh = clusterMergeMesh;
        }
    }
}
