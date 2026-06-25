using ShiftedSignal.Garden.Environment;
using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

namespace ShiftedSignal.Garden.Behavior
{
    #if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Behavior/Event Channels/GatherSuppliesEventChannel")]
    #endif
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "GatherSuppliesEventChannel", message: "[Self] gathers [Amount] [Supplies]", category: "Events", id: "171ec72f3599367d455bdc2b1cec036c")]
    public sealed partial class GatherSuppliesEventChannel : EventChannel<GameObject, int, SupplySO> { }
    
}

