using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages multiple Cinemachine virtual cameras, including per-camera
/// field of view and follow offset settings.
/// </summary>
public class CameraManager : Singleton<CameraManager>
{
    public enum VirtualCameraType
    {
        Player,
        Secondary,
        Combat,
        Dialogue,
        FreeLook
    }

    [Serializable]
    public class VCamera
    {
        [SerializeField] private VirtualCameraType cameraType;
        public VirtualCameraType CameraType => cameraType;

        [SerializeField] private CinemachineCamera virtualCamera;
        public CinemachineCamera VirtualCamera => virtualCamera;

        [Header("Field Of View")]
        [SerializeField] private float minFieldOfView = 25f;
        public float MinFieldOfView => minFieldOfView;

        [SerializeField] private float maxFieldOfView = 70f;
        public float MaxFieldOfView => maxFieldOfView;

        [Header("Follow Offset")]
        [SerializeField] private float minFollowOffsetY = 50f;
        public float MinFollowOffsetY => minFollowOffsetY;

        [SerializeField] private float maxFollowOffsetY = 100f;
        public float MaxFollowOffsetY => maxFollowOffsetY;

        /// <summary>
        /// Ensures serialized values remain valid.
        /// </summary>
        public void Validate()
        {
            if (maxFieldOfView < minFieldOfView)
                maxFieldOfView = minFieldOfView;

            if (maxFollowOffsetY < minFollowOffsetY)
                maxFollowOffsetY = minFollowOffsetY;
        }
    }

    private const int ActivePriority = 20;
    private const int InactivePriority = 0;

    [Header("Main Camera")]
    [SerializeField] private Camera currentCamera;
    public Camera CurrentCamera => currentCamera;

    [Header("Virtual Cameras")]
    [SerializeField] private List<VCamera> virtualCameras = new();

    [Header("Field Of View")]
    [SerializeField] private float fieldOfViewLerpSpeed = 5f;
    [SerializeField] private float fieldOfViewScrollSensitivity = 0.05f;

    [Header("Follow Offset")]
    [SerializeField] private float followOffsetScrollSpeed = 1f;
    [SerializeField] private float followOffsetLerpSpeed = 5f;

    [Header("Free Look Movement")]
    [SerializeField] private float freeLookPanSpeed = 20f;

    private VCamera currentVCamera;
    private float targetFieldOfView;
    private Vector3 targetFollowOffset;

    /// <summary>
    /// The currently active camera entry.
    /// </summary>
    public VCamera CurrentVCamera => currentVCamera;

    /// <summary>
    /// The currently active Cinemachine virtual camera.
    /// </summary>
    public CinemachineCamera CurrentVirtualCamera => currentVCamera?.VirtualCamera;

    protected override void Awake()
    {
        base.Awake();
        InitializeActiveCamera();
    }

    private void Update()
    {
        if (CurrentVirtualCamera == null)
            return;

        HandleScrollInput();
        HandleFreeLookMovement();
        UpdateFieldOfView();
        UpdateFollowOffset();
    }

    /// <summary>
    /// Switches to a virtual camera by type.
    /// </summary>
    /// <param name="cameraType">The camera type to activate.</param>
    public void SwitchCamera(VirtualCameraType cameraType)
    {
        VCamera cameraEntry = GetCameraEntry(cameraType);

        if (cameraEntry == null)
        {
            Debug.LogWarning($"CameraManager: No camera entry found for {cameraType}.");
            return;
        }

        SetActiveCamera(cameraEntry);
    }

    /// <summary>
    /// Cycles to the next configured virtual camera.
    /// </summary>
    public void CycleCamera()
    {
        if (virtualCameras == null || virtualCameras.Count == 0)
            return;

        int currentIndex = virtualCameras.IndexOf(currentVCamera);
        int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % virtualCameras.Count;

        SetActiveCamera(virtualCameras[nextIndex]);
    }

    /// <summary>
    /// Sets the active virtual camera entry.
    /// </summary>
    /// <param name="cameraEntry">The camera entry to activate.</param>
    public void SetActiveCamera(VCamera cameraEntry)
    {
        if (cameraEntry == null || cameraEntry.VirtualCamera == null)
        {
            Debug.LogWarning("CameraManager: Tried to activate a null camera entry.");
            return;
        }

        foreach (VCamera entry in virtualCameras)
        {
            if (entry == null || entry.VirtualCamera == null)
                continue;

            entry.VirtualCamera.Priority = entry == cameraEntry ? ActivePriority : InactivePriority;
        }

        currentVCamera = cameraEntry;

        LensSettings lens = CurrentVirtualCamera.Lens;
        targetFieldOfView = Mathf.Clamp(
            lens.FieldOfView,
            currentVCamera.MinFieldOfView,
            currentVCamera.MaxFieldOfView);

        CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();
        if (follow != null)
        {
            targetFollowOffset = follow.FollowOffset;
            targetFollowOffset.y = Mathf.Clamp(
                targetFollowOffset.y,
                currentVCamera.MinFollowOffsetY,
                currentVCamera.MaxFollowOffsetY);
        }
    }

    /// <summary>
    /// Adjusts the current camera's target field of view within its configured range.
    /// Smaller FOV means more zoomed in.
    /// </summary>
    /// <param name="sizeChange">The amount to add to the target field of view.</param>
    public void ChangeFieldOfView(float sizeChange)
    {
        if (currentVCamera == null || CurrentVirtualCamera == null)
            return;

        targetFieldOfView = Mathf.Clamp(
            targetFieldOfView + sizeChange,
            currentVCamera.MinFieldOfView,
            currentVCamera.MaxFieldOfView);
    }

