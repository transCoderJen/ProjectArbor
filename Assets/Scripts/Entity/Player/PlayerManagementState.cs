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
            Time.timeScale = 0;
            CameraManager.Instance.SwitchCamera(CameraManager.VirtualCameraType.FreeLook);
            CameraManager.Instance.ResetOffsets();
            Player.StopMovement();
            Player.InManagementState = true;
            
        }
        public override void Update()
        {
            base.Update();

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                Player.StateMachine.ChangeState(Player.IdleState);
                return;
            }
        }
        
        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override void Exit()
        {
            base.Exit();
            Player.InManagementState = false;
            Time.timeScale = 1;
            CameraManager.Instance.ResetOffsetsAndSwitchCamera(CameraManager.VirtualCameraType.Player);         
        }
    }
}
