using UnityEngine;
using ShiftedSignal.Garden.Managers;
using UnityEngine.InputSystem;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{    
    public class PlayerAttackState : PlayerState
    {
        private int comboCounter;
        public float lastTimeAttacked;
        private float comboWindow = .35f;
        private bool attackInputCached = false;

        public PlayerAttackState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Player.AttackBuffered = false;

            if (comboCounter > 2 || Time.time >= lastTimeAttacked + comboWindow)
                comboCounter = 0;

            Player.Anim.SetInteger("ComboCounter", comboCounter);

            Vector3 AttackDir = Player.FacingDir;

            Vector2 moveInput = Player.MoveInput.action.ReadValue<Vector2>().normalized;

            if (moveInput != Vector2.zero)
            {
                Debug.Log("Changing attack direction");
                AttackDir = new Vector3(moveInput.x, 0f, moveInput.y);
            }

            Player.ApplyMovement(new Vector2(Player.AttackMovement[comboCounter].x * AttackDir.x,
                                                Player.AttackMovement[comboCounter].x * AttackDir.z), normalized: false);
            
            Player.TryCutGrass(Player.LastFacingDir);

            SpawnSlashFX();

            StateTimer = .15f;
        }

        private void SpawnSlashFX()
        {
            float scale;
            Vector3 rotation;
            if (comboCounter == 0)
            {
                rotation = new Vector3(0, -90, 0);
                scale = 2;
            }
            else if (comboCounter == 1)
            {
                rotation = new Vector3(180, -90, 0);
                scale = 2.2f;
            }
            else
            {
                rotation = new Vector3(90, -90, 0);
                scale = 2.5f;
            }
            ObjectPoolManager.SpawnObject(Player.EquippedWeapon.SlashFX,
                                            Player.transform.position + new Vector3(0, Player.CheckHeight, 0),
                                            Quaternion.LookRotation(Player.FacingDir) * Quaternion.Euler(rotation.x, rotation.y, rotation.z),
                                            Player.transform, scale: scale);
        }

        public override void Update()
        {
            base.Update();

            if (Player.AttackInput.action.WasPressedThisFrame())
            {
                Player.AttackBuffered = true;
            }
            if (StateTimer < 0)
                Player.StopMovement();
            
            if (TriggerCalled)
                Player.StateMachine.ChangeState(Player.IdleState);
        }

        public override void Exit()
        {
            base.Exit();

            comboCounter++;
            Debug.Log("Combo Counter at: " + comboCounter);
            lastTimeAttacked = Time.time;
        }

        public int getComboCounter() => comboCounter;
    }
}
