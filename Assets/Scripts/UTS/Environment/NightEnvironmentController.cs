using UnityEngine;
using UnityEngine.Rendering;

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
        [SerializeField] private Color editorAmbientSkyColor = new Color(0.45f, 0.45f, 0.45f);
        [SerializeField] private Color editorAmbientEquatorColor = new Color(0.35f, 0.35f, 0.35f);
        [SerializeField] private Color editorAmbientGroundColor = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] private Color editorFogColor = new Color(0.5f, 0.5f, 0.5f);

        [Header("Runtime Game (Cinematic Moonlight Night)")]
        [SerializeField] private Color nightLightColor = new Color(0.65f, 0.78f, 1.0f); // Cool silver moonlight
        [SerializeField, Range(0.01f, 1.0f)] private float nightIntensity = 0.38f;
        [SerializeField] private Color nightAmbientSkyColor = new Color(0.22f, 0.26f, 0.35f); // Soft cool ambient
        [SerializeField] private Color nightAmbientEquatorColor = new Color(0.14f, 0.17f, 0.22f);
        [SerializeField] private Color nightAmbientGroundColor = new Color(0.08f, 0.09f, 0.12f);
        [SerializeField] private Color nightFogColor = new Color(0.04f, 0.06f, 0.09f);

        [SerializeField] private bool previewNightInEditor = true;

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
            // Update lighting state if mode changes in editor
            if (!Application.isPlaying)
            {
                ApplyLighting();
            }
        }

        public void ApplyLighting()
        {
            if (directionalLight == null) directionalLight = GetComponent<Light>();
            if (directionalLight == null) return;

            bool isNight = Application.isPlaying || previewNightInEditor;

            if (isNight)
            {
                directionalLight.color = nightLightColor;
                directionalLight.intensity = nightIntensity;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = nightAmbientSkyColor;
                RenderSettings.ambientEquatorColor = nightAmbientEquatorColor;
                RenderSettings.ambientGroundColor = nightAmbientGroundColor;
                RenderSettings.fogColor = nightFogColor;
            }
            else
            {
                directionalLight.color = editorLightColor;
                directionalLight.intensity = editorIntensity;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = editorAmbientSkyColor;
                RenderSettings.ambientEquatorColor = editorAmbientEquatorColor;
                RenderSettings.ambientGroundColor = editorAmbientGroundColor;
                RenderSettings.fogColor = editorFogColor;
            }
        }
    }
}
