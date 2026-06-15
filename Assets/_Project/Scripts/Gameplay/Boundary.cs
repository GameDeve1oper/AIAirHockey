// Boundary.cs
using UnityEngine;

namespace AIAirHockey
{
    // Attached to each wall. Spawns a small spark + tiny shake on puck hits.
    public class Boundary : MonoBehaviour
    {
        [SerializeField] private GameObject _wallSparkPrefab; // pooled effect

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.layer != LayerMask.NameToLayer("Puck")) return;
            Vector2 point = collision.GetContact(0).point;
            if (_wallSparkPrefab != null && PoolManager.Exists)
                PoolManager.Instance.SpawnTimed(_wallSparkPrefab, point, Quaternion.identity, 0.6f);
        }
    }
}