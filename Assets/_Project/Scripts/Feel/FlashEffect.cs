// FlashEffect.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    // Briefly tints a SpriteRenderer white on impact, then restores.
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlashEffect : MonoBehaviour
    {
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _duration = 0.1f;
        private SpriteRenderer _sr;
        private Color _base;
        private Coroutine _routine;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _base = _sr.color;
        }

        public void Flash()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _sr.color = _flashColor;
            yield return new WaitForSeconds(_duration);
            _sr.color = _base;
        }
    }
}