using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.Tools;
using Unity.Cinemachine;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{ 
    public class OcclusionManager : Singleton<OcclusionManager>
    {
        [SerializeField] private LayerMask occlusionLayers;
        [SerializeField] private float sphereCastRadius = 0.3f;
        [SerializeField] private float maxDistancePadding = 0.2f;
        [field: SerializeField] public float zDepthCutOff { get; private set; } = 10f;

        private Player player;
        private Camera currentCamera;

        private readonly HashSet<Occludable> currentOccluders = new HashSet<Occludable>();
        private readonly HashSet<Occludable> previousOccluders = new HashSet<Occludable>();


        protected override void Awake()
        {
            base.Awake();
            player = PlayerManager.Instance.Player;
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                player = PlayerManager.Instance.Player;
            }

            if (CameraManager.Instance != null)
                currentCamera = CameraManager.Instance.CurrentCamera;

            if (player == null || currentCamera == null)
                return;

            UpdateOcclusion();
        }

        private void UpdateOcclusion()
        {
            previousOccluders.Clear();

            foreach (Occludable occludable in currentOccluders)
                previousOccluders.Add(occludable);

            currentOccluders.Clear();

            Vector3 cameraPosition = currentCamera.transform.position;
            Vector3 playerPosition = player.transform.position;
            Vector3 direction = playerPosition - cameraPosition;
            float distance = direction.magnitude + maxDistancePadding;

            if (distance <= 0.01f)
                return;

            RaycastHit[] hits = Physics.SphereCastAll(
                cameraPosition,
                sphereCastRadius,
                direction.normalized,
                distance,
                occlusionLayers,
                QueryTriggerInteraction.Collide);

            Debug.DrawLine(cameraPosition, playerPosition, Color.red);

            for (int i = 0; i < hits.Length; i++)
            {
                Occludable occludable = hits[i].collider.GetComponentInParent<Occludable>();

                if (occludable == null)
                    continue;

                currentOccluders.Add(occludable);
            }

            foreach (Occludable occludable in currentOccluders)
            {
                occludable.SetOccluded(true);
                previousOccluders.Remove(occludable);
            }

            foreach (Occludable occludable in previousOccluders)
            {
                if (occludable != null)
                    occludable.SetOccluded(false);
            }
        }
    }
}