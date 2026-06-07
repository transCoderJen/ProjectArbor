using System;
using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Misc;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.Managers
{    
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

        [SerializeField] private Camera OverlayCamera;

        [Header("Virtual Cameras")]
        [SerializeField] private List<VCamera> virtualCameras = new();

        [Header("Field Of View")]
        [SerializeField] private float fieldOfViewLerpSpeed = 5f;
        [SerializeField] private float fieldOfViewScrollSensitivity = 0.05f;

        [Header("Follow Offset")]
        [SerializeField] private float followOffsetScrollSpeed = 1f;
        [SerializeField] private float followOffsetLerpSpeed = 5f;
        [SerializeField] private float followOffsetSwitchThreshold = 0.05f;
        [SerializeField] private float maxResetWaitTime = 1.5f;

        [Header("Free Look Movement")]
        [SerializeField] private float freeLookPanSpeed = 20f;

        [Header("Distance Culling")]
        [Tooltip("The distance at which objects on the Tree layer stop rendering")]
        [SerializeField] private float cullDistance = 60f;
        [SerializeField] private LayerMask cullLayers;

        private VCamera currentVCamera;
        private float targetFieldOfView;
        private Vector3 targetFollowOffset;

        private Coroutine cameraTransitionCoroutine;
        private bool isTransitioning;

        /// <summary>
        /// Uses unscaled time so camera movement still works while the game is paused.
        /// </summary>
        private float CameraDeltaTime => Time.unscaledDeltaTime;

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

        private void Start()
        {
            SyncOverlayCamera();
            SetupDistanceCulling();
        }

        private void Update()
        {

            // if (CurrentVirtualCamera == null || !Player.Instance.ControlsEnabled)
            //     return;

            if (CurrentVirtualCamera == null)
                return;

            if (!isTransitioning)
            {
                HandleScrollInput();
                HandleFreeLookMovement();
            }

            UpdateFieldOfView();
            UpdateFollowOffset();
        }

        private void LateUpdate()
        {
            if (CurrentVirtualCamera == null)
                return;
        }

        private void UpdateFollowOffset()
        {
            CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();
            if (follow == null)
                return;

            Vector3 offset = follow.FollowOffset;

            offset = Vector3.Lerp(
                offset,
                targetFollowOffset,
                Time.unscaledDeltaTime * followOffsetLerpSpeed
            );

            if (Vector3.Distance(offset, targetFollowOffset) <= 0.01f)
            {
                offset = targetFollowOffset;
            }

            follow.FollowOffset = offset;
        }



        private void SetupDistanceCulling()
        {
            if (currentCamera == null)
                return;

            float[] distances = new float[32];

            for (int i = 0; i < 32; i++)
            {
                if ((cullLayers.value & (1 << i)) != 0)
                {
                    distances[i] = cullDistance;
                }
                else
                {
                    distances[i] = 0f;
                }
            }

            currentCamera.layerCullDistances = distances;
            currentCamera.layerCullSpherical = true;
        }

        /// <summary>
        /// Switches to a virtual camera by type immediately.
        /// </summary>
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
        /// Smoothly resets the current camera offset, then switches to the target camera.
        /// </summary>
        public void ResetOffsetsAndSwitchCamera(VirtualCameraType cameraType)
        {
            VCamera nextCameraEntry = GetCameraEntry(cameraType);

            if (nextCameraEntry == null || nextCameraEntry.VirtualCamera == null)
            {
                Debug.LogWarning($"CameraManager: No camera entry found for {cameraType}.");
                return;
            }

            if (CurrentVirtualCamera == null)
            {
                SetActiveCamera(nextCameraEntry);
                return;
            }

            if (cameraTransitionCoroutine != null)
                StopCoroutine(cameraTransitionCoroutine);

            cameraTransitionCoroutine = StartCoroutine(ResetOffsetsAndSwitchCameraCoroutine(nextCameraEntry));
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

                follow.FollowOffset = targetFollowOffset;
            }
        }

        /// <summary>
        /// Adjusts the current camera's target field of view within its configured range.
        /// Smaller FOV means more zoomed in.
        /// </summary>
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
        public void ChangeFreeLookOffsetXZ(Vector2 movement)
        {
            if (currentVCamera == null || CurrentVirtualCamera == null)
                return;

            if (currentVCamera.CameraType != VirtualCameraType.FreeLook)
                return;

            Vector3 moveDelta = new Vector3(
                movement.x,
                0f,
                movement.y) * (freeLookPanSpeed * CameraDeltaTime);

            targetFollowOffset += moveDelta;
        }

        /// <summary>
        /// Sets the follow target for a specific virtual camera.
        /// </summary>
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

        /// <summary>
        /// Smoothly resets the current camera's X and Z follow offset.
        /// </summary>
        public void ResetOffsets()
        {
            if (CurrentVirtualCamera == null)
                return;

            CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();
            if (follow == null)
                return;

            targetFollowOffset = new Vector3(0f, follow.FollowOffset.y, -30f);
        }

        private IEnumerator ResetOffsetsAndSwitchCameraCoroutine(VCamera nextCameraEntry)
        {
            isTransitioning = true;

            ResetOffsets();

            CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();

            if (follow != null)
            {
                float elapsedTime = 0f;

                while (Vector3.Distance(follow.FollowOffset, targetFollowOffset) > followOffsetSwitchThreshold)
                {
                    elapsedTime += CameraDeltaTime;

                    if (elapsedTime >= maxResetWaitTime)
                        break;

                    yield return null;
                }
            }

            SetActiveCamera(nextCameraEntry);

            isTransitioning = false;
            cameraTransitionCoroutine = null;
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
                CameraDeltaTime * fieldOfViewLerpSpeed);

            CurrentVirtualCamera.Lens = lens;
        }

        // private void UpdateFollowOffset()
        // {
        //     CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();
        //     if (follow == null)
        //         return;

        //     Vector3 offset = follow.FollowOffset;
        //     offset.x = Mathf.Lerp(offset.x, targetFollowOffset.x, CameraDeltaTime * followOffsetLerpSpeed);
        //     offset.y = Mathf.Lerp(offset.y, targetFollowOffset.y, CameraDeltaTime * followOffsetLerpSpeed);
        //     offset.z = Mathf.Lerp(offset.z, targetFollowOffset.z, CameraDeltaTime * followOffsetLerpSpeed);
        //     follow.FollowOffset = offset;
        // }

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

            if (Application.isPlaying)
                SetupDistanceCulling();
        }

        [ContextMenu("SyncOverlayCamera")]
        private void SyncOverlayCamera()
        {
            if (OverlayCamera == null || currentCamera == null)
                return;

            OverlayCamera.transform.position = currentCamera.transform.position;
            OverlayCamera.transform.rotation = currentCamera.transform.rotation;

            OverlayCamera.fieldOfView = currentCamera.fieldOfView;
            OverlayCamera.nearClipPlane = currentCamera.nearClipPlane;
            OverlayCamera.farClipPlane = currentCamera.farClipPlane;
            OverlayCamera.aspect = currentCamera.aspect;
        }
    }
}