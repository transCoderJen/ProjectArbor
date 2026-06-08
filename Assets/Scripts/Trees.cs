using System.Collections;
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

    [SerializeField] private ItemData WoodMaterial;
    [SerializeField] private int WoodAvailable = 3;
    [SerializeField] private ParticleSystem FallingLeavesVFX;

    private SpriteRenderer sr;
    private MaterialPropertyBlock propertyBlock;

    private Color currentOutlineColor;
    private float currentVibrateFade;

    private void Awake()
    {
        sr = GetComponentInParent<SpriteRenderer>();
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
        Debug.Log("Trying to interact with wood");

        if (WoodAvailable <= 0)
            return;
        
        Inventory.Instance.AddItem(WoodMaterial);

        if (PickupPopupManager.Instance != null)
        {
            PickupPopupManager.Instance.Show(
                transform.position,
                WoodMaterial.Icon,
                1);
        }

        StartCoroutine(Wiggle());

        if (FallingLeavesVFX != null)
            FallingLeavesVFX.Play();

        WoodAvailable--;

        Inventory.Instance.AddItem(WoodMaterial);

        if (WoodAvailable == 0)
            StartCoroutine(DestroyTree());
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