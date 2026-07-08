using System;
using System.Collections.Generic;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

namespace ShiftedSignal.Garden.Managers
{
    /// <summary>
    /// Manages pooled GameObjects using enum-based lookup and runtime prefab lookup.
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform ObjectPoolEmptyHolder;

        [Header("Pool Setup")]
        [SerializeField] private PooledObject[] PooledObjects;

        private static readonly Dictionary<PooledObjectList, PoolRuntimeData> Pools = new();
        private static readonly Dictionary<GameObject, PoolRuntimeData> InstanceLookup = new();
        private static readonly Dictionary<GameObject, PoolRuntimeData> RuntimePools = new();

        protected override void Awake()
        {
            base.Awake();
            InitializePools();
        }

        private void InitializePools()
        {
            Pools.Clear();
            InstanceLookup.Clear();
            RuntimePools.Clear();

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
        /// Spawns an object from the specified enum-based pool type.
        /// </summary>
        public static GameObject SpawnObject(
            PooledObjectList poolType,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            float scale = 1f)
        {
            if (!Pools.TryGetValue(poolType, out PoolRuntimeData pool))
            {
                Debug.LogWarning($"No pool found for type: {poolType}");
                return null;
            }

            return SpawnFromPool(pool, position, rotation, parent, scale);
        }

        /// <summary>
        /// Spawns an object directly from a prefab.
        /// If no runtime pool exists for this prefab, one is created automatically.
        /// </summary>
        public static GameObject SpawnObject(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            float scale = 1f)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Tried to spawn a null prefab.");
                return null;
            }

            if (!RuntimePools.TryGetValue(prefab, out PoolRuntimeData pool))
            {
                pool = CreateRuntimePool(prefab);
                RuntimePools.Add(prefab, pool);
            }

            return SpawnFromPool(pool, position, rotation, parent, scale);
        }

        private static GameObject SpawnFromPool(
            PoolRuntimeData pool,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            float scale)
        {
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
            instanceTransform.localScale = Vector3.one * scale;

            instanceTransform.SetParent(parent != null ? parent : pool.Parent);
            instanceTransform.SetPositionAndRotation(position, rotation);

            instance.SetActive(true);

            return instance;
        }

        /// <summary>
        /// Returns an object to its original pool.
        /// </summary>
        public static void ReturnObjectToPool(GameObject obj)
        {
            if (obj == null)
                return;

            if (!InstanceLookup.TryGetValue(obj, out PoolRuntimeData pool))
            {
                Debug.LogWarning($"Trying to return non-pooled object: {obj.name}");
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
                ReturnPoolChildren(pool);
            }

            foreach (PoolRuntimeData pool in RuntimePools.Values)
            {
                ReturnPoolChildren(pool);
            }
        }

        private static void ReturnPoolChildren(PoolRuntimeData pool)
        {
            for (int i = pool.Parent.childCount - 1; i >= 0; i--)
            {
                Transform child = pool.Parent.GetChild(i);

                if (child.gameObject.activeInHierarchy)
                {
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
                InstanceLookup.Add(instance, pool);
            }

            return instance;
        }

        /// <summary>
        /// Creates a runtime pool for a prefab that was not configured in the inspector.
        /// </summary>
        private static PoolRuntimeData CreateRuntimePool(GameObject prefab)
        {
            GameObject groupObject = new GameObject($"{prefab.name}_RuntimePool");

            if (Instance != null && Instance.ObjectPoolEmptyHolder != null)
            {
                groupObject.transform.SetParent(Instance.ObjectPoolEmptyHolder);
            }

            return new PoolRuntimeData(
                PooledObjectList.None,
                prefab,
                groupObject.transform
            );
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

    public enum PooledObjectList
    {
        None,
        RedArrowProjectile,
        EnemyProjectile,
        SlashBlue,
        SlashRed,
        HitBubbles,
        HitRedSparks,
        Pickup,
        HealArea,
        HeartRoot,
        WolfEnemy,
        VillagerEnemy,
        BlueCometProjectile,
        PinkTrailProjectile,
        Explosion360,
        ExplosionCircular,
        ExplosionVertical,
        OrangeCometProjectile

    }

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