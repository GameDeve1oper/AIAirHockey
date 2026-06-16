// GoalNotification.cs
using System.Collections;
using TMPro;
using UnityEngine;

namespace AIAirHockey
{
    // Shows 'GOAL!' on a goal, and the 3-2-1-GO countdown.
    public class GoalNotification : MonoBehaviour
    {
        [SerializeField] private TMP_Text _goalText;
        [SerializeField] private TMP_Text _countdownText;

        private void OnEnable()
        {
            EventBus.OnGoalScored += ShowGoal;
            EventBus.OnCountdownTick += ShowCountdown;
        }
        private void OnDisable()
        {
            EventBus.OnGoalScored -= ShowGoal;
            EventBus.OnCountdownTick -= ShowCountdown;
        }

        private void Awake()
        {
            _goalText.gameObject.SetActive(false);
            _countdownText.gameObject.SetActive(false);
        }

        private void ShowGoal(PlayerSide conceded)
        {
            StopAllCoroutines();
            StartCoroutine(PunchText(_goalText, "GOAL!", 1.0f));
        }

        private void ShowCountdown(int n)
        {
            string s = n == 0 ? "GO!" : n.ToString();
            StartCoroutine(PunchText(_countdownText, s, 0.9f));
        }

        // Pop the text in with a scale punch, then fade out.
        private IEnumerator PunchText(TMP_Text label, string text, float life)
        {
            label.text = text;
            label.gameObject.SetActive(true);
            float t = 0f;
            Vector3 baseScale = Vector3.one;
            while (t < life)
            {
                t += Time.unscaledDeltaTime;
                float k = t / life;
                // Quick pop up then settle.
                float scale = Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(k * 3f));
                label.transform.localScale = baseScale * scale;
                // Fade out in the last 30%.
                float alpha = k < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.7f) / 0.3f);
                var c = label.color; c.a = alpha; label.color = c;
                yield return null;
            }
            label.gameObject.SetActive(false);
            var cc = label.color; cc.a = 1f; label.color = cc;
            label.transform.localScale = baseScale;
        }
    }
}