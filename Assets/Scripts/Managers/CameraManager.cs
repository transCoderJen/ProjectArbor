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

        [Header("Camera Switching")]
        [SerializeField] private float cameraSwitchLockDuration = 0.5f;

        [Header("Field Of View")]
        [SerializeField] private float fieldOfViewLerpSpeed = 5f;
        [SerializeField] private float fieldOfViewScrollSensitivity = 0.05f;

        [Header("Follow Offset")]
        [SerializeField] private float followOffsetScrollSpeed = 1f;
        [SerializeField] private float followOffsetLerpSpeed = 5f;
        [SerializeField] private float followOffsetSwitchThreshold = 0.05f;
        [SerializeField] private float maxResetWaitTime = 1.5f;

        [Header("Camera Angle")]
        [SerializeField] private float followOffsetX = 0f;

        [Header("Free Look Movement")]
        [SerializeField] private Transform freeLookTarget;
        [SerializeField] private float freeLookPanSpeed = 20f;

        [SerializeField] private bool enableMouseRegionPanning = true;
        [SerializeField] private float mouseRegionPanSize = 25f;
        

        [Header("Distance Culling")]
        [SerializeField] private float cullDistance = 60f;
        [SerializeField] private LayerMask cullLayers;

        private VCamera currentVCamera;
        private float targetFieldOfView;
        private Vector3 targetFollowOffset;

        private Coroutine cameraTransitionCoroutine;
        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        private float CameraDeltaTime => Time.unscaledDeltaTime;

        public VCamera CurrentVCamera => currentVCamera;
        public CinemachineCamera CurrentVirtualCamera => currentVCamera?.VirtualCamera;

        protected override void Awake()
        {
            base.Awake();
            CreateFreeLookTargetIfNeeded();
            InitializeActiveCamera();
        }

        private void Start()
        {
            SyncOverlayCamera();
            SetupDistanceCulling();
        }

        private void Update()
        {
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

        public void SwitchCamera(VirtualCameraType cameraType)
        {
            if (isTransitioning)
                return;

            VCamera cameraEntry = GetCameraEntry(cameraType);

            if (cameraEntry == null)
            {
                Debug.LogWarning($"CameraManager: No camera entry found for {cameraType}.");
                return;
            }

            if (cameraTransitionCoroutine != null)
                StopCoroutine(cameraTransitionCoroutine);

            cameraTransitionCoroutine = StartCoroutine(SwitchCameraCoroutine(cameraEntry));
        }

        public void ResetOffsetsAndSwitchCamera(VirtualCameraType cameraType)
        {
            if (isTransitioning)
                return;

            VCamera nextCameraEntry = GetCameraEntry(cameraType);

            if (nextCameraEntry == null || nextCameraEntry.VirtualCamera == null)
            {
                Debug.LogWarning($"CameraManager: No camera entry found for {cameraType}.");
                return;
            }

            if (CurrentVirtualCamera == null)
            {
                if (cameraTransitionCoroutine != null)
                    StopCoroutine(cameraTransitionCoroutine);

                cameraTransitionCoroutine = StartCoroutine(SwitchCameraCoroutine(nextCameraEntry));
                return;
            }

            if (cameraTransitionCoroutine != null)
                StopCoroutine(cameraTransitionCoroutine);

            cameraTransitionCoroutine = StartCoroutine(
                ResetOffsetsAndSwitchCameraCoroutine(nextCameraEntry));
        }

        private IEnumerator SwitchCameraCoroutine(VCamera cameraEntry)
        {
            isTransitioning = true;

            SetActiveCameraImmediate(cameraEntry);

            yield return new WaitForSecondsRealtime(cameraSwitchLockDuration);

            isTransitioning = false;
            cameraTransitionCoroutine = null;
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

            SetActiveCameraImmediate(nextCameraEntry);

            yield return new WaitForSecondsRealtime(cameraSwitchLockDuration);

            isTransitioning = false;
            cameraTransitionCoroutine = null;
        }

        private void SetActiveCameraImmediate(VCamera cameraEntry)
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

                entry.VirtualCamera.Priority =
                    entry == cameraEntry ? ActivePriority : InactivePriority;
            }

            currentVCamera = cameraEntry;

            if (currentVCamera.CameraType == VirtualCameraType.FreeLook)
                SetupFreeLookCamera();

            LensSettings lens = CurrentVirtualCamera.Lens;

            targetFieldOfView = Mathf.Clamp(
                lens.FieldOfView,
                currentVCamera.MinFieldOfView,
                currentVCamera.MaxFieldOfView);

            CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();

            if (follow != null)
            {
                targetFollowOffset = follow.FollowOffset;

                targetFollowOffset.x = followOffsetX;

                targetFollowOffset.y = Mathf.Clamp(
                    targetFollowOffset.y,
                    currentVCamera.MinFollowOffsetY,
                    currentVCamera.MaxFollowOffsetY);

                follow.FollowOffset = targetFollowOffset;
            }
        }

        private void SetupFreeLookCamera()
        {
            if (freeLookTarget == null)
            {
                Debug.LogWarning("CameraManager: FreeLook target is not assigned.");
                return;
            }

            if (Player.Instance == null)
            {
                Debug.LogWarning("CameraManager: Could not snap FreeLook target. Player not found.");
                return;
            }

            freeLookTarget.position = Player.Instance.transform.position;

            CurrentVirtualCamera.Follow = freeLookTarget;
        }

        private void CreateFreeLookTargetIfNeeded()
        {
            if (freeLookTarget != null)
                return;

            GameObject target = new GameObject("FreeLookTarget");
            freeLookTarget = target.transform;
        }

        public void CycleCamera()
        {
            if (isTransitioning)
                return;

            if (virtualCameras == null || virtualCameras.Count == 0)
                return;

            int currentIndex = virtualCameras.IndexOf(currentVCamera);
            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % virtualCameras.Count;

            if (cameraTransitionCoroutine != null)
                StopCoroutine(cameraTransitionCoroutine);

            cameraTransitionCoroutine = StartCoroutine(
                SwitchCameraCoroutine(virtualCameras[nextIndex]));
        }

        public void ChangeFieldOfView(float sizeChange)
        {
            if (currentVCamera == null || CurrentVirtualCamera == null)
                return;

            targetFieldOfView = Mathf.Clamp(
                targetFieldOfView + sizeChange,
                currentVCamera.MinFieldOfView,
                currentVCamera.MaxFieldOfView);
        }

        public void ChangeFollowOffsetY(float scrollInput)
        {
            if (currentVCamera == null || CurrentVirtualCamera == null)
                return;

            targetFollowOffset.y = Mathf.Clamp(
                targetFollowOffset.y + scrollInput * followOffsetScrollSpeed,
                currentVCamera.MinFollowOffsetY,
                currentVCamera.MaxFollowOffsetY);

            targetFollowOffset.x = followOffsetX;
        }

        public void ChangeFreeLookOffsetXZ(Vector2 movement)
        {
            if (currentVCamera == null || CurrentVirtualCamera == null)
                return;

            if (currentVCamera.CameraType != VirtualCameraType.FreeLook)
                return;

            if (freeLookTarget == null)
                return;

            Vector3 moveDelta = new Vector3(
                movement.x,
                0f,
                movement.y) * (freeLookPanSpeed * CameraDeltaTime);

            freeLookTarget.position += moveDelta;
        }

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
            if (CurrentVirtualCamera == null)
                return;

            CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();

            if (follow == null)
                return;

            targetFollowOffset = new Vector3(
                followOffsetX,
                follow.FollowOffset.y,
                -30f);
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
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed);

            if (isShiftHeld)
                ChangeFieldOfView(-scrollY * fieldOfViewScrollSensitivity);
            else
                ChangeFollowOffsetY(scrollY);
        }

        private void HandleFreeLookMovement()
        {
            if (currentVCamera == null ||
                currentVCamera.CameraType != VirtualCameraType.FreeLook)
                return;

            Vector2 moveInput = Vector2.zero;
            moveInput = KeyboardPanning(moveInput);
            moveInput = MousePanning(moveInput);

            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            if (moveInput == Vector2.zero)
                return;

            ChangeFreeLookOffsetXZ(moveInput);
        }

        private Vector2 MousePanning(Vector2 moveInput)
        {
            if (!enableMouseRegionPanning)
                return moveInput;

            if (Mouse.current == null)
                return moveInput;

            if (Helpers.IsOverUI())
                return moveInput;

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            bool mouseIsOnScreen =
                mousePosition.x >= 0f &&
                mousePosition.x <= Screen.width &&
                mousePosition.y >= 0f &&
                mousePosition.y <= Screen.height;

            if (!mouseIsOnScreen)
                return moveInput;

            if (mousePosition.x <= mouseRegionPanSize)
                moveInput.x -= 1f;

            if (mousePosition.x >= Screen.width - mouseRegionPanSize)
                moveInput.x += 1f;

            if (mousePosition.y <= mouseRegionPanSize)
                moveInput.y -= 1f;

            if (mousePosition.y >= Screen.height - mouseRegionPanSize)
                moveInput.y += 1f;

            return moveInput;
        }

        private static Vector2 KeyboardPanning(Vector2 moveInput)
        {
            // Keyboard movement
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed)
                    moveInput.x -= 1f;

                if (Keyboard.current.dKey.isPressed)
                    moveInput.x += 1f;

                if (Keyboard.current.sKey.isPressed)
                    moveInput.y -= 1f;

                if (Keyboard.current.wKey.isPressed)
                    moveInput.y += 1f;
            }

            return moveInput;
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

        private void UpdateFollowOffset()
        {
            CinemachineFollow follow = CurrentVirtualCamera.GetComponent<CinemachineFollow>();

            if (follow == null)
                return;

            targetFollowOffset.x = followOffsetX;

            Vector3 offset = Vector3.Lerp(
                follow.FollowOffset,
                targetFollowOffset,
                CameraDeltaTime * followOffsetLerpSpeed);

            if (Vector3.Distance(offset, targetFollowOffset) <= 0.01f)
                offset = targetFollowOffset;

            follow.FollowOffset = offset;
        }

        private void SetupDistanceCulling()
        {
            if (currentCamera == null)
                return;

            float[] distances = new float[32];

            for (int i = 0; i < 32; i++)
            {
                distances[i] =
                    (cullLayers.value & (1 << i)) != 0
                        ? cullDistance
                        : 0f;
            }

            currentCamera.layerCullDistances = distances;
            currentCamera.layerCullSpherical = true;
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
                SetActiveCameraImmediate(bestEntry);
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