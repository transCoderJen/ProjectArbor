using System.Collections;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.GridSystem
{
    public class GridActivationAltar : MonoBehaviour, IInteractable
    {
        private static readonly int OuterOutlineColorId = Shader.PropertyToID("_OuterOutlineColor");
        private static readonly int VibrateFadeId = Shader.PropertyToID("_VibrateFade");

        [Header("Activation")]
        [SerializeField] private GridActivationCondition activationCondition;
        [SerializeField] private float activationRadius = 12f;
        [SerializeField] private bool activateOnlyOnce = true;

        [Header("Highlight")]
        [ColorUsage(false, true)]
        [SerializeField] private Color HighlightColor = Color.white;

        [Header("State")]
        [SerializeField] private bool hasActivated;

        private SpriteRenderer sr;
        private MaterialPropertyBlock propertyBlock;

        private Color currentOutlineColor;
        private float currentVibrateFade;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            propertyBlock = new MaterialPropertyBlock();

            currentOutlineColor = Color.black;
            currentVibrateFade = 0f;

            ApplyPropertyBlock();
        }

        public void Highlight(bool highlight)
        {
            currentOutlineColor = highlight ? HighlightColor : Color.black;
            ApplyPropertyBlock();
        }

        public void Interact(Player player)
        {
            TryActivate();
        }

        public void TryActivate()
        {
            if (activateOnlyOnce && hasActivated)
                return;

            if (activationCondition == null)
            {
                Debug.LogWarning($"{name} has no activation condition assigned.");
                return;
            }

            if (!activationCondition.CanActivate())
            {
                Debug.Log("Activation condition not met.");
                StartCoroutine(Wiggle());
                return;
            }

            activationCondition.ConsumeCost();

            GridManager.Instance.ActivateBlocksInRadius(transform.position, activationRadius);

            hasActivated = true;

            StartCoroutine(Wiggle());
        }

        private IEnumerator Wiggle()
        {
            currentVibrateFade = 1f;
            ApplyPropertyBlock();

            yield return Helpers.GetWait(0.2f);

            currentVibrateFade = 0f;
            ApplyPropertyBlock();
        }

        private void ApplyPropertyBlock()
        {
            if (sr == null)
                return;

            sr.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor(OuterOutlineColorId, currentOutlineColor);
            propertyBlock.SetFloat(VibrateFadeId, currentVibrateFade);

            sr.SetPropertyBlock(propertyBlock);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}