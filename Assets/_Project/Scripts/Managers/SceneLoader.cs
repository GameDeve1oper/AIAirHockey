// SceneLoader.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIAirHockey
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        // Scene names. Must match the actual scene file names exactly.
        public const string MainMenu = "MainMenu";
        public const string Gameplay = "Gameplay";

        // Event other systems (loading screen) can hook into for progress 0..1.
        public System.Action<float> OnLoadProgress;
        public System.Action OnLoadComplete;

        // Loads a scene single-mode (replaces current non-persistent scene).
        public void Load(string sceneName)
        {
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            // Unity reports 0..0.9 during load, then waits for activation.
            while (op.progress < 0.9f)
            {
                OnLoadProgress?.Invoke(op.progress / 0.9f);
                yield return null;
            }
            OnLoadProgress?.Invoke(1f);
            // Tiny pause so the loading bar is visible even on fast loads.
            yield return new WaitForSeconds(0.3f);
            op.allowSceneActivation = true;
            yield return op;
            OnLoadComplete?.Invoke();
        }
    }
}