// Goal.cs
using UnityEngine;

namespace AIAirHockey
{
    // Attached to GoalTop and GoalBottom trigger zones.
    // '_side' is the side that DEFENDS this goal. When the puck enters,
    // the OTHER side scored.
    public class Goal : MonoBehaviour
    {
        [SerializeField] private PlayerSide _side = PlayerSide.Top;

        private bool _armed = true; // prevents double-counting one entry

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_armed) return;
            if (other.gameObject.layer != LayerMask.NameToLayer("Puck")) return;

            _armed = false;
            // Tell the world this goal (this side) was scored on.
            EventBus.RaiseGoalScored(_side);
        }

        // MatchManager re-arms the goal after each reset.
        public void Arm() => _armed = true;
    }
}