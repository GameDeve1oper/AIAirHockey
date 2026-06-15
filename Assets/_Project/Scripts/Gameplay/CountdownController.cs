// CountdownController.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    // Runs a 3-2-1-GO countdown, raising EventBus ticks the UI listens to.
    public class CountdownController : MonoBehaviour
    {
        [SerializeField] private float _stepDelay = 1f;

        // Runs the countdown then calls onComplete.
        public IEnumerator Run(System.Action onComplete)
        {
            for (int n = 3; n >= 1; n--)
            {
                EventBus.RaiseCountdownTick(n);
                AudioManager.Instance.Play(SoundId.Countdown);
                yield return new WaitForSeconds(_stepDelay);
            }
            EventBus.RaiseCountdownTick(0); // 0 == GO
            AudioManager.Instance.Play(SoundId.CountdownGo);
            yield return new WaitForSeconds(0.5f);
            onComplete?.Invoke();
        }
    }
}