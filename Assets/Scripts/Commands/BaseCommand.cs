using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Commands
{
    public abstract class BaseCommand : ScriptableObject, ICommand
    {
        [field: SerializeField] public string Name { get; private set; } = "Command";
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: Range(0,8)] [field: SerializeField] public int Slot { get; private set; }
        [field: SerializeField] public bool RequiresClickToActivate { get; private set; } = true;
        public abstract bool CanHandle(CommandContext context);

        public abstract void Handle(CommandContext context);
        /// <summary>
        /// Whether or no this item should be enabled on the UI when displayed
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        
        
        public abstract bool IsLocked(CommandContext context);
        /// <summary>
        /// Whether or not this item is eligible to show up on the UI.
        /// For example, Upgrades may have multiple items assigned to the same slot.
        /// This function should differentiate whoch one woll show up at a vien time.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual bool IsAvailable(CommandContext context) => true;

        public virtual void Activate(AbstractCommandable commandable)
        {
        }
    }
}