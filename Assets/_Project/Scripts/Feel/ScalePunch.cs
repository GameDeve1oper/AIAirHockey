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
        private Paddle _paddle;
        private Coroutine _routine;

        private void Awake() 
        { 
            _paddle = GetComponent<Paddle>();
        }

        public void Punch()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PunchRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float t = 0f;
            Vector3 baseScale = _paddle != null ? _paddle.BaseScale : new Vector3(0.07f, 0.07f, 0.07f);
            while (t < _duration)
            {
                t += Time.deltaTime;
                float k = t / _duration;
                float baseMult = _paddle != null ? _paddle.ScaleModifier : 1.0f;
                float s = Mathf.Sin(k * Mathf.PI) * _amount;
                transform.localScale = baseScale * baseMult * (1f + s);
                yield return null;
            }
            transform.localScale = baseScale * (_paddle != null ? _paddle.ScaleModifier : 1.0f);
        }
    }
}