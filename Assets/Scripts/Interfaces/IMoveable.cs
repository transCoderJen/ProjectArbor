using UnityEngine;

namespace ShiftedSignal.Garden.Interfaces
{    
    public interface IMoveable
    {
        void MoveTo(Vector3 position);
        void Stop();
    }
}