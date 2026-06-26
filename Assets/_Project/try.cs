using UnityEngine;
using UnityEngine.SceneManagement; // Required for switching scenes

public class MainMenu : MonoBehaviour
{
    // Call this function to load the first level
    public void PlayGame()
    {
        // Option A: Load by the next index in Build Settings (Recommended)
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);

        // Option B: Load by exact scene name (Uncomment below if preferred)
        // SceneManager.LoadSceneAsync("GameScene");
    }

    // Call this function to close the game application
    public void QuitGame()
    {
        Debug.Log("Quit button pressed!"); // Confirms action in the Unity Editor
        Application.Quit(); // Shuts down the built application
    }
}