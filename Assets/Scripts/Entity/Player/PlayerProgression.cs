using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerProgression : MonoBehaviour, ISaveManager
    {
        [Header("Weapon")]
        [SerializeField] private int baseWeaponDamage = 1;
        [SerializeField] private int weaponDamageLevel = 0;

        public int BaseWeaponDamage => baseWeaponDamage;
        public int WeaponDamageLevel => weaponDamageLevel;
        public int CurrentWeaponDamage => baseWeaponDamage + weaponDamageLevel;

        public void IncreaseWeaponDamage(int amount = 1)
        {
            weaponDamageLevel += Mathf.Max(1, amount);
        }

        public void LoadData(GameData data)
        {
            weaponDamageLevel = data.weaponDamageLevel;
        }

        public void SaveData(ref GameData data)
        {
            data.weaponDamageLevel = weaponDamageLevel;
        }
    }
}