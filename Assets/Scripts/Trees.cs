using System.Collections;
using MoreMountains.Feedbacks;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.UserInterface;
using UnityEngine;

public class Trees : MonoBehaviour, IInteractable
{
    private static readonly int OuterOutlineColorId = Shader.PropertyToID("_OuterOutlineColor");
    private static readonly int VibrateFadeId = Shader.PropertyToID("_VibrateFade");

    [ColorUsage(false, true)]
    [SerializeField] private Color HighlightColor = Color.white;

    [Header("Wood")]
    [SerializeField] private ItemData WoodMaterial;
    [SerializeField] private int WoodBundlesAvailable = 3;
    [SerializeField] private int WoodAmountPerBundle = 5;

    [Header("Hits Per Wood")]
    [SerializeField] private int MinHitsPerWood = 2;
    [SerializeField] private int MaxHitsPerWood = 4;

    [Header("Feedback")]
    [SerializeField] private MMF_Player InteractFeedback;
    [SerializeField] private ParticleSystem FallingLeavesVFX;

    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;

    private Color currentOutlineColor;
    private float currentVibrateFade;

    private int currentHits;
    private int hitsRequiredForNextWood;
    private Coroutine wiggleRoutine;

    private void Awake()
    {
        sr = GetComponentInParent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        currentOutlineColor = Color.black;
        currentVibrateFade = 0f;

        RollHitsRequired();
        ApplyPropertyBlock();
    }

    public void Highlight(bool highlight)
    {
        currentOutlineColor = highlight ? HighlightColor : Color.black;
        ApplyPropertyBlock();
    }

    public void Interact(Player player)
    {
        Debug.Log("Trying to interact with tree");

        if (WoodBundlesAvailable <= 0)
            return;

        InteractFeedback?.PlayFeedbacks();

        if (wiggleRoutine != null)
            StopCoroutine(wiggleRoutine);

        wiggleRoutine = StartCoroutine(Wiggle());

        if (FallingLeavesVFX != null)
            FallingLeavesVFX.Play();

        currentHits++;

        if (currentHits < hitsRequiredForNextWood)
            return;

        GiveWood();

        WoodBundlesAvailable--;

        if (WoodBundlesAvailable <= 0)
        {
            StartCoroutine(DestroyTree());
            return;
        }

        RollHitsRequired();
    }

    private void GiveWood()
    {
        Inventory.Instance.AddItem(WoodMaterial, WoodAmountPerBundle);

        if (PickupPopupManager.Instance != null)
        {
            PickupPopupManager.Instance.Show(
                WoodMaterial.Icon,
                WoodAmountPerBundle,
                WoodMaterial.name);
        }
    }

    private void RollHitsRequired()
    {
        int min = Mathf.Max(1, MinHitsPerWood);
        int max = Mathf.Max(min, MaxHitsPerWood);

        hitsRequiredForNextWood = Random.Range(min, max + 1);
        currentHits = 0;
    }

    private IEnumerator DestroyTree()
    {
        yield return Helpers.GetWait(0.3f);
        Destroy(transform.parent.gameObject);
    }

    private IEnumerator Wiggle()
    {
        currentVibrateFade = 1f;
        ApplyPropertyBlock();

        yield return Helpers.GetWait(0.2f);

        currentVibrateFade = 0f;
        ApplyPropertyBlock();

        wiggleRoutine = null;
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
}