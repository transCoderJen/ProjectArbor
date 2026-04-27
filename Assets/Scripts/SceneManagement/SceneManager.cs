
using ShiftedSignal.Garden.Misc;

namespace ShiftedSignal.Garden.SceneManagement
{
    public class SceneManager : Singleton<SceneManager>
    {
        public string SceneTransitionName { get; private set; }

        public void SetTransitionName(string transitionName)
        {
            SceneTransitionName = transitionName;
        }
    }
}