    /// <summary>
    /// Adjusts the current camera's target follow offset Y within its configured range.
    /// </summary>
    /// <param name="scrollInput">The scroll input amount.</param>
    public void ChangeFollowOffsetY(float scrollInput)
    {
        if (currentVCamera == null || CurrentVirtualCamera == null)
            return;

        targetFollowOffset.y = Mathf.Clamp(
            targetFollowOffset.y + (scrollInput * followOffsetScrollSpeed),
            currentVCamera.MinFollowOffsetY,
            currentVCamera.MaxFollowOffsetY);
    }

    /// <summary>
    /// Adjusts the current camera's target follow offset X and Z.
    /// Intended for the FreeLook camera. Bounds are handled externally.
    /// </summary>
    /// <param name="movement">Input movement on X and Z axes.</param>
    public void ChangeFreeLookOffsetXZ(Vector2 movement)
    {
        if (currentVCamera == null || CurrentVirtualCamera == null)
            return;

        if (currentVCamera.CameraType != VirtualCameraType.FreeLook)
            return;

        Vector3 moveDelta = new Vector3(
            movement.x,
            0f,
            movement.y) * (freeLookPanSpeed * Time.deltaTime);

        targetFollowOffset += moveDelta;
    }

    /// <summary>
    /// Sets the follow target for a specific virtual camera.
    /// </summary>
    /// <param name="cameraType">The target camera type.</param>
    /// <param name="target">The transform to follow.</param>
    public void SetCameraFollow(VirtualCameraType cameraType, Transform target)
    {
        VCamera cameraEntry = GetCameraEntry(cameraType);

        if (cameraEntry == null || cameraEntry.VirtualCamera == null)
        {
            Debug.LogWarning($"CameraManager: No camera entry found for {cameraType}.");
            return;
        }

        cameraEntry.VirtualCamera.Follow = target;
    }

    /// <summary>
    /// Sets the look at target for a specific virtual camera.
    /// </summary>
    /// <param name="cameraType">The target camera type.</param>
    /// <param name="target">The transform to look at.</param>
    public void SetCameraLookAt(VirtualCameraType cameraType, Transform target)
    {
        VCamera cameraEntry = GetCameraEntry(cameraType);

        if (cameraEntry == null || cameraEntry.VirtualCamera == null)
        {
            Debug.LogWarning($"CameraManager: No camera entry found for {cameraType}.");
            return;
        }

        cameraEntry.VirtualCamera.LookAt = target;
    }

    public void ResetOffsets()
    {
        CinemachineFollow offset = CurrentVirtualCamera.GetComponent<CinemachineFollow>();

        offset.FollowOffset = new Vector3 (0, offset.FollowOffset.y, -30);
    }
    
    private void HandleScrollInput()
    {
        if (Mouse.current == null)
            return;

        float scrollY = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Approximately(scrollY, 0f))
            return;

        bool isShiftHeld =
            Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        if (isShiftHeld)
        {
            ChangeFieldOfView(-scrollY * fieldOfViewScrollSensitivity);
        }
        else
        {
            ChangeFollowOffsetY(scrollY);
        }
    }

    private void HandleFreeLookMovement()
    {
        if (currentVCamera == null || currentVCamera.CameraType != VirtualCameraType.FreeLook)
            return;

        if (Keyboard.current == null)
            return;

        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current.aKey.isPressed)
            moveInput.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            moveInput.x += 1f;

        if (Keyboard.current.sKey.isPressed)
            moveInput.y -= 1f;

        if (Keyboard.current.wKey.isPressed)
            moveInput.y += 1f;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        if (moveInput == Vector2.zero)
            return;

        ChangeFreeLookOffsetXZ(moveInput);
    }

    private void UpdateFieldOfView()
    {
        LensSettings lens = CurrentVirtualCamera.Lens;
        lens.FieldOfView = Mathf.Lerp(
            lens.FieldOfView,
            targetFieldOfView,
            Time.deltaTime * fieldOfViewLerpSpeed);

        CurrentVirtualCamera.Lens = lens;
    }

    private void UpdateFollowOffset()
    {
        CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();
        if (follow == null)
            return;

        Vector3 offset = follow.FollowOffset;
        offset.x = Mathf.Lerp(offset.x, targetFollowOffset.x, Time.deltaTime * followOffsetLerpSpeed);
        offset.y = Mathf.Lerp(offset.y, targetFollowOffset.y, Time.deltaTime * followOffsetLerpSpeed);
        offset.z = Mathf.Lerp(offset.z, targetFollowOffset.z, Time.deltaTime * followOffsetLerpSpeed);
        follow.FollowOffset = offset;
    }

    private VCamera GetCameraEntry(VirtualCameraType cameraType)
    {
        foreach (VCamera entry in virtualCameras)
        {
            if (entry == null)
                continue;

            if (entry.CameraType == cameraType)
                return entry;
        }

        return null;
    }

    private void InitializeActiveCamera()
    {
        if (virtualCameras == null || virtualCameras.Count == 0)
        {
            Debug.LogWarning("CameraManager: No virtual cameras assigned.");
            return;
        }

        VCamera bestEntry = null;

        foreach (VCamera entry in virtualCameras)
        {
            if (entry == null || entry.VirtualCamera == null)
                continue;

            if (bestEntry == null || entry.VirtualCamera.Priority > bestEntry.VirtualCamera.Priority)
                bestEntry = entry;
        }

        if (bestEntry != null)
            SetActiveCamera(bestEntry);
    }

    private void OnValidate()
    {
        if (virtualCameras == null)
            return;

        foreach (VCamera entry in virtualCameras)
        {
            if (entry == null)
                continue;

            entry.Validate();
        }
    }
}