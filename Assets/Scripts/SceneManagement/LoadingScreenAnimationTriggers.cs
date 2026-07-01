using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;

namespace ShiftedSignal.Garden.SceneManagement
{
    public class LoadingScreenAnimationTriggers : MonoBehaviour
    {
        public static string CurrentLoadingMessage = "Loading...";

        public void ShowLoadingScreen()
        {
            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.Show(CurrentLoadingMessage);
        }

        public void HideLoadingScreen()
        {
            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.Hide();
        }
    }
}