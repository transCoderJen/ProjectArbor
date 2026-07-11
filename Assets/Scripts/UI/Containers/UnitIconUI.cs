using System;
using System.Collections;
using ShiftedSignal.Garden.Units;
using ShiftedSignal.Garden.UserInterface.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.UserInterface.Containers
{
    public class UnitIconUI : MonoBehaviour, IUIElement<AbstractCommandable>
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI healthText;

        private AbstractCommandable commandable;

        private const string HEALTH_TEXT_FORMAT = "{0} / {1}";

        public void EnableFor(AbstractCommandable commandable)
        {
            gameObject.SetActive(true);
            healthText.SetText(string.Format(HEALTH_TEXT_FORMAT, commandable.CurrentHealth, commandable.MaxHealth));
            icon.sprite = commandable.Config.Icon;
            this.commandable = commandable;
            commandable.OnHealthUpdated -= OnHealthUpdated;
            commandable.OnHealthUpdated += OnHealthUpdated;
        }

        public void Disable()
        {
            gameObject.SetActive(false);

            if (commandable != null)
            {
                commandable.OnHealthUpdated -= OnHealthUpdated;
                commandable = null;
            }
        }

        private void OnHealthUpdated(AbstractCommandable commandable, int lastHealth, int currentHealth)
        {
            StartCoroutine(ScrollHealth(lastHealth, currentHealth, commandable.MaxHealth));
        }

        private IEnumerator ScrollHealth(int startHealth, int endHealth, int maxHealth)
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
            elapsed += Time.deltaTime;
            int displayHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, endHealth, elapsed / duration));
            healthText.SetText(string.Format(HEALTH_TEXT_FORMAT, displayHealth, maxHealth));
            yield return null;
            }

            healthText.SetText(string.Format(HEALTH_TEXT_FORMAT, endHealth, maxHealth));
        }
    }
}