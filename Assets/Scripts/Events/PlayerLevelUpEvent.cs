using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct PlayerLevelUpEvent : IEvent
    {
        public int Level { get; private set; }
        
        public PlayerLevelUpEvent(int level)
        {
            Level = level;
        }
    }
}