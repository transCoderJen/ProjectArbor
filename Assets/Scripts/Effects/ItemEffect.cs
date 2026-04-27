using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public class ItemEffect : ScriptableObject
    {
        [TextArea]
        public string EffectDescription;

        public virtual void ExecuteEffect(Transform _spawnPosition)
        {

        }
    }
}