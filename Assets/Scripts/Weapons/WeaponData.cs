using ShiftedSignal.Garden.Managers;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    public PooledObjectList SlashFX;
    public PooledObjectList HitFX;
}