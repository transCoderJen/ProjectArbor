using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Interfaces;
using ShiftedSignal.Garden.ItemsAndInventory;
using ShiftedSignal.Garden.Units;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    [Serializable]
    public class SeedStack
    {
        public ItemData_Seed Seed;
        public int Amount;
    }

    public class SeedVault : BaseBuilding, ISeedSource
    {
        [Header("Seed Storage")]
        [SerializeField] private List<SeedStack> seedStacks = new();

        private Worker reservedWorker;

        public IReadOnlyList<SeedStack> SeedStacks => seedStacks;

        public bool HasAnySeed
        {
            get
            {
                foreach (SeedStack stack in seedStacks)
                {
                    if (stack != null &&
                        stack.Seed != null &&
                        stack.Amount > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool CanProvideSeed(ItemData_Seed seed, int amount)
        {
            if (seed == null || amount <= 0)
                return false;

            SeedStack stack = GetStack(seed);

            return stack != null && stack.Amount >= amount;
        }

        public bool TryBeginCollectSeed(Worker worker)
        {
            if (worker == null)
                return false;

            if (reservedWorker != null &&
                reservedWorker != worker)
            {
                return false;
            }

            reservedWorker = worker;
            return true;
        }

        public ItemData_Seed GetNextSeed()
        {
            foreach (SeedStack stack in seedStacks)
            {
                if (stack == null)
                    continue;

                if (stack.Seed == null)
                    continue;

                if (stack.Amount <= 0)
                    continue;

                return stack.Seed;
            }

            return null;
        }

        public bool TryCompleteCollectSeed(
            Worker worker,
            ItemData_Seed seed,
            int amount)
        {
            if (worker == null)
                return false;

            if (reservedWorker != worker)
                return false;

            if (!CanProvideSeed(seed, amount))
                return false;

            SeedStack stack = GetStack(seed);

            stack.Amount -= amount;

            if (stack.Amount <= 0)
            {
                seedStacks.Remove(stack);
            }

            reservedWorker = null;
            return true;
        }

        public void AbortCollectSeed(Worker worker)
        {
            if (worker == null)
                return;

            if (reservedWorker != worker)
                return;

            reservedWorker = null;
        }

        public bool TryAddSeed(ItemData_Seed seed, int amount = 1)
        {
            if (seed == null || amount <= 0)
                return false;

            SeedStack stack = GetStack(seed);

            if (stack == null)
            {
                seedStacks.Add(new SeedStack
                {
                    Seed = seed,
                    Amount = amount
                });
            }
            else
            {
                stack.Amount += amount;
            }

            return true;
        }

        private SeedStack GetStack(ItemData_Seed seed)
        {
            foreach (SeedStack stack in seedStacks)
            {
                if (stack == null)
                    continue;

                if (stack.Seed == seed)
                    return stack;
            }

            return null;
        }
    }
}