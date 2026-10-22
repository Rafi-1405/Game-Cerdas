using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UTS.Player
{
    [DisallowMultipleComponent]
    public class PlayerFlashlight : MonoBehaviour
    {
        [Header("Flashlight State")]
        [SerializeField] private bool isOn = true;
        [SerializeField] private bool isAttachedToBelt = false;

        [Header("Light Parameters (Resident Evil Tactical Dual-Beam)")]
        [SerializeField] private Color lightColor = new Color(1f, 0.97f, 0.90f, 1f);
        [SerializeField] private float spotIntensity = 12.0f;
        [SerializeField] private float spotRange = 52f;
        [SerializeField] private float spotAngle = 80f;
        [SerializeField] private float innerSpotAngle = 56f;

        [Header("Wide Flood Spill (Peripheral Lighting)")]
        [SerializeField] private float floodIntensity = 4.8f;
        [SerializeField] private float floodRange = 30f;
        [SerializeField] private float floodAngle = 102f;
        [SerializeField] private float floodInnerAngle = 65f;

        [Header("Foot Ambient Light")]
        [SerializeField] private float footLightIntensity = 1.2f;
        [SerializeField] private float footLightRange = 7.0f;

        [Header("Anchors & Offsets")]
        [SerializeField] private Vector3 handheldOffset = new Vector3(0.28f, 1.05f, 0.32f);
        [SerializeField] private Vector3 beltOffset = new Vector3(0.18f, 0.72f, 0.12f);
        [SerializeField] private float aimRotateSpeed = 15f;

        private GameObject lightHolder;
        private Light spotLight;
        private Light floodLight;
        private Light ambientFootLight;
        private bool isAiming = false;
        private readonly RaycastHit[] aimHitBuffer = new RaycastHit[16];

        public bool IsOn => isOn;
        public bool IsAttachedToBelt => isAttachedToBelt;

        private void Awake()
        {
            SetupFlashlightComponents();
        }

        private void Start()
        {
            ApplyLightSettings();
        }

        private void Update()
        {
            HandleToggleInput();
            UpdateFlashlightTransform();
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

            // 1. Spotlight utama (Core Beam - RE Style)
            spotLight = lightHolder.GetComponent<Light>();
            if (spotLight == null)
            {
                spotLight = lightHolder.AddComponent<Light>();
            }
            spotLight.type = LightType.Spot;
            spotLight.shadows = LightShadows.Soft;

            // 2. Wide Flood Light (Cahaya tumpah/spill melebar di sekitar)
            Transform existingFlood = lightHolder.transform.Find("WideFloodLight");
            GameObject floodObj;
            if (existingFlood != null)
            {
                floodObj = existingFlood.gameObject;
            }
            else
            {
                floodObj = new GameObject("WideFloodLight");
                floodObj.transform.SetParent(lightHolder.transform, false);
            }

            floodLight = floodObj.GetComponent<Light>();
            if (floodLight == null)
            {
                floodLight = floodObj.AddComponent<Light>();
            }
            floodLight.type = LightType.Spot;
            floodLight.shadows = LightShadows.None;

            // 3. Ambient fill light di sekitar kaki & tubuh pemain
            Transform existingFill = lightHolder.transform.Find("AmbientFootLight");
            GameObject fillObj;
            if (existingFill != null)
            {
                fillObj = existingFill.gameObject;
            }
            else
            {
                fillObj = new GameObject("AmbientFootLight");
                fillObj.transform.SetParent(lightHolder.transform, false);
            }

            ambientFootLight = fillObj.GetComponent<Light>();
            if (ambientFootLight == null)
            {
                ambientFootLight = fillObj.AddComponent<Light>();
            }
            ambientFootLight.type = LightType.Point;
            ambientFootLight.shadows = LightShadows.None;
        }

        public void ApplyLightSettings()
        {
            if (spotLight != null)
            {
                spotLight.color = lightColor;
                spotLight.intensity = spotIntensity;
                spotLight.range = spotRange;
                spotLight.spotAngle = spotAngle;
                spotLight.innerSpotAngle = innerSpotAngle;
                spotLight.enabled = isOn;
            }

            if (floodLight != null)
            {
                floodLight.color = new Color(0.92f, 0.94f, 1f);
                floodLight.intensity = floodIntensity;
                floodLight.range = floodRange;
                floodLight.spotAngle = floodAngle;
                floodLight.innerSpotAngle = floodInnerAngle;
                floodLight.enabled = isOn;
            }

            if (ambientFootLight != null)
            {
                ambientFootLight.color = new Color(0.85f, 0.9f, 1f);
                ambientFootLight.intensity = footLightIntensity;
                ambientFootLight.range = footLightRange;
                ambientFootLight.enabled = isOn;
            }
        }

        public void ToggleFlashlight()
        {
            SetFlashlightState(!isOn);
        }

        public void SetFlashlightState(bool on)
        {
            isOn = on;
            if (spotLight != null) spotLight.enabled = isOn;
            if (floodLight != null) floodLight.enabled = isOn;
            if (ambientFootLight != null) ambientFootLight.enabled = isOn;
            Debug.Log($"[PlayerFlashlight] Senter {(isOn ? "MENYALA" : "PADAM")}", this);
        }

        public void SetBeltAttachment(bool attached)
        {
            isAttachedToBelt = attached;
            Debug.Log($"[PlayerFlashlight] Senter dipindahkan ke: {(isAttachedToBelt ? "SABUK (Belt)" : "TANGAN (Handheld)")}", this);
        }

        public void SetAiming(bool aiming)
        {
            isAiming = aiming;
        }

        private void HandleToggleInput()
        {
            bool togglePressed = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame)
            {
                togglePressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.F))
            {
                togglePressed = true;
            }
#endif
            if (togglePressed)
            {
                ToggleFlashlight();
            }
        }

        private void UpdateFlashlightTransform()
        {
            if (lightHolder == null) return;

            Vector3 targetLocalPos = isAttachedToBelt ? beltOffset : handheldOffset;
            lightHolder.transform.localPosition = targetLocalPos;

            if (isAiming)
            {
                // Arahkan senter ke titik bidik kursor/tengah layar
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                    Vector3 targetAimPoint = ray.origin + ray.direction * spotRange;

                    int hitCount = Physics.RaycastNonAlloc(ray, aimHitBuffer, spotRange, ~0, QueryTriggerInteraction.Ignore);
                    float closestValidDist = float.MaxValue;

                    for (int i = 0; i < hitCount; i++)
                    {
                        RaycastHit hit = aimHitBuffer[i];
                        if (hit.collider == null) continue;

                        // Abaikan collider milik Player itu sendiri atau child-nya
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            continue;
                        }

                        if (hit.distance < closestValidDist)
                        {
                            closestValidDist = hit.distance;
                            targetAimPoint = hit.point;
                        }
                    }

                    Vector3 aimDirection = (targetAimPoint - lightHolder.transform.position).normalized;

                    // Proteksi keamanan: Senter HARUS mengarah ke depan (searah pandangan kamera).
                    // Cegah senter berbalik ke belakang menyinari badan/muka pemain sendiri.
                    if (Vector3.Dot(aimDirection, cam.transform.forward) < 0.2f)
                    {
                        aimDirection = cam.transform.forward;
                    }

                    if (aimDirection.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(aimDirection, Vector3.up);
                        lightHolder.transform.rotation = Quaternion.Slerp(lightHolder.transform.rotation, targetRot, aimRotateSpeed * Time.deltaTime);
                        return;
                    }
                }
            }

            // Normal mode: senter menghadap sedikit ke bawah depan
            Quaternion normalRot = transform.rotation * Quaternion.Euler(6f, 0f, 0f);
            lightHolder.transform.rotation = Quaternion.Slerp(lightHolder.transform.rotation, normalRot, 10f * Time.deltaTime);
        }
    }
}
