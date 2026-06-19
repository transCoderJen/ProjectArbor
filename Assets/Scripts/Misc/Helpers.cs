using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShiftedSignal.Garden.Misc
{
    public static class Helpers 
    {
        private static float minClickInterval = 0.15f; // 150 ms, tweak in Inspector
        private static float _lastClickTime = -Mathf.Infinity;

        private static Camera _camera;

        public static Camera Camera
        {
            get {
                if (_camera == null) _camera = Camera.main;
                return _camera;
            }
            
        }

        public static bool DebounceClicks()
        {
            float now = Time.unscaledTime; // use unscaled so it's not affected by pause/timeScale
            if (now - _lastClickTime < minClickInterval)
            {
                Debug.Log("Ignoring rapid duplicate click.");
                return false;
            }
            _lastClickTime = now;
            return true;
        }
        
        private static readonly Dictionary<float, WaitForSeconds> WaitDictionary = new Dictionary<float, WaitForSeconds>();

        public static WaitForSeconds GetWait(float time)
        {
            if (WaitDictionary.TryGetValue(time, out var wait)) return wait;

            WaitDictionary[time] = new WaitForSeconds(time);
            return WaitDictionary[time];
        }

        private static readonly List<RaycastResult> Results = new();
        private static PointerEventData eventDataCurrentPosition;

        public static bool IsOverUI()
        {
            if (EventSystem.current == null)
                return false;

            eventDataCurrentPosition ??=
                new PointerEventData(EventSystem.current);

            eventDataCurrentPosition.position = Input.mousePosition;

            Results.Clear();

            EventSystem.current.RaycastAll(
                eventDataCurrentPosition,
                Results);

            return Results.Count > 0;
        }

        public static Vector2 GetWorldPositionOfCanvasElement(RectTransform element)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(element, element.position, Camera, out var result);
            return result;
        }

        public static void DeleteChildren(this Transform parent)
        {
            foreach (Transform child in parent)
            {
                Object.Destroy(child.gameObject);
            }
        }

        public static void DeleteChildrenInEditor(this Transform parent)
        {
            foreach (Transform child in parent)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        public static void Fade(this SpriteRenderer sr, float alpha) 
        {
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
        }

        public static void Fade(this Image image, float alpha) 
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public static void DontFlip(this Transform transform)
        {
            if (transform.localScale.x < 0)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }

        public static bool EveryXFrames(int interval)
        {
            if (interval <= 0)
                return false;

            return Time.frameCount % interval == 0;
        }
    }
}