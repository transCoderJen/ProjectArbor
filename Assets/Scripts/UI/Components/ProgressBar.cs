using System;
using UnityEngine;

namespace ShiftedSignal.Garden.UserInterface.Components
{
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private RectTransform mask;
        [SerializeField] private Vector2 padding;
        private RectTransform maskParentRectTransform;
        
        [SerializeField] [Range(0,1)] private float progress;

        private void Update()
        {
            SetProgress(progress);
        }
        
        private void Awake()
        {
            if (mask == null)
            {
                Debug.LogError($"Progress bar {name} is missing a mask!  This progress bar will not work!");
                return;
            }

            maskParentRectTransform = mask.parent.GetComponent<RectTransform>();
        }

        public void SetProgress(float progress)
        {
           Vector2 parentSize = maskParentRectTransform.sizeDelta;
           Vector2 targetSize = parentSize;

           targetSize.x *= Mathf.Clamp01(progress);

           mask.offsetMin = Vector2.zero + padding;
           mask.offsetMax = targetSize - parentSize - padding;
        }
    }
}