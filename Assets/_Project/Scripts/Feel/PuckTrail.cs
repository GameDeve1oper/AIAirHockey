// PuckTrail.cs
using UnityEngine;

namespace AIAirHockey
{
    // Add to the Puck. Drives a TrailRenderer based on speed and a glow
    // sprite that brightens at high speed.
    [RequireComponent(typeof(Puck))]
    public class PuckTrail : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private GameConfig _config;

        private Puck _puck;
        private void Awake() { _puck = GetComponent<Puck>(); }

        private void Update()
        {
            float speed = _puck.Velocity.magnitude;
            float t = Mathf.Clamp01(speed / _config.puckMaxSpeed);

            // Trail gets longer/brighter with speed.
            if (_trail != null) _trail.time = Mathf.Lerp(0.05f, 0.25f, t);

            // Glow alpha scales with speed.
            if (_glow != null)
            {
                Color c = _glow.color;
                c.a = Mathf.Lerp(0.15f, 0.8f, t);
                _glow.color = c;
            }
        }
    }
}
