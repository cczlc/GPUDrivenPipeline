using UnityEngine;

[System.Serializable]
public class InstanceSetting
{ 
    [SerializeField]
    public ComputeShader instanceCull = null;

    [SerializeField]
    public InstanceData[] instanceDatas;
}
