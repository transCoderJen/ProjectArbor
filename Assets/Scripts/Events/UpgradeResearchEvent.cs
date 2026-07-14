using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.TechTree;

namespace ShiftedSignal.Garden.Events
{
    public struct UpgradeResearchEvent : IEvent
    {
        public UpgradeSO Upgrade { get; private set; }
        
        public UpgradeResearchEvent(UpgradeSO upgrade)
        {
            Upgrade = upgrade;
        }
    }
}