// PowerUpScreenshotRunner.cs
using System.Collections;
using UnityEngine;

namespace AIAirHockey
{
    public class PowerUpScreenshotRunner : MonoBehaviour
    {
        public static void RunAndCapture()
        {
            GameObject runnerObj = new GameObject("_PowerUpScreenshotRunner");
            var runner = runnerObj.AddComponent<PowerUpScreenshotRunner>();
            runner.StartCoroutine(runner.Routine());
        }

        private IEnumerator Routine()
        {
            yield return null;
            yield return null;

            // Explicitly spawn power-up item at (0.9, 1.8)
            var mgr = PowerUpManager.Instance;
            if (mgr != null)
            {
                mgr.TrySpawnRandomPowerUp();
            }

            // Wait 5 frames for renderer sweep
            for (int i = 0; i < 10; i++) yield return null;

            ScreenCapture.CaptureScreenshot("shot3_power_spawn.png");
            Debug.Log("Screenshot 3 Captured via PowerUpScreenshotRunner!");
        }
    }
}
