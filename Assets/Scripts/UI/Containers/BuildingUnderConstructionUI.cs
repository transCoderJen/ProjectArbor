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
            if (building == null)
            {
                yield break;
            }

            int frameCount = 0;

            while (enabled && building.IsUnderConstruction)
            {
                frameCount++;

                float progress = building.BuildProgressPercent;
                progressBar.SetProgress(progress);

                yield return null;
            }


            if (building != null && building.IsComplete)
                progressBar.SetProgress(1f);

        }
    }
}