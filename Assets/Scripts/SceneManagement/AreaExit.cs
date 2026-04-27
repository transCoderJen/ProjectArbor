using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEditor.MPE;
using UnityEngine;

namespace ShiftedSignal.Garden.SceneManagement
{  
    public class AreaExit : MonoBehaviour
    {
        // [SerializeField] private SceneAsset sceneAsset; // Drag scene here in the inspector
        [SerializeField] private string SceneTransitionName;

        [SerializeField] private string TargetEntranceName;
        [SerializeField] TransitionType TransitionType;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Player player))
            {
                player.StateMachine.ChangeState(player.IdleState);

                // StartCoroutine(player.BusyFor(1f));

                LevelLoader.Instance.LoadScene(
                    SceneTransitionName,
                    TargetEntranceName,
                    TransitionType
                );
            }
        }
    }
}
