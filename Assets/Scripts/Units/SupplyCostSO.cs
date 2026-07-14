using System;
using ShiftedSignal.Garden.Environment;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.Units
{
    [Serializable]
    public struct RequiredSupply
    {
        public SupplySO Material;
        public int Amount;
    }

    [CreateAssetMenu(fileName = "Supply Cost", menuName = "Data/Supply Cost", order = 5)]
    public class SupplyCostSO : ScriptableObject
    {
        [Header("Gold Cost")]
        [field: SerializeField] public int Cost { get; private set; }

        [Header("Material Cost")]
        [field: SerializeField] public RequiredSupply[] RequiredSupplies { get; private set; }

        public bool CanAfford()
        {
            if (PlayerManager.Instance == null)
                return false;

            if (PlayerManager.Instance.Currency < Cost)
                return false;

            return HasRequiredSupplies();
        }

        private bool HasRequiredSupplies()
        {
            if (RequiredSupplies == null)
                return true;

            for (int i = 0; i < RequiredSupplies.Length; i++)
            {
                RequiredSupply required = RequiredSupplies[i];

                if (required.Material == null)
                    continue;

                if (!required.Material.HasEnoughSupplies(required.Amount))
                    return false;
            }

            return true;
        }

        public void Spend()
        {
            if (PlayerManager.Instance == null)
                return;

            PlayerManager.Instance.Currency -= Cost;

            Bus<CurrencyUpdatedEvent>.Raise(
                new CurrencyUpdatedEvent(-Cost));

            
            SpendRequiredSupplies();
        }

        private void SpendRequiredSupplies()
        {
            if (RequiredSupplies == null)
                return;

            for (int i = 0; i < RequiredSupplies.Length; i++)
            {
                RequiredSupply required = RequiredSupplies[i];

                if (required.Material == null)
                    continue;

                required.Material.SpendSupplies(required.Amount);

                Bus<SupplyEvent>.Raise(
                    new SupplyEvent(-required.Amount, required.Material));
            }
        }

        public void Refund()
        {
            if (PlayerManager.Instance == null)
                return;

            if (Cost > 0)
            {
                PlayerManager.Instance.Currency += Cost;

                Bus<CurrencyUpdatedEvent>.Raise(
                    new CurrencyUpdatedEvent(Cost));
            }

            RefundRequiredSupplies();
        }

        private void RefundRequiredSupplies()
        {
            if (RequiredSupplies == null)
                return;

            for (int i = 0; i < RequiredSupplies.Length; i++)
            {
                RequiredSupply required = RequiredSupplies[i];

                if (required.Material == null || required.Amount <= 0)
                    continue;

                Bus<SupplyEvent>.Raise(
                    new SupplyEvent(
                        required.Amount,
                        required.Material));
            }
        }
    }
}