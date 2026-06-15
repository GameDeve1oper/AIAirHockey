// PlayerPaddle.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIAirHockey
{
    public class PlayerPaddle : Paddle
    {
        private Camera _cam;
        private Vector2 _targetWorld;
        private bool _hasTouch;

        protected override void Awake()
        {
            base.Awake();
            _cam = Camera.main;
            _targetWorld = _rb.position;
        }

        private void Update()
        {
            ReadInput();
        }

        // Read finger (or mouse in editor) screen position -> world target.
        private void ReadInput()
        {
            Vector2 screenPos;
            bool active;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                active = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                screenPos = Mouse.current.position.ReadValue();
                active = true;
            }
            else
            {
                active = false;
                screenPos = Vector2.zero;
            }

            _hasTouch = active;
            if (active)
            {
                Vector3 world = _cam.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
                _targetWorld = new Vector2(world.x, world.y);
            }
        }

        private void FixedUpdate()
        {
            // If not touching, hold position (target stays where it was).
            MoveTo(_targetWorld, _config.playerPaddleMaxSpeed);
        }
    }
}