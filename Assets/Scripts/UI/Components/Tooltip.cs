using System.Collections.Generic;
using System.Text;
using ShiftedSignal.Garden.Commands;
using ShiftedSignal.Garden.TechTree;
using ShiftedSignal.Garden.Units;
using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    public class Tooltip : MonoBehaviour
    {
        [field: SerializeField]
        public RectTransform RectTransform { get; private set; }

        [field: SerializeField]
        [field: Range(0f, 1f)]
        public float HoverDelay { get; private set; } = 0.5f;

        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private TechTreeSO techTree;

        private readonly StringBuilder stringBuilder = new();

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public void SetText(BaseCommand command)
        {
            if (command == null)
            {
                text.text = string.Empty;
                return;
            }

            stringBuilder.Clear();
            stringBuilder.AppendLine(command.name);

            UnlockableSO unlockable = GetUnlockable(command);

            if (unlockable != null &&
                techTree != null &&
                !techTree.IsUnlocked(unlockable))
            {
                AddUnlockRequirementsText(unlockable);
            }
            else
            {
                SupplyCostSO supplyCost = GetSupplyCost(command);

                if (supplyCost != null)
                {
                    AddSupplyCostText(supplyCost);
                }
            }

            text.text = stringBuilder.ToString().TrimEnd();

            ResizeTooltip();
        }

        private static UnlockableSO GetUnlockable(BaseCommand command)
        {
            return command switch
            {
                BuildUnitCommand buildUnitCommand =>
                    buildUnitCommand.Unit,

                BuildBuildingCommand buildBuildingCommand =>
                    buildBuildingCommand.Building,

                _ => null
            };
        }

        private static SupplyCostSO GetSupplyCost(BaseCommand command)
        {
            return command switch
            {
                BuildUnitCommand buildUnitCommand =>
                    buildUnitCommand.Unit?.SupplyCost,

                BuildBuildingCommand buildBuildingCommand =>
                    buildBuildingCommand.Building?.SupplyCost,

                _ => null
            };
        }

        private void AddUnlockRequirementsText(
            UnlockableSO unlockable)
        {
            stringBuilder.AppendLine("Requires:");

            IEnumerable<UnlockableSO> unmetDependencies =
                techTree.GetUnmetDependencies(unlockable);

            bool foundRequirement = false;

            foreach (UnlockableSO dependency in unmetDependencies)
            {
                if (dependency == null)
                    continue;

                stringBuilder.AppendLine($"• {dependency.Name}");
                foundRequirement = true;
            }

            if (!foundRequirement)
            {
                stringBuilder.AppendLine("• Unknown requirement");
            }
        }

        private void AddSupplyCostText(SupplyCostSO supplyCost)
        {
            if (supplyCost.Cost > 0)
            {
                stringBuilder.AppendLine($"Gold: {supplyCost.Cost}");
            }

            RequiredSupply[] requiredSupplies =
                supplyCost.RequiredSupplies;

            if (requiredSupplies == null)
                return;

            for (int i = 0; i < requiredSupplies.Length; i++)
            {
                RequiredSupply requiredSupply =
                    requiredSupplies[i];

                if (requiredSupply.Material == null ||
                    requiredSupply.Amount <= 0)
                {
                    continue;
                }

                stringBuilder.AppendLine(
                    $"{requiredSupply.Material.Name}: " +
                    $"{requiredSupply.Amount}");
            }
        }

        private void ResizeTooltip()
        {
            Vector2 preferredSize = text.GetPreferredValues();

            const float horizontalPadding = 50f;
            const float verticalPadding = 30f;

            RectTransform.sizeDelta = new Vector2(
                preferredSize.x + horizontalPadding,
                preferredSize.y + verticalPadding);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}