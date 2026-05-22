using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;
namespace ShiftedSignal.Garden.Interfaces
{
    public interface IInteractable
    {
        void Interact(Player player);
        void Highlight(bool highlight);
        public bool IsPlayerNear();
    }
}