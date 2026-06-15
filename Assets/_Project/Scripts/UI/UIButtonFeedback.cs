// UIButtonFeedback.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIAirHockey
{
    // Add to any Button. Scales down on press, pops back, plays click sound.
    [RequireComponent(typeof(Button))]
    public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _pressScale = 0.92f;
        [SerializeField] private float _speed = 14f;
        private Vector3 _baseScale;
        private Vector3 _targetScale;

        private void Awake() { _baseScale = transform.localScale; _targetScale = _baseScale; }

        public void OnPointerDown(PointerEventData e)
        {
            _targetScale = _baseScale * _pressScale;
            if (AudioManager.Exists) AudioManager.Instance.Play(SoundId.ButtonClick);
        }
        public void OnPointerUp(PointerEventData e) { _targetScale = _baseScale; }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, _targetScale, _speed * Time.unscaledDeltaTime);
        }
    }
}