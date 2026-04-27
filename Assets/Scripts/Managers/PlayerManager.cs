using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.Managers
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        public Player Player;

        public void ResetPlayer()
        {
            Player.gameObject.SetActive(false);
            Player.gameObject.SetActive(true);
        }
    }
}