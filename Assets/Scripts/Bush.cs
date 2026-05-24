using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignalGames.GOF.ItemsAndInventory;
using UnityEngine;

public class Bush : MonoBehaviour, IInteractable
{
    [ColorUsage(false, true)]
    [SerializeField] private Color HighlightColor;

    [Header("Berries")]
    [SerializeField] private GameObject BerriesParent;
    [SerializeField] private Sprite BerriesSprite;
    [SerializeField] private bool HasBerries = true;
    [SerializeField] private ItemData Berries;

    [Header("Berry Generation")]
    [SerializeField] private int MinBerryCount = 4;
    [SerializeField] private int MaxBerryCount = 9;
    [SerializeField] private float MinBerryScale = 0.15f;
    [SerializeField] private float MaxBerryScale = 0.35f;
    [SerializeField] private Vector2 BerryAreaPadding = new Vector2(0.15f, 0.15f);

    private readonly List<GameObject> activeBerries = new();

    private SpriteRenderer sr;
    private SpriteMask berryMask;

    public bool IsPlayerNear { get; private set; } = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

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
        {
            berryMask = existingMask.GetComponent<SpriteMask>();
        }

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
            GameObject berryObject = new GameObject($"Berry {i + 1}");
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
        BerriesParent.SetActive(true);
    }

    public void Highlight(bool highlight)
    {
        if (highlight)
            sr.material.SetColor("_OuterOutlineColor", HighlightColor);
        else
            sr.material.SetColor("_OuterOutlineColor", Color.black);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() != null)
        {
            IsPlayerNear = true;
            Highlight(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() != null)
        {
            IsPlayerNear = false;
            Highlight(false);
        }
    }

    public void Interact(Player player)
    {
        if (!HasBerries || activeBerries.Count <= 0)
            return;

        StartCoroutine(nameof(Wiggle));

        Inventory.Instance.AddItem(Berries);
        RemoveOneBerry();

        HasBerries = activeBerries.Count > 0;

        if (!HasBerries)
        {
            BerriesParent.SetActive(false);
        }
    }

    private void RemoveOneBerry()
    {
        if (activeBerries.Count <= 0)
            return;

        int randomIndex = Random.Range(0, activeBerries.Count);
        GameObject berryToRemove = activeBerries[randomIndex];

        activeBerries.RemoveAt(randomIndex);

        if (berryToRemove != null)
        {
            Destroy(berryToRemove);
        }
    }

    bool IInteractable.IsPlayerNear() => IsPlayerNear;

    private IEnumerator Wiggle()
    {
        sr.material.SetFloat("_VibrateFade", 1);
        yield return Helpers.GetWait(.2f);
        sr.material.SetFloat("_VibrateFade", 0);
    }
}