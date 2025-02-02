using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance;
    [SerializeField] private float _globalShakeForce = 1f;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }

    public void ShakeCamera(CinemachineImpulseSource impulseSource)
    {
        impulseSource.GenerateImpulseWithForce(_globalShakeForce);
    }
}
