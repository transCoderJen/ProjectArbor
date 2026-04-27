using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using UnityEngine;

public class DiscoTime : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;

    private void OnTriggerEnter(Collider collision)
    {
        // Debug.Log("Disco deactivated");
        // if (audioSource != null)
        // {
        //     audioSource.Play();
        // }
    }

    private void OnTriggerExit(Collider collision)
    {
        
            // Debug.Log("Disco Activated");
            // if (audioSource != null)
            // {
            //     audioSource.Stop();
            // }
            
        
    }
}
