using System;
using System.Collections;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Misc;
using ShiftedSignalGames.GOF.ItemsAndInventory;
using UnityEngine;
using UnityEngine.Android;

public class Tree : MonoBehaviour, IInteractable
{
    [ColorUsage(false, true)]
    [SerializeField] private Color HighlightColor;
    [SerializeField] private ItemData WoodMaterial;
    [SerializeField] private int WoodAvailable;
    [SerializeField] private ParticleSystem FallingLeavesVFX;
    private SpriteRenderer sr;
    public bool IsPlayerNear { get; private set; } = false;

    private void Awake()
    {
        sr = GetComponentInParent<SpriteRenderer>();
    }


    public void Highlight(bool highlight)
    {
        if (highlight)
            sr.material.SetColor("_OuterOutlineColor", HighlightColor);
        else
            sr.material.SetColor("_OuterOutlineColor", Color.black);
    }

    public void Interact(Player player)
    {
        Debug.Log("Trying to interact with wood");
        if (WoodAvailable > 0)
        {
            StartCoroutine(nameof(Wiggle));
            FallingLeavesVFX.Play();
            // TODO Add Audio For shaking tree
            WoodAvailable--;
            Inventory.Instance.AddItem(WoodMaterial);
            if (WoodAvailable == 0)
            {
                // TODO Destroy tree vfx
                StartCoroutine(nameof(DestroyTree));
                
            }
        }
    }

    private IEnumerator DestroyTree()
    {
        yield return Helpers.GetWait(0.3f);
        Destroy(transform.parent.gameObject);
    }

    bool IInteractable.IsPlayerNear() => IsPlayerNear;

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

    private IEnumerator Wiggle()
    {
        sr.material.SetFloat("_VibrateFade", 1);
        yield return Helpers.GetWait(.2f);
        sr.material.SetFloat("_VibrateFade", 0);
    }
}
