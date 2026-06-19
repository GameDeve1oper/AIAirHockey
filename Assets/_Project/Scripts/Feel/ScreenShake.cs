// ScreenShake.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    // Attach to the Gameplay Main Camera. Shakes on goals + hard hits.
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance { get; private set; }

        private Vector3 _basePos;
        private Coroutine _routine;

        private void Awake()
        {
            Instance = this;
            _basePos = transform.localPosition;
        }

        private void OnEnable() { EventBus.OnGoalScored += OnGoal; }
        private void OnDisable() { EventBus.OnGoalScored -= OnGoal; }

        private void OnGoal(PlayerSide s) => Shake(0.35f, 0.4f);

        // duration seconds, magnitude in world units.
        public void Shake(float magnitude, float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShakeRoutine(magnitude, duration));
        }

        private IEnumerator ShakeRoutine(float magnitude, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float damper = 1f - (t / duration); // fade out
                Vector2 offset = Random.insideUnitCircle * magnitude * damper;
                transform.localPosition = _basePos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            transform.localPosition = _basePos;
        }
}
}