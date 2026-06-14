using UnityEngine;
using UnityEngine.EventSystems;

public interface IRaidEnemy
{
    void SetFarmTarget(Transform farmTarget);
    void StartRaid();
}