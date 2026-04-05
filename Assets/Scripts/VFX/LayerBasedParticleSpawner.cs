using System;
using System.Collections.Generic;
using UnityEngine;


    /// <summary>
    /// Spawns particle effects based on the layer of the hit object.
    /// </summary>
    public class LayerBasedParticleSpawner : MonoBehaviour
    {
        [Serializable]
        private struct LayerParticlePair
        {
            [SerializeField] public LayerMask Layer;
            [SerializeField] public GameObject ParticlePrefab;
        }

        [Header("Mappings")]
        [SerializeField] private List<LayerParticlePair> LayerParticles = new();

        [Header("Settings")]
        [SerializeField] private bool ParentToHitObject = false;

        /// <summary>
        /// Spawns a particle effect based on the layer of the hit collider.
        /// </summary>
        public void SpawnFromHit(RaycastHit hit)
        {
            Spawn(hit.collider.gameObject, hit.point, Quaternion.LookRotation(hit.normal));
        }

        /// <summary>
        /// Spawns a particle effect based on the object's layer.
        /// </summary>
        public void Spawn(GameObject target, Vector3 position, Quaternion rotation)
        {
            int targetLayer = target.layer;

            foreach (var pair in LayerParticles)
            {
                if ((pair.Layer.value & (1 << targetLayer)) != 0)
                {
                    GameObject instance = Instantiate(pair.ParticlePrefab, position, rotation);

                    if (ParentToHitObject)
                    {
                        instance.transform.SetParent(target.transform);
                    }

                    return;
                }
            }
        }
    }
