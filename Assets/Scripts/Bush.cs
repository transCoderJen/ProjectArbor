using System;
using System.Collections;
using System.Net;
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
    
    [SerializeField] private GameObject BerriesParent;
    [SerializeField] private Sprite BerriesSprite;
    [SerializeField] private bool HasBerries = true;
    [SerializeField] private ItemData Berries;

    private SpriteRenderer sr;
    public bool IsPlayerNear { get; private set; } = false;
    
    public void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        
        SpriteRenderer[] berrySrs = BerriesParent != null
            ? BerriesParent.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];

        foreach (SpriteRenderer berrySpriteRenderer in berrySrs)
        {
            berrySpriteRenderer.sprite = BerriesSprite;
            // TODO randomize size and placement of berries on the bush
        }
    }

    void OnEnable()
    {
        Bus<DayStartedEvent>.OnEvent += ResetBerries;
    }

    void OnDisable()
    {
        Bus<DayStartedEvent>.OnEvent -= ResetBerries;
    }

    private void ResetBerries(DayStartedEvent args)
    {
        HasBerries = true;
        BerriesParent.SetActive(true);
    }

    public void Highlight(bool highlight)
    {
        if (highlight)
            sr.material.SetColor("_OuterOutlineColor", HighlightColor);
        else 
            sr.material.SetColor("_OuterOutlineColor", Color.black);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() != null)
        {
            IsPlayerNear = true;
            Highlight(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() != null)
        {
            IsPlayerNear = true;
            Highlight(false);
        }
    }

    
    public void Interact(Player player)
    {
        StartCoroutine(nameof(Wiggle));

        if (HasBerries)
        {
            Inventory.Instance.AddItem(Berries);
            HasBerries = false;

            //TODO set back to true at the begining of the day - or every x hours
        }
    }

    bool IInteractable.IsPlayerNear() => IsPlayerNear;

    private IEnumerator Wiggle()
    {
        sr.material.SetFloat("_VibrateFade", 1);
        yield return Helpers.GetWait(.2f);
        sr.material.SetFloat("_VibrateFade", 0);

        BerriesParent.SetActive(false);
    }
}