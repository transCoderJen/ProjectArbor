using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    public abstract class BaseCommand : ScriptableObject, ICommand
    {
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: Range(0,8)] [field: SerializeField] public int Slot { get; private set; }
        [field: SerializeField] public bool RequiresClickToActivate { get; private set; } = true;
        // [field: SerializeField] public GameObject GhostPrefab { get; private set; }

        public abstract bool CanHandle(CommandContext context);
        public abstract void Handle(CommandContext context);

        public abstract bool IsLocked(CommandContext context);

        public virtual void Activate(AbstractCommandable commandable)
        {
        }
    }
}