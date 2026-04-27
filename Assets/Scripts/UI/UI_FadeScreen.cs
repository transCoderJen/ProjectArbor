using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_FadeScreen : MonoBehaviour
    {
        private Animator anim;

        // Start is called before the first frame update
        void Start()
        {
            anim = GetComponent<Animator>();
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        public void FadeOut() => anim.SetTrigger("fadeOut");
        public void FadeIn() => anim.SetTrigger("fadeIn");
    }
}