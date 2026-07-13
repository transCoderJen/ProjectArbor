// using System;
// using System.Collections.Generic;
// using UnityEngine;

// namespace ShiftedSignal.Garden.EventBus
// {
//     public static class Bus<T> where T : IEvent
//     {
//         public delegate void Event(T args);

//         public static Dictionary<Owner, Event> OnEvent = new()
//         {          
//             { Owner.Unowned,   null },
//             { Owner.Player,    null } ,
//             { Owner.Friendly,  null },
//             { Owner.Enemy,     null },
//             { Owner.Buildable, null }
//         };

//         public static void Raise(Owner owner, T evt) => OnEvent[owner]?.Invoke(evt);

//         public static void RegisterForAll(Event handler)
//         {
//             foreach(Owner owner in Enum.GetValues(typeof(Owner)))
//             {
//                 OnEvent[owner] += handler;
//             }
//         }

//         public static void UnregisterForAll(Event handler)
//         {
//             foreach(Owner owner in Enum.GetValues(typeof(Owner)))
//             {
//                 OnEvent[owner] -= handler;
//             }
//         }
//     }    
// }

using UnityEngine;

namespace ShiftedSignal.Garden.EventBus
{
    public static class Bus<T> where T : IEvent
    {
        public delegate void Event(T args);
        public static event Event OnEvent;

        public static void Raise(T evt) => OnEvent?.Invoke(evt);
    }    
}