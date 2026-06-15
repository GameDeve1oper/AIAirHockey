// Bootstrap.cs
using UnityEngine;

namespace AIAirHockey
{
    // Lives only in the Bootstrap scene. Runs once at app start,
    // then loads the Main Menu. Managers persist via DontDestroyOnLoad.
    public class Bootstrap : MonoBehaviour
    {
        private void Start()
        {
            // Lock to 60 FPS for consistent physics/feel on mobile.
            Application.targetFrameRate = 60;
            // Prevent the screen from sleeping during play.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Start menu music and go to the menu.
            AudioManager.Instance.PlayMenuMusic();
            SceneLoader.Instance.Load(SceneLoader.MainMenu);
        }
    }
}