using UnityEngine;

namespace ShiftedSignal.Garden.Buildable
{
    public class Fence : BaseBuildable
    {
        [SerializeField] private MeshRenderer[] Planks;

        private bool plank1Destroyed = false;
        private bool plank2Destroyed = false;

        public override void DoDamage(int damage)
        {
            base.DoDamage(damage);

            if (hp <= 0)
                return;

            if (hp <= .5f * MaxHP && !plank1Destroyed)
            {
                if (Planks.Length > 0 && Planks[0] != null)
                    Destroy(Planks[0].gameObject);

                plank1Destroyed = true;
            }

            if (hp <= .25f * MaxHP && !plank2Destroyed)
            {
                if (Planks.Length > 1 && Planks[1] != null)
                    Destroy(Planks[1].gameObject);

                plank2Destroyed = true;
            }
        }
    }
}