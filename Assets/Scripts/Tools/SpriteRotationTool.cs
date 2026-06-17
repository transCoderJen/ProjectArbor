using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShiftedSignal.Garden.Tools
{
    public class SpriteRotationTool : MonoBehaviour
    {
        [Header("Filtering")]
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private bool includeInactive = true;

        [Header("Sprite Rotation")]
        [SerializeField] private float spriteXRotation = 0f;
        [SerializeField] private float spriteYRotation = 0f;
        [SerializeField] private float spriteZRotation = 0f;

        [Header("Camera Offset Compensation")]
        [Tooltip("If true, Sprite Y Rotation is multiplied by -1. Useful when compensating for CameraManager FollowOffsetX.")]
        [SerializeField] private bool invertYRotation = true;

        [ContextMenu("Fix All 2D Sprite Rotations")]
        private void FixAll2DSpriteRotations()
        {
            SpriteRenderer[] spriteRenderers = FindObjectsByType<SpriteRenderer>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                Debug.LogWarning("SpriteRotationTool: No SpriteRenderers found.");
                return;
            }

            float finalYRotation = invertYRotation ? -spriteYRotation : spriteYRotation;

            Quaternion targetRotation = Quaternion.Euler(
                spriteXRotation,
                finalYRotation,
                spriteZRotation);

            int changedCount = 0;
            int skippedCount = 0;

            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer == null)
                    continue;

                if (!IsInAffectedLayer(spriteRenderer.gameObject.layer))
                {
                    skippedCount++;
                    continue;
                }

                Transform spriteTransform = spriteRenderer.transform;

#if UNITY_EDITOR
                Undo.RecordObject(spriteTransform, "Fix Sprite Rotation");
#endif

                spriteTransform.rotation = targetRotation;
                changedCount++;

#if UNITY_EDITOR
                EditorUtility.SetDirty(spriteTransform);
#endif
            }

            Debug.Log(
                $"SpriteRotationTool: Rotated {changedCount} SpriteRenderers. Skipped {skippedCount} due to layer mask.");
        }

        private bool IsInAffectedLayer(int objectLayer)
        {
            return (affectedLayers.value & (1 << objectLayer)) != 0;
        }
    }
}