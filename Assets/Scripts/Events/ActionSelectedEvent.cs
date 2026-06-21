using System;
using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;

namespace ShiftedSignal.Garden.Events
{
    public struct ActionSelectedEvent : IEvent
    {
        public BaseCommand Action { get; private set; }
        
        public ActionSelectedEvent(BaseCommand action)
        {
            Action = action;
        }
    }
}