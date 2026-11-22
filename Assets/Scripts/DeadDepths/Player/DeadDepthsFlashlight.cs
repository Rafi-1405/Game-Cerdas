using System;
using UnityEngine;
using DeadDepths.Audio;
using DeadDepths.Core;
using UTS.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadDepths.Player
{
    /// <summary>
    /// Tactical dual-beam flashlight for Dead Depths bound to [T] (primary) and [L] (secondary)
    /// so it never conflicts with [F] Punch. Follows cursor aim smoothly during RMB Combat Stance.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeadDepthsFlashlight : MonoBehaviour
    {
        [Header("Flashlight State")]
        [SerializeField] private bool isOn = true;

        [Header("Core Spot Beam")]
        [SerializeField] private Color spotColor = new Color(1f, 0.96f, 0.88f, 1f);
        [SerializeField] private float spotIntensity = 14.0f;
        [SerializeField] private float spotRange = 36f;
        [SerializeField] private float spotAngle = 68f;
        [SerializeField] private float innerSpotAngle = 42f;

        [Header("Wide Flood Spill")]
        [SerializeField] private float floodIntensity = 4.2f;
        [SerializeField] private float floodRange = 18f;
        [SerializeField] private float floodAngle = 105f;
        [SerializeField] private float floodInnerAngle = 62f;

        [Header("Foot Ambient Fill")]
        [SerializeField] private float footLightIntensity = 1.4f;
        [SerializeField] private float footLightRange = 6.5f;

        [Header("Anchor & Aiming")]
        [SerializeField] private Vector3 handheldOffset = new Vector3(0.24f, 1.15f, 0.32f);
        [SerializeField] private float aimRotateSpeed = 18f;

        public bool IsOn => isOn;
        public event Action<bool> OnFlashlightToggled;

        private GameObject lightHolder;
        private Light spotLight;
        private Light floodLight;
        private Light ambientFootLight;
        private DeadDepthsPlayerController player;

        private void Awake()
        {
            player = GetComponent<DeadDepthsPlayerController>();
            SetupFlashlightComponents();
            ApplyLightSettings();
        }

        private void Update()
        {
            if (player == null) player = GetComponent<DeadDepthsPlayerController>();
            if (Time.timeScale <= 0f) return;

            bool inMainMenu = GameManager.Instance != null && GameManager.Instance.IsInMainMenu;
            if (!inMainMenu && (player == null || !player.IsDead))
            {
                if (WasTogglePressed())
                {
                    ToggleFlashlight();
                }
            }

            UpdateFlashlightTransform();
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.tKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame))
            {
                return true;
            }
#else
            if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.L))
            {
                return true;
            }
#endif
            return false;
        }

        public void ToggleFlashlight()
        {
            SetFlashlightState(!isOn);
        }

        public void SetFlashlightState(bool on)
        {
            isOn = on;
            ApplyLightSettings();
            if (Application.isPlaying)
            {
                AudioManager.Instance?.PlayCue(SfxCue.Flashlight, transform.position);
            }
            OnFlashlightToggled?.Invoke(isOn);
        }

        private void SetupFlashlightComponents()
        {
            Transform existingHolder = transform.Find("FlashlightHolder");
            if (existingHolder != null)
            {
                lightHolder = existingHolder.gameObject;
            }
            else
            {
                lightHolder = new GameObject("FlashlightHolder");
                lightHolder.transform.SetParent(transform, false);
            }
            lightHolder.transform.localPosition = handheldOffset;

            spotLight = lightHolder.GetComponent<Light>();
            if (spotLight == null) spotLight = lightHolder.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.shadows = LightShadows.Soft;

            Transform existingFlood = lightHolder.transform.Find("WideFloodLight");
            GameObject floodGo = existingFlood != null ? existingFlood.gameObject : new GameObject("WideFloodLight");
            if (existingFlood == null) floodGo.transform.SetParent(lightHolder.transform, false);
            floodLight = floodGo.GetComponent<Light>();
            if (floodLight == null) floodLight = floodGo.AddComponent<Light>();
            floodLight.type = LightType.Spot;
            floodLight.shadows = LightShadows.None;

            Transform existingFoot = lightHolder.transform.Find("AmbientFootLight");
            GameObject footGo = existingFoot != null ? existingFoot.gameObject : new GameObject("AmbientFootLight");
            if (existingFoot == null) footGo.transform.SetParent(lightHolder.transform, false);
            ambientFootLight = footGo.GetComponent<Light>();
            if (ambientFootLight == null) ambientFootLight = footGo.AddComponent<Light>();
            ambientFootLight.type = LightType.Point;
            ambientFootLight.shadows = LightShadows.None;
        }

        private void ApplyLightSettings()
        {
            bool effectiveOn = isOn && (player == null || !player.IsDead);

            if (spotLight != null)
            {
                spotLight.color = spotColor;
                spotLight.intensity = spotIntensity;
                spotLight.range = spotRange;
                spotLight.spotAngle = spotAngle;
                spotLight.innerSpotAngle = innerSpotAngle;
                spotLight.enabled = effectiveOn;
            }

            if (floodLight != null)
            {
                floodLight.color = new Color(0.90f, 0.94f, 1.0f);
                floodLight.intensity = floodIntensity;
                floodLight.range = floodRange;
                floodLight.spotAngle = floodAngle;
                floodLight.innerSpotAngle = floodInnerAngle;
                floodLight.enabled = effectiveOn;
            }

            if (ambientFootLight != null)
            {
                ambientFootLight.color = new Color(0.80f, 0.88f, 1.0f);
                ambientFootLight.intensity = footLightIntensity;
                ambientFootLight.range = footLightRange;
                ambientFootLight.enabled = effectiveOn;
            }
        }

        private void UpdateFlashlightTransform()
        {
            if (lightHolder == null) return;
            ApplyLightSettings();

            Vector3 targetPos = handheldOffset;
            if (player != null && player.IsCrouching) targetPos.y = 0.78f;
            lightHolder.transform.localPosition = targetPos;

            Quaternion targetRot;
            if (player != null && player.IsAiming)
            {
                Vector3 aimDir = player.LastValidAimPoint - lightHolder.transform.position;
                aimDir.y = Mathf.Clamp(aimDir.y, -0.35f, 0.15f);
                if (aimDir.sqrMagnitude > 0.01f)
                {
                    targetRot = Quaternion.LookRotation(aimDir.normalized, Vector3.up);
                }
                else
                {
                    targetRot = transform.rotation * Quaternion.Euler(8f, 0f, 0f);
                }
            }
            else
            {
                targetRot = transform.rotation * Quaternion.Euler(8f, 0f, 0f);
            }

            lightHolder.transform.rotation = Quaternion.Slerp(lightHolder.transform.rotation, targetRot, Time.deltaTime * aimRotateSpeed);
        }
    }
}
