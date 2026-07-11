using System.Collections;
using ShiftedSignal.Garden.Buildable;
using ShiftedSignal.Garden.UserInterface.Components;
using ShiftedSignal.Garden.UserInterface.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class BuildingUnderConstructionUI : MonoBehaviour, IUIElement<BaseBuilding>
    {
        [SerializeField] private TextMeshProUGUI unitName;
        [SerializeField] private ProgressBar progressBar;

        public void EnableFor(BaseBuilding building)
        {
            gameObject.SetActive(true);
            unitName.SetText(building.UnitSO.Name);
            StartCoroutine(AnimateBuildingProgress(building));
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateBuildingProgress(BaseBuilding building)
        {
            while (enabled && building.IsUnderConstruction)
            {
                progressBar.SetProgress(
                    building.BuildProgressPercent);

                yield return null;
            }

            if (building.IsComplete)
                progressBar.SetProgress(1f);
        }
    }
}