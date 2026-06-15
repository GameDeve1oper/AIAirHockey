// SafeArea.cs
using UnityEngine;

namespace AIAirHockey
{
    // Put this on a full-screen RectTransform child of the Canvas.
    // It resizes itself to the device's safe area (avoids notches).
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _lastSafe;

        private void Awake() { _rt = GetComponent<RectTransform>(); Apply(); }
        private void Update() { if (Screen.safeArea != _lastSafe) Apply(); }

        private void Apply()
        {
            _lastSafe = Screen.safeArea;
            Vector2 min = _lastSafe.position;
            Vector2 max = _lastSafe.position + _lastSafe.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;
            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}