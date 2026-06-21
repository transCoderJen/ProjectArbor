using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    public class AnimatedNumber : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private float animationSpeed = 10f;

        private float currentValue;
        private float targetValue;

        private void Update()
        {
            if (Mathf.Approximately(currentValue, targetValue))
                return;

            currentValue = Mathf.Lerp(
                currentValue,
                targetValue,
                Time.deltaTime * animationSpeed);

            if (Mathf.Abs(currentValue - targetValue) < 0.5f)
                currentValue = targetValue;

            targetText.text = Mathf.RoundToInt(currentValue).ToString();
        }

        public void SetValue(int value)
        {
            targetValue = value;
        }

        public void SetImmediate(int value)
        {
            currentValue = value;
            targetValue = value;
            targetText.text = value.ToString();
        }
    }
}