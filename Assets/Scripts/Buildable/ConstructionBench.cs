using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.UserInterface.Containers;
using UnityEngine;

public class ConstructionBench : MonoBehaviour, IInteractable
{
    private static readonly int OuterOutlineColorId =
        Shader.PropertyToID("_OuterOutlineColor");

    [Header("Highlight")]
    [ColorUsage(false, true)]
    [SerializeField] private Color highlightColor = Color.white;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("UI")]
    [SerializeField] private ConstructionMenuUI constructionMenu;

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        propertyBlock = new MaterialPropertyBlock();

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
        if (constructionMenu == null)
        {
            Debug.LogWarning(
                $"{nameof(ConstructionBench)} on {name} has no construction menu assigned.",
                this);

            return;
        }

        constructionMenu.Open();
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