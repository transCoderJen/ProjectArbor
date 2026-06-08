using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.UserInterface;
using UnityEngine;

public class Bush : MonoBehaviour, IInteractable
{
    private static readonly int OuterOutlineColorId = Shader.PropertyToID("_OuterOutlineColor");
    private static readonly int VibrateFadeId = Shader.PropertyToID("_VibrateFade");

    [ColorUsage(false, true)]
    [SerializeField] private Color HighlightColor = Color.white;

    [Header("Berries")]
    [SerializeField] private GameObject BerriesParent;
    [SerializeField] private Sprite BerriesSprite;
    [SerializeField] private bool HasBerries = true;
    [SerializeField] private ItemData Berry;

    [Header("Berry Generation")]
    [SerializeField] private int MinBerryCount = 4;
    [SerializeField] private int MaxBerryCount = 9;
    [SerializeField] private float MinBerryScale = 0.15f;
    [SerializeField] private float MaxBerryScale = 0.35f;
    [SerializeField] private Vector2 BerryAreaPadding = new(0.15f, 0.15f);

    private readonly List<GameObject> activeBerries = new();

    private SpriteRenderer sr;
    private SpriteMask berryMask;
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

        EnsureBerriesParent();
        EnsureBerryMask();
        GenerateBerries();
    }

    private void OnEnable()
    {
        Bus<DayStartedEvent>.OnEvent += ResetBerries;
    }

    private void OnDisable()
    {
        Bus<DayStartedEvent>.OnEvent -= ResetBerries;
    }

    private void EnsureBerriesParent()
    {
        if (BerriesParent != null)
            return;

        BerriesParent = new GameObject("Berries Parent");
        BerriesParent.transform.SetParent(transform);
        BerriesParent.transform.localPosition = Vector3.zero;
        BerriesParent.transform.localRotation = Quaternion.identity;
        BerriesParent.transform.localScale = Vector3.one;
    }

    private void EnsureBerryMask()
    {
        Transform existingMask = transform.Find("Berry Sprite Mask");

        if (existingMask != null)
            berryMask = existingMask.GetComponent<SpriteMask>();

        if (berryMask == null)
        {
            GameObject maskObject = new GameObject("Berry Sprite Mask");
            maskObject.transform.SetParent(transform);
            maskObject.transform.localPosition = Vector3.zero;
            maskObject.transform.localRotation = Quaternion.identity;
            maskObject.transform.localScale = Vector3.one;

            berryMask = maskObject.AddComponent<SpriteMask>();
        }

        berryMask.sprite = sr.sprite;
        berryMask.isCustomRangeActive = true;
        berryMask.frontSortingLayerID = sr.sortingLayerID;
        berryMask.frontSortingOrder = sr.sortingOrder + 10;
        berryMask.backSortingLayerID = sr.sortingLayerID;
        berryMask.backSortingOrder = sr.sortingOrder;
    }

    private void GenerateBerries()
    {
        ClearGeneratedBerries();

        if (BerriesSprite == null || sr == null || sr.sprite == null)
            return;

        int berryCount = Random.Range(MinBerryCount, MaxBerryCount + 1);
        Bounds spriteBounds = sr.sprite.bounds;

        float minX = spriteBounds.min.x + BerryAreaPadding.x;
        float maxX = spriteBounds.max.x - BerryAreaPadding.x;
        float minY = spriteBounds.min.y + BerryAreaPadding.y;
        float maxY = spriteBounds.max.y - BerryAreaPadding.y;

        for (int i = 0; i < berryCount; i++)
        {
            GameObject berryObject = new($"Berry {i + 1}");
            berryObject.transform.SetParent(BerriesParent.transform);

            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            float randomScale = Random.Range(MinBerryScale, MaxBerryScale);

            berryObject.transform.localPosition = new Vector3(randomX, randomY, -0.01f);
            berryObject.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            berryObject.transform.localScale = Vector3.one * randomScale;

            SpriteRenderer berryRenderer = berryObject.AddComponent<SpriteRenderer>();
            berryRenderer.sprite = BerriesSprite;
            berryRenderer.sortingLayerID = sr.sortingLayerID;
            berryRenderer.sortingOrder = sr.sortingOrder + 1;
            berryRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            activeBerries.Add(berryObject);
        }

        HasBerries = activeBerries.Count > 0;
        BerriesParent.SetActive(HasBerries);
    }

    private void ClearGeneratedBerries()
    {
        activeBerries.Clear();

        if (BerriesParent == null)
            return;

        for (int i = BerriesParent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = BerriesParent.transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private void ResetBerries(DayStartedEvent args)
    {
        HasBerries = true;
        GenerateBerries();

        if (BerriesParent != null)
            BerriesParent.SetActive(true);
    }

    public void Highlight(bool highlight)
    {
        currentOutlineColor = highlight ? HighlightColor : Color.black;
        ApplyPropertyBlock();
    }

    public void Interact(Player player)
    {
        if (!HasBerries || activeBerries.Count <= 0)
            return;

        StartCoroutine(Wiggle());

        Inventory.Instance.AddItem(Berry);

        if (PickupPopupManager.Instance != null)
        {
                PickupPopupManager.Instance.Show(
                transform.position,
                Berry.Icon,
                1);
        }
        
        RemoveOneBerry();

        HasBerries = activeBerries.Count > 0;

        if (!HasBerries && BerriesParent != null)
            BerriesParent.SetActive(false);
    }

    private void RemoveOneBerry()
    {
        if (activeBerries.Count <= 0)
            return;

        int randomIndex = Random.Range(0, activeBerries.Count);
        GameObject berryToRemove = activeBerries[randomIndex];

        activeBerries.RemoveAt(randomIndex);

        if (berryToRemove != null)
            Destroy(berryToRemove);
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