using System.Collections;
using ShiftedSignal.Garden.EntitySpace;
using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
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
        [SerializeField] RotationAdjustmentDirection FacingDir;
        [SerializeField] private bool StartInGameTimer;

        private static readonly int MovementXHash = Animator.StringToHash("MovementX");
        private static readonly int MovementYHash = Animator.StringToHash("MovementY");

        private void Start() {
            if (TransitionName == SceneManager.Instance.SceneTransitionName)
            {
                Bus<UpdateInGameTimerEvent>.Raise(new UpdateInGameTimerEvent(StartInGameTimer));
                
                SetPlayerPosition();
                LevelLoader.Instance.StartScene(TransitionType);
                // PlayerManager.Instance.ResetPlayer();
                Invoke(nameof(ResetCameraPosition), .1f);
            }
        }

        private void SetPlayerPosition()
        {
            Player player = Player.Instance;

            if (player == null)
            {
                Debug.LogWarning("No Player instance found for AreaEntrance.");
                return;
            }

            player.transform.position = transform.position;
            player.StateMachine.ChangeState(player.IdleState);
            SetPlayerFacingDirection(player);
        }

        public void SetPlayerFacingDirection(Player player)
        {
            float movementX = 0f;
            float movementY = 0f;

            switch (FacingDir)
            {
                case RotationAdjustmentDirection.Up:
                    movementY = 1f;
                    break;
                case RotationAdjustmentDirection.UpRight:
                    movementX = 1f;
                    movementY = 1f;
                    break;
                case RotationAdjustmentDirection.Right:
                    movementX = 1f;
                    break;
                case RotationAdjustmentDirection.DownRight:
                    movementX = 1f;
                    movementY = -1f;
                    break;
                case RotationAdjustmentDirection.Down:
                    movementY = -1f;
                    break;
                case RotationAdjustmentDirection.DownLeft:
                    movementX = -1f;
                    movementY = -1f;
                    break;
                case RotationAdjustmentDirection.Left:
                    movementX = -1f;
                    break;
                case RotationAdjustmentDirection.UpLeft:
                    movementX = -1f;
                    movementY = 1f;
                    break;
            }

            player.Anim.SetFloat(MovementXHash, movementX);
            player.Anim.SetFloat(MovementYHash, movementY);
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
