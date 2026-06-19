// ScalePunch.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    // Pops an object's scale up then back. Call Punch() on impact.
    public class ScalePunch : MonoBehaviour
    {
        [SerializeField] private float _amount = 0.25f;   // how big the pop
        [SerializeField] private float _duration = 0.18f;
        private Vector3 _baseScale;
        private Coroutine _routine;

        private void Awake() { _baseScale = transform.localScale; }

        public void Punch()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float t = 0f;
            while (t < _duration)
            {
                t += Time.deltaTime;
                float k = t / _duration;
                // Up fast (0->1 of amount) then back, via sine arc.
                float s = Mathf.Sin(k * Mathf.PI) * _amount;
                transform.localScale = _baseScale * (1f + s);
                yield return null;
            }
            transform.localScale = _baseScale;
        }
    }
}