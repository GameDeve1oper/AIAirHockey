// Singleton.cs
using UnityEngine;

namespace AIAirHockey
{
    // Generic singleton base for MonoBehaviours.
    // Any class T that inherits Singleton<T> gets a global Instance
    // and survives scene loads.
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        // True once the instance exists, so callers can null-check safely.
        public static bool Exists => Instance != null;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // A duplicate exists (e.g. Bootstrap loaded twice). Destroy this one.
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
            // Keep this object alive across scene changes.
            DontDestroyOnLoad(gameObject);
        }
    }
}