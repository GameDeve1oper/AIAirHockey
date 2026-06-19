// HitStop.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    // Briefly freezes time on strong impacts for a punchy feel.
    public class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }
        private void Awake() { Instance = this; }

        private bool _busy;

        public void Stop(float seconds)
        {
            if (_busy) return;
            StartCoroutine(StopRoutine(seconds));
        }

        private IEnumerator StopRoutine(float seconds)
        {
            _busy = true;
            float original = Time.timeScale;
            // Don't override a real pause.
            if (original > 0f)
            {
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(seconds);
                Time.timeScale = original;
            }
            _busy = false;
        }
    }
}