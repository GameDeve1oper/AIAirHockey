// CameraFit.cs
using UnityEngine;

namespace AIAirHockey
{
    // Keeps the entire board width visible regardless of device aspect ratio.
    // A fixed orthographic size only shows boardHalfWidth on a specific aspect
    // ratio; taller/narrower phone screens show LESS width than that, letting
    // paddles clamp past where the camera can actually display them.
    // This grows the camera's size (never shrinks it) so the board's width
    // always fits, adding a bit of extra vertical breathing room on tall
    // screens instead of cropping anything horizontally.
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private float _designedSize = 5f;        // your current Editor value
        [SerializeField] private float _horizontalPadding = 0.2f; // small breathing room past the walls

        private void Awake()
        {
            var cam = GetComponent<Camera>();
            if (_config == null || !cam.orthographic) return;

            float requiredHalfWidth = _config.boardHalfWidth + _horizontalPadding;
            float requiredSize = requiredHalfWidth / cam.aspect;

            cam.orthographicSize = Mathf.Max(_designedSize, requiredSize);
        }
    }
}