using ShiftedSignal.Garden.UserInterface.Containers;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopScrollbarSnap : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private ShopMenuUI shopMenu;

    public void OnEndDrag(PointerEventData eventData)
    {
        if (shopMenu != null)
        {
            shopMenu.SnapBuyScrollToNearestItem();
        }
    }
}