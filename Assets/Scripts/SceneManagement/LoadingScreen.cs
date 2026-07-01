using ShiftedSignal.Garden.Misc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.SaveAndLoad
{
    public class LoadingScreen : Singleton<LoadingScreen>
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private Image progressFill;

        protected override void Awake()
        {
            base.Awake();

            Hide();
        }
        public void Show(string message)
        {
            if (root != null)
                root.SetActive(true);

            if (loadingText != null)
                loadingText.text = message;

            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(progress);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}