// PoolManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace AIAirHockey
{
    public class PoolManager : Singleton<PoolManager>
    {
        // One reusable pool per prefab.
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools =
            new Dictionary<GameObject, Queue<GameObject>>();

        // Remembers which prefab each spawned object came from, so we can
        // return it to the correct pool.
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab =
            new Dictionary<GameObject, GameObject>();

        // Pre-create 'count' copies of a prefab so the first use has no hitch.
        public void Prewarm(GameObject prefab, int count)
        {
            if (!_pools.ContainsKey(prefab))
                _pools[prefab] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                _instanceToPrefab[obj] = prefab;
                _pools[prefab].Enqueue(obj);
            }
        }

        // Get an object from the pool (or create one if empty).
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(prefab))
                _pools[prefab] = new Queue<GameObject>();

            GameObject obj;
            if (_pools[prefab].Count > 0)
                obj = _pools[prefab].Dequeue();
            else
            {
                obj = Instantiate(prefab, transform);
                _instanceToPrefab[obj] = prefab;
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        // Return an object to its pool for reuse.
        public void Despawn(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (_instanceToPrefab.TryGetValue(obj, out GameObject prefab))
                _pools[prefab].Enqueue(obj);
            else
                Destroy(obj); // not from a pool; just destroy
        }

        // Convenience: spawn, then auto-despawn after 'life' seconds.
        public GameObject SpawnTimed(GameObject prefab, Vector3 pos, Quaternion rot, float life)
        {
            GameObject obj = Spawn(prefab, pos, rot);
            StartCoroutine(DespawnAfter(obj, life));
            return obj;
        }

        private System.Collections.IEnumerator DespawnAfter(GameObject obj, float life)
        {
            yield return new WaitForSeconds(life);
            Despawn(obj);
        }
    }
}