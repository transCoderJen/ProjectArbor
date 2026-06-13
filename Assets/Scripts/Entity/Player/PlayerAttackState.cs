using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.EntitySpace.PlayerSpace
{
    public class PlayerAttackState : PlayerState
    {
        private int comboCounter;
        public float lastTimeAttacked;

        private readonly float comboWindow = 0.35f;

        private readonly PooledObjectList[] slashFXByCombo =
        {
            PooledObjectList.SlashRed,
            PooledObjectList.SlashRed,
            PooledObjectList.SlashRed
        };

        public PlayerAttackState(
            Player player,
            PlayerStateMachine stateMachine,
            string animBoolName) : base(player, stateMachine, animBoolName)
        {
        }

        public override void Enter()
        {
            base.Enter();

            Player.AttackBuffered = false;

            if (comboCounter > 2 || Time.time >= lastTimeAttacked + comboWindow)
                comboCounter = 0;

            Player.Anim.SetInteger("ComboCounter", comboCounter);

            Vector3 attackDir = Player.FacingDir;

            Vector2 moveInput = Player.CachedMoveInput.normalized;

            if (moveInput != Vector2.zero)
            {
                attackDir = new Vector3(moveInput.x, 0f, moveInput.y);
                Player.SetFacingDirection(attackDir);
            }

            Vector2 attackMovement = new Vector2(
                Player.AttackMovement[comboCounter].x * attackDir.x,
                Player.AttackMovement[comboCounter].x * attackDir.z);

            Player.ApplyMovement(attackMovement, normalized: false);

            SpawnSlashFX();

            StateTimer = 0.15f;
        }

        private void SpawnSlashFX()
        {
            if (Player.FacingDir == Vector3.zero)
                return;

            float scale;
            Vector3 rotation;

            if (comboCounter == 0)
            {
                rotation = new Vector3(0f, -90f, 0f);
                scale = 2f;
            }
            else if (comboCounter == 1)
            {
                rotation = new Vector3(180f, -90f, 0f);
                scale = 2.2f;
            }
            else
            {
                rotation = new Vector3(90f, -90f, 0f);
                scale = 2.5f;
            }

            PooledObjectList slashFX = GetSlashFXForCombo(comboCounter);

            ObjectPoolManager.SpawnObject(
                slashFX,
                Player.transform.position + new Vector3(0f, Player.CheckHeight, 0f),
                Quaternion.LookRotation(Player.FacingDir) *
                Quaternion.Euler(rotation.x, rotation.y, rotation.z),
                Player.transform,
                scale: scale);
        }

        private PooledObjectList GetSlashFXForCombo(int comboIndex)
        {
            if (comboIndex < 0 || comboIndex >= slashFXByCombo.Length)
                return slashFXByCombo[0];

            return slashFXByCombo[comboIndex];
        }

        public override void Update()
        {
            base.Update();

            if (StateTimer < 0)
                Player.StopMovement();

            if (TriggerCalled)
                Player.StateMachine.ChangeState(Player.IdleState);
        }

        public override void Exit()
        {
            base.Exit();

            comboCounter++;
            lastTimeAttacked = Time.time;
        }

        public int GetComboCounter() => comboCounter;
    }
}