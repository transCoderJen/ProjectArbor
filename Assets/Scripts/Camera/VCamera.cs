using Unity.Cinemachine;
using UnityEngine;

[System.Serializable]
public class VCamera
{
    public CameraManager.VirtualCameraType CameraType;
    public CinemachineCamera VirtualCamera;

    [Header("Zoom")]
    public float MinFieldOfView = 6f;
    public float MaxFieldOfView = 12f;

    [Header("Follow Offset")]
    public float MinFollowOffsetY = 50f;
    public float MaxFollowOffsetY = 100f;
}