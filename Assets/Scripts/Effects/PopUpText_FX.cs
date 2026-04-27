using TMPro;
using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    /// <summary>
    /// Floating popup text effect for 3D world-space text.
    /// </summary>
    public class PopUpTextFX : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshPro myText;
        [SerializeField] private Camera targetCamera;

        [Header("Movement")]
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float disappearingSpeed = 3f;

        [Header("Fade")]
        [SerializeField] private float colorDisappearingSpeed = 2f;
        [SerializeField] private float lifeTime = 1f;

        private float textTimer;

        private void Awake()
        {
            if (myText == null)
                myText = GetComponent<TextMeshPro>();

            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Start()
        {
            textTimer = lifeTime + Random.Range(-0.3f, 0.3f);
            speed += Random.Range(-0.5f, 0.5f);

            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;

                transform.localScale = new Vector3(
                    1f / parentScale.x,
                    1f / parentScale.y,
                    1f / parentScale.z
                );
            }
        }

        private void Update()
        {
            FaceCamera();
            MoveUp(speed);

            textTimer -= Time.deltaTime;

            if (textTimer > 0f)
                return;

            FadeOut();

            if (myText.color.a < 0.5f)
                MoveUp(disappearingSpeed);

            if (myText.color.a <= 0f)
                Destroy(gameObject);
        }

        private void FaceCamera()
        {
            if (targetCamera == null)
                return;

            transform.rotation = Quaternion.LookRotation(
                transform.position - targetCamera.transform.position
            );
        }

        private void MoveUp(float moveSpeed)
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }

        private void FadeOut()
        {
            Color color = myText.color;
            color.a -= colorDisappearingSpeed * Time.deltaTime;
            color.a = Mathf.Clamp01(color.a);
            myText.color = color;
        }
    }
}