
using System.Collections.Generic;
using UnityEngine;
using ShiftedSignal.Garden.Managers;

namespace GAP_ParticleSystemController
{
    [System.Serializable]
    public class ParticleSystemOriginalSettings
    {
        public SerializableMinMaxGradient _startColor;
        public SerializableMinMaxGradient _colorOverLifetimeC;
        public SerializableMinMaxCurve _startSize;
        public SerializableMinMaxCurve _startSizeX;
        public SerializableMinMaxCurve _startSizeY;
        public SerializableMinMaxCurve _startSizeZ;
        public SerializableMinMaxCurve _startSpeed;
        public SerializableMinMaxCurve _startDelay;
        public SerializableMinMaxCurve _startLifetime;
        public SerializableMinMaxCurve _velocityOverLifetimeX;
        public SerializableMinMaxCurve _velocityOverLifetimeY;
        public SerializableMinMaxCurve _velocityOverLifetimeZ;
        public SerializableVector3 _localPosition;
        public SerializableGradient _trailGradient;
        public float _duration;
        public float _shapeRadius;
        public float _trailWidthMultiplier;
        public float _trailTime;
        public bool _active;
        public bool _loop;
        public bool _prewarm;
    }

    [ExecuteInEditMode]
    public class ParticleSystemController : MonoBehaviour
    {
        [SerializeField] GameObject hitFX;
        public float size = 1;
        public float speed = 1;
        public float simulationSpeed = 1; // directly under speed
        public float duration = 1;
        public bool loop;
        public bool prewarm;
        public bool lights;
        public bool trails;
        public bool changeColor;
        public Color newMaxColor = new Color(0, 0, 0, 1);
        public Color newMinColor = new Color(0, 0, 0, 1);
        public List<GameObject> ParticleSystems = new List<GameObject>();
        public List<bool> ActiveParticleSystems = new List<bool>();

        private List<ParticleSystemOriginalSettings> psOriginalSettingsList = new List<ParticleSystemOriginalSettings>();

        private void OnValidate()
        {
            ApplySimulationSpeed();
        }

        private void ApplySimulationSpeed()
        {
            if (ParticleSystems == null || ParticleSystems.Count == 0)
            {
                return;
            }

            float clampedSimulationSpeed = Mathf.Max(0f, simulationSpeed);

            for (int i = 0; i < ParticleSystems.Count; i++)
            {
                if (ParticleSystems[i] == null)
                {
                    continue;
                }

                ParticleSystem ps = ParticleSystems[i].GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = ps.main;
                main.simulationSpeed = clampedSimulationSpeed;
            }
        }

        public void UpdateParticleSystem()
        {
            // your existing code...
        }

        public void ChangeColorOnly()
        {
            // your existing code...
        }

        public void ResizeOnly()
        {
            // your existing code...
        }

        public void ResetParticleSystem()
        {
            if (hitFX != null)
            {
                hitFX.SetActive(false);
            }
        }

        public Color ChangeHUE(Color oldColor, Color newColor)
        {
            float newHue;
            float newSaturation;
            float newValue;
            float oldHue;
            float oldSaturation;
            float oldValue;
            float originalAlpha = oldColor.a;
            Color.RGBToHSV(newColor, out newHue, out newSaturation, out newValue);
            Color.RGBToHSV(oldColor, out oldHue, out oldSaturation, out oldValue);
            var updatedColor = Color.HSVToRGB(newHue, oldSaturation, oldValue);
            updatedColor.a = originalAlpha;
            return updatedColor;
        }

        public Gradient ChangeGradientColor(Gradient oldGradient, Color newMaxColor, Color newMinColor)
        {
            GradientColorKey[] colorKeys = new GradientColorKey[oldGradient.colorKeys.Length];
            for (int j = 0; j < oldGradient.colorKeys.Length; j++)
            {
                colorKeys[j].time = oldGradient.colorKeys[j].time;
                if (j % 2 == 0)
                    colorKeys[j].color = ChangeHUE(oldGradient.colorKeys[j].color, newMaxColor);
                if (j % 2 == 1)
                    colorKeys[j].color = ChangeHUE(oldGradient.colorKeys[j].color, newMinColor);
            }
            oldGradient.SetKeys(colorKeys, oldGradient.alphaKeys);
            return oldGradient;
        }

        public void FillLists()
        {
            if (ParticleSystems.Count == 0)
            {
                var ps = GetComponent<ParticleSystem>();
                var trail = GetComponent<TrailRenderer>();
                if (ps != null || trail != null)
                    ParticleSystems.Add(gameObject);

                AddChildRecurvsively(transform);

                for (int i = 0; i < ParticleSystems.Count; i++)
                {
                    ActiveParticleSystems.Add(true);
                }

                ApplySimulationSpeed();
            }
            else
            {
                Debug.Log("Lists already have GameObjects. For automatic filling consider emptying the lists and try again.");
            }
        }

        public void EmptyLists()
        {
            ParticleSystems.Clear();
            ActiveParticleSystems.Clear();
        }

        void AddChildRecurvsively(Transform transf)
        {
            foreach (Transform t in transf)
            {
                var child = t.gameObject;
                var psChild = child.GetComponent<ParticleSystem>();
                var trailChild = child.GetComponent<TrailRenderer>();
                if (psChild != null || trailChild != null)
                    ParticleSystems.Add(child);
                if (child.transform.childCount > 0)
                    AddChildRecurvsively(child.transform);
            }
        }


        public void ActivateHitParticles()
        {
            hitFX.SetActive(true);
        }

        void OnParticleSystemStopped()
        {
            ObjectPoolManager.ReturnObjectToPool(this.gameObject);
        }
    }
}