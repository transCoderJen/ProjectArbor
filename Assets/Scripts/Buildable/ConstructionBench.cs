using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.UserInterface.Containers;
using UnityEngine;

public class ConstructionBench : MonoBehaviour, IInteractable
{
    private static readonly int OuterOutlineColorId =
        Shader.PropertyToID("_OuterOutlineColor");

    private const string ForemanKnotName = "foreman";

    [Header("Highlight")]
    [ColorUsage(false, true)]
    [SerializeField] private Color highlightColor = Color.white;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("UI")]
    [SerializeField] private ConstructionMenuUI constructionMenu;

    private MaterialPropertyBlock propertyBlock;
    private bool dialogueActive;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        propertyBlock = new MaterialPropertyBlock();

        SetOutlineColor(Color.black);
    }

    private void OnEnable()
    {
        Bus<OpenConstructionMenuEvent>.OnEvent +=
            HandleOpenConstructionMenu;

        Bus<ReturnToConstructionMenuEvent>.OnEvent +=
            HandleReturnToConstructionMenu;

        Bus<DialogueFinishedEvent>.OnEvent +=
            HandleDialogueFinished;
    }

    private void OnDisable()
    {
        Bus<OpenConstructionMenuEvent>.OnEvent -=
            HandleOpenConstructionMenu;

        Bus<ReturnToConstructionMenuEvent>.OnEvent -=
            HandleReturnToConstructionMenu;

        Bus<DialogueFinishedEvent>.OnEvent -=
            HandleDialogueFinished;

        dialogueActive = false;
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
        // Blocks interaction while the foreman conversation,
        // construction menu, or placement flow is active.
        if (dialogueActive)
            return;

        if (constructionMenu == null)
        {
            Debug.LogWarning(
                $"{name} has no ConstructionMenuUI assigned.",
                this);

            return;
        }

        dialogueActive = true;

        Bus<EnterDialogueEvent>.Raise(
            new EnterDialogueEvent(ForemanKnotName));
    }

    private void HandleOpenConstructionMenu(
        OpenConstructionMenuEvent evt)
    {
        if (!dialogueActive || constructionMenu == null)
            return;

        constructionMenu.Open();
    }

    private void HandleReturnToConstructionMenu(
        ReturnToConstructionMenuEvent evt)
    {
        if (!dialogueActive || constructionMenu == null)
            return;

        constructionMenu.ReturnFromPlacement();
    }

    private void HandleDialogueFinished(
        DialogueFinishedEvent evt)
    {
        if (!dialogueActive)
            return;

        dialogueActive = false;
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