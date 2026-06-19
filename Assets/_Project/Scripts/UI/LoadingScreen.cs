// LoadingScreen.cs
using UnityEngine;
using UnityEngine.UI;

namespace AIAirHockey
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Slider _bar;

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void Start()
        {
            SceneLoader.Instance.OnLoadProgress += OnProgress;
            SceneLoader.Instance.OnLoadComplete += OnComplete;
        }


        private void OnEnable()
        {
            // Show whenever a load starts. We detect start via progress 0.
        }

        private void OnProgress(float t)
        {
            if (!_root.activeSelf) _root.SetActive(true);
            _bar.value = t;
        }

        private void OnComplete()
        {
            _root.SetActive(false);
            _bar.value = 0f;
        }

        private void OnDestroy()
        {
            if (SceneLoader.Exists)
            {
                SceneLoader.Instance.OnLoadProgress -= OnProgress;
                SceneLoader.Instance.OnLoadComplete -= OnComplete;
            }
        }
    }
}