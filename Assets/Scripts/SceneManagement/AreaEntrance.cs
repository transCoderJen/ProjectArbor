using System.Collections;
using ShiftedSignal.Garden.Managers;
using UnityEngine;

namespace ShiftedSignal.Garden.SceneManagement
{
    public class AreaEntrance : MonoBehaviour
    {
        [SerializeField] private string TransitionName;
        [SerializeField] private Transform WayPoint;
        [SerializeField] TransitionType TransitionType;
        [SerializeField] CameraManager.VirtualCameraType virtualCameraType;

        private void Start() {
            if (TransitionName == SceneManager.Instance.SceneTransitionName)
            {
                SetPlayerPosition();
                LevelLoader.Instance.StartScene(TransitionType);
                PlayerManager.Instance.ResetPlayer();
                Invoke(nameof(ResetCameraPosition), .1f);
            }
        }

        private void SetPlayerPosition()
        {
            PlayerManager.Instance.Player.transform.position = transform.position;
        }

        private void ResetCameraPosition()
        {
            // CameraManager.Instance.SetPlayerCameraFollow();
            // CameraManager.Instance.gameObject.SetActive(false);
            CameraManager.Instance.SwitchCamera(virtualCameraType);
            // CameraManager.Instance.gameObject.SetActive(true);
        }
    }
}
