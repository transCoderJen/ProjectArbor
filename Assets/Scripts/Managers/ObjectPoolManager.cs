using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    /// <summary>
    /// Manages pooled GameObjects using enum-based lookup instead of prefab names.
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform ObjectPoolEmptyHolder;

        [Header("Pool Setup")]
        [SerializeField] private PooledObject[] PooledObjects;

        private static readonly Dictionary<PooledObjectList, PoolRuntimeData> Pools = new();
        private static readonly Dictionary<GameObject, PooledObjectList> InstanceLookup = new();

        protected override void Awake()
        {
            base.Awake();
            InitializePools();
        }

        /// <summary>
        /// Creates all configured pools and prewarms them.
        /// </summary>
        private void InitializePools()
        {
            Pools.Clear();
            InstanceLookup.Clear();

            if (ObjectPoolEmptyHolder == null)
            {
                GameObject root = new GameObject("Pooled Objects");
                ObjectPoolEmptyHolder = root.transform;
                ObjectPoolEmptyHolder.SetParent(transform);
            }

            foreach (PooledObject pooledObject in PooledObjects)
            {
                if (pooledObject.Prefab == null)
                {
                    Debug.LogWarning($"Pool entry for {pooledObject.Type} has no prefab assigned.", this);
                    continue;
                }

                if (Pools.ContainsKey(pooledObject.Type))
                {
                    Debug.LogWarning($"Duplicate pool type found: {pooledObject.Type}. Skipping duplicate.", this);
                    continue;
                }

                GameObject groupObject = new GameObject(pooledObject.Type.ToString());
                groupObject.transform.SetParent(ObjectPoolEmptyHolder);

                PoolRuntimeData runtimeData = new PoolRuntimeData(
                    pooledObject.Type,
                    pooledObject.Prefab,
                    groupObject.transform
                );

                Pools.Add(pooledObject.Type, runtimeData);

                for (int i = 0; i < pooledObject.InitialSize; i++)
                {
                    GameObject instance = CreateNewInstance(runtimeData);
                    ReturnObjectToPool(instance);
                }
            }
        }

        /// <summary>
        /// Spawns an object from the specified pool type.
        /// </summary>
        public static GameObject SpawnObject(PooledObjectList poolType, Vector3 position, Quaternion rotation, Transform parent = null, float scale = 1)
        {
            if (!Pools.TryGetValue(poolType, out PoolRuntimeData pool))
            {
                Debug.LogWarning($"No pool found for type: {poolType}");
                return null;
            }

            GameObject instance = null;

            while (pool.InactiveObjects.Count > 0 && instance == null)
            {
                instance = pool.InactiveObjects.Dequeue();
            }

            if (instance == null)
            {
                instance = CreateNewInstance(pool);
            }
            
            Transform instanceTransform = instance.transform;
            instanceTransform.localScale = new Vector3(scale, scale, scale);
            

            if (parent != null)
                instanceTransform.SetParent(parent);
            else
                instanceTransform.SetParent(pool.Parent);
            
                
            instanceTransform.SetPositionAndRotation(position, rotation);
            
            instance.SetActive(true);

            return instance;
        }

        /// <summary>
        /// Returns an object to its pool.
        /// </summary>
        public static void ReturnObjectToPool(GameObject obj)
        {
            if (obj == null)
                return;

            if (!InstanceLookup.TryGetValue(obj, out PooledObjectList poolType))
            {
                Debug.LogWarning($"Trying to return non-pooled object: {obj.name}");
                obj.SetActive(false);
                return;
            }

            if (!Pools.TryGetValue(poolType, out PoolRuntimeData pool))
            {
                Debug.LogWarning($"Pool runtime data missing for type: {poolType}");
                obj.SetActive(false);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(pool.Parent);
            pool.InactiveObjects.Enqueue(obj);
        }

        /// <summary>
        /// Deactivates and returns every pooled object currently in the scene hierarchy.
        /// </summary>
        public void ResetPooledObjects()
        {
            foreach (PoolRuntimeData pool in Pools.Values)
            {
                for (int i = pool.Parent.childCount - 1; i >= 0; i--)
                {
                    Transform child = pool.Parent.GetChild(i);
                    ReturnObjectToPool(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Creates a new pooled instance for the given pool.
        /// </summary>
        private static GameObject CreateNewInstance(PoolRuntimeData pool)
        {
            GameObject instance = Instantiate(pool.Prefab, pool.Parent);
            instance.SetActive(false);

            if (!InstanceLookup.ContainsKey(instance))
            {
                InstanceLookup.Add(instance, pool.Type);
            }

            return instance;
        }

        [ContextMenu("Rebuild Pools")]
        private void RebuildPools()
        {
            if (ObjectPoolEmptyHolder != null)
            {
                for (int i = ObjectPoolEmptyHolder.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(ObjectPoolEmptyHolder.GetChild(i).gameObject);
                }
            }

            InitializePools();
        }
    }

    /// <summary>
    /// Editor-facing configuration for a single pool.
    /// </summary>
    [Serializable]
    public struct PooledObject
    {
        [Header("Pool Identity")]
        public PooledObjectList Type;

        [Header("Prefab")]
        public GameObject Prefab;

        [Header("Prewarm Count")]
        [Min(0)] public int InitialSize;
    }

    /// <summary>
    /// Enum used to identify each pool.
    /// </summary>
    public enum PooledObjectList
    {
        None,
        Bullet,
        EnemyProjectile,
        SlashBlue,
        SlashRed,
        HitBubbles,
        HitRedSparks,
        Pickup
    }

    /// <summary>
    /// Internal runtime data for each configured pool.
    /// </summary>
    public class PoolRuntimeData
    {
        public PooledObjectList Type;
        public GameObject Prefab;
        public Transform Parent;
        public Queue<GameObject> InactiveObjects;

        public PoolRuntimeData(PooledObjectList type, GameObject prefab, Transform parent)
        {
            Type = type;
            Prefab = prefab;
            Parent = parent;
            InactiveObjects = new Queue<GameObject>();
        }
    }
}