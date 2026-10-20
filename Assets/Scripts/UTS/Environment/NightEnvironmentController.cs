using UnityEngine;

namespace UTS.Environment
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class NightEnvironmentController : MonoBehaviour
    {
        [Header("Editor Preview (Daylight)")]
        [SerializeField] private Color editorLightColor = new Color(1.0f, 0.96f, 0.88f); // Warm daylight
        [SerializeField, Range(0.5f, 2.0f)] private float editorIntensity = 1.0f;

        [Header("Runtime Game (Project Zomboid Night)")]
        [SerializeField] private Color nightLightColor = new Color(0.10f, 0.14f, 0.22f); // Midnight blue
        [SerializeField, Range(0.01f, 0.5f)] private float nightIntensity = 0.12f;

        [SerializeField] private bool previewNightInEditor;

        private Light directionalLight;

        private void Awake()
        {
            directionalLight = GetComponent<Light>();
            ApplyLighting();
        }

        private void OnEnable()
        {
            directionalLight = GetComponent<Light>();
            ApplyLighting();
        }

        private void OnValidate()
        {
            if (directionalLight == null) directionalLight = GetComponent<Light>();
            ApplyLighting();
        }

        private void Update()
        {
            // Update lighting state if mode changes
            if (!Application.isPlaying)
            {
                ApplyLighting();
            }
        }

        public void ApplyLighting()
        {
            if (directionalLight == null) return;

            if (Application.isPlaying || previewNightInEditor)
            {
                directionalLight.color = nightLightColor;
                directionalLight.intensity = nightIntensity;
            }
            else
            {
                directionalLight.color = editorLightColor;
                directionalLight.intensity = editorIntensity;
            }
        }
    }
}
