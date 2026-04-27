using ShiftedSignal.Garden.Managers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{    
    public class PlayerManagementState : PlayerState
    {

        public PlayerManagementState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();
            CameraManager.Instance.SwitchCamera(CameraManager.VirtualCameraType.FreeLook);
            CameraManager.Instance.ResetOffsets();
            Player.StopMovement();
            
        }
        public override void Update()
        {
            base.Update();

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                Player.StateMachine.ChangeState(Player.IdleState);
                return;
            }

            // ToggleToolTipVisibility();

            // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // if (Physics.Raycast(ray, out RaycastHit hit))
            // {
            //     Vector3 worldPosition = hit.point; // This is the exact world position
            //     worldPosition.y = .27f;

            //     var cellSize = GridManager.Instance.CellSize;

            //     worldPosition = new Vector3(
            //         Mathf.Floor(worldPosition.x / cellSize) * cellSize + (cellSize / 2),
            //         worldPosition.y,
            //         Mathf.Floor(worldPosition.z / cellSize) * cellSize + (cellSize / 2)
            //     );
            //     player.toolIndicator.position = worldPosition;
            // }

        }

        private void ToggleToolTipVisibility()
        {
            if (Player.GetBlock() == null ||Player.GetBlock().PreventUse)
            {
                Player.ToolIndicator.gameObject.SetActive(false);
            }
            else
            {
                Player.ToolIndicator.gameObject.SetActive(true);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override void Exit()
        {
            base.Exit();
            CameraManager.Instance.ResetOffsetsAndSwitchCamera(CameraManager.VirtualCameraType.Player);
            // player.ToolIndicator.gameObject.SetActive(false);
            
        }
    }
}
