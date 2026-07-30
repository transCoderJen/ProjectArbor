using ShiftedSignal.Garden.Dialogue;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.Shops;
using ShiftedSignal.Garden.UserInterface.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.NPCs
{
    public class Shopkeeper : MonoBehaviour, IInteractable
    {
        private static readonly int OuterOutlineColorId =
            Shader.PropertyToID("_OuterOutlineColor");

        [Header("Shop")]
        [SerializeField] private ShopSO shop;

        [Header("Highlight")]
        [ColorUsage(false, true)]
        [SerializeField] private Color highlightColor = Color.white;

        [SerializeField] private SpriteRenderer spriteRenderer;

        private MaterialPropertyBlock propertyBlock;

        public ShopSO Shop => shop;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            propertyBlock = new MaterialPropertyBlock();

            SetOutlineColor(Color.black);
        }

        private void OnDisable()
        {
            SetOutlineColor(Color.black);
        }

        public void Highlight(bool highlight)
        {
            SetOutlineColor(
                highlight
                    ? highlightColor
                    : Color.black);
        }

        public void Interact(Player player)
        {
            DialogueManager.Instance.SetActiveShop(shop);
            
            Bus<EnterDialogueEvent>.Raise(
                new EnterDialogueEvent(shop.ShopDialogueKnot));
        }
        
        private void SetOutlineColor(Color color)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor(
                OuterOutlineColorId,
                color);

            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}