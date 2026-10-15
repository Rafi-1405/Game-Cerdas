using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UTS.Environment
{
    [DisallowMultipleComponent]
    public class GreenSmokeExtractionZone : MonoBehaviour
    {
        public static GreenSmokeExtractionZone Instance { get; private set; }

        [Header("Zone Configuration")]
        [SerializeField] private float radius = 5.0f;
        [SerializeField] private float requiredHoldTime = 10.0f;
        [SerializeField] private bool restartOnR = true;

        [Header("Visual Colors")]
        [SerializeField] private Color smokeColor = new Color(0.12f, 1.0f, 0.40f, 0.85f); // PUBG Rescue Green
        [SerializeField] private Color ringColor = new Color(0.15f, 1.0f, 0.45f, 0.95f);

        private Transform player;
        private float currentHoldTime = 0f;
        private bool isPlayerInside = false;
        private bool isExtracted = false;

        private LineRenderer ringRenderer;
        private ParticleSystem smokeParticleSystem;
        private Light baseGlowLight;

        public bool IsPlayerInside => isPlayerInside;
        public float CurrentHoldTime => currentHoldTime;
        public float RequiredHoldTime => requiredHoldTime;
        public float RemainingHoldTime => Mathf.Max(0f, requiredHoldTime - currentHoldTime);
        public float ProgressRatio => requiredHoldTime > 0f ? Mathf.Clamp01(currentHoldTime / requiredHoldTime) : 0f;
        public bool IsExtracted => isExtracted;

        private void Awake()
        {
            Instance = this;
            SetupCircularRing();
            SetupCenterFlareCanister();
            SetupGreenSmokeParticle();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (player == null)
            {
                FindPlayer();
                if (player == null) return;
            }

            if (isExtracted)
            {
                if (restartOnR && IsRestartPressed())
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
                return;
            }

            // Hitung jarak horizontal (planar) ke player
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            float planarDistance = offset.magnitude;

            if (planarDistance <= radius)
            {
                isPlayerInside = true;
                currentHoldTime += Time.deltaTime;

                if (currentHoldTime >= requiredHoldTime)
                {
                    currentHoldTime = requiredHoldTime;
                    isExtracted = true;
                    OnExtractionSuccess();
                }
            }
            else
            {
                isPlayerInside = false;
                if (currentHoldTime > 0f)
                {
                    // Timer berkurang perlahan jika pemain keluar dari zona
                    currentHoldTime = Mathf.Max(0f, currentHoldTime - Time.deltaTime * 1.5f);
                }
            }
        }

        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        private void OnExtractionSuccess()
        {
            Debug.Log("[GreenSmokeExtractionZone] EXTRACTION SUCCESSFUL! Pemain berhasil bertahan 10 detik dan dievakuasi.", this);
            
            // Beri tahu ZombieHordeManager jika ada di scene
            UTS.Zombies.ZombieHordeManager hordeManager = FindAnyObjectByType<UTS.Zombies.ZombieHordeManager>();
            if (hordeManager != null)
            {
                hordeManager.TriggerExtractionSuccess();
            }
        }

        private void SetupCircularRing()
        {
            Transform existingRing = transform.Find("ExtractionRing");
            GameObject ringObj;
            if (existingRing != null)
            {
                ringObj = existingRing.gameObject;
            }
            else
            {
                ringObj = new GameObject("ExtractionRing");
                ringObj.transform.SetParent(transform, false);
            }

            ringRenderer = ringObj.GetComponent<LineRenderer>();
            if (ringRenderer == null)
            {
                ringRenderer = ringObj.AddComponent<LineRenderer>();
            }

            int segments = 64;
            ringRenderer.useWorldSpace = false;
            ringRenderer.positionCount = segments + 1;
            ringRenderer.startWidth = 0.18f;
            ringRenderer.endWidth = 0.18f;
            ringRenderer.loop = true;

            // Material warna neon hijau unlit
            Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            lineMat.color = ringColor;
            ringRenderer.sharedMaterial = lineMat;
            ringRenderer.startColor = ringColor;
            ringRenderer.endColor = ringColor;

            float deltaAngle = (2f * Mathf.PI) / segments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * deltaAngle;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                ringRenderer.SetPosition(i, new Vector3(x, 0.06f, z));
            }
        }

        private void SetupCenterFlareCanister()
        {
            Transform existingCanister = transform.Find("FlareCanister");
            GameObject canisterObj;
            if (existingCanister != null)
            {
                canisterObj = existingCanister.gameObject;
            }
            else
            {
                canisterObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                canisterObj.name = "FlareCanister";
                canisterObj.transform.SetParent(transform, false);
                Collider col = canisterObj.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
            }

            canisterObj.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            canisterObj.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);

            Renderer rend = canisterObj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material canMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                canMat.color = new Color(0.12f, 0.55f, 0.28f);
                rend.sharedMaterial = canMat;
            }
        }

        private static Texture2D cachedSmokeTexture;

        private Texture2D GetOrCreateSmokeTexture()
        {
            if (cachedSmokeTexture != null) return cachedSmokeTexture;

            int res = 64;
            cachedSmokeTexture = new Texture2D(res, res, TextureFormat.RGBA32, false);
            cachedSmokeTexture.wrapMode = TextureWrapMode.Clamp;
            cachedSmokeTexture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(0.5f, 0.5f);
            Color[] pixels = new Color[res * res];

            for (int y = 0; y < res; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res;
                    float d = Vector2.Distance(new Vector2(u, v), center) * 2f; // 0 di tengah, 1 di pinggir
                    float a = Mathf.Clamp01(1f - d);
                    // Kurva halus gaussian / smoothstep untuk kabut lembut
                    a = a * a * (3f - 2f * a);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, a);
                }
            }

            cachedSmokeTexture.SetPixels(pixels);
            cachedSmokeTexture.Apply();
            return cachedSmokeTexture;
        }

        private void SetupGreenSmokeParticle()
        {
            Transform existingSmoke = transform.Find("GreenSmokePillar");
            GameObject smokeObj;
            if (existingSmoke != null)
            {
                smokeObj = existingSmoke.gameObject;
            }
            else
            {
                smokeObj = new GameObject("GreenSmokePillar");
                smokeObj.transform.SetParent(transform, false);
            }
            smokeObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            // Rotasi -90 derajat pada sumbu X mengarahkan forward (+Z) partikel lurus tegak ke langit (+Y)
            smokeObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            smokeParticleSystem = smokeObj.GetComponent<ParticleSystem>();
            if (smokeParticleSystem == null)
            {
                smokeParticleSystem = smokeObj.AddComponent<ParticleSystem>();
            }

            // Konfigurasi Particle System Pilar Asap Realistis (Kecil di pangkal, melebar bertahap ke atas)
            var main = smokeParticleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 4.0f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(7.5f, 10.5f); // Meluncur cepat ke atas
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = smokeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.14f; // Efek apung gas panas naik ke atas

            var emission = smokeParticleSystem.emission;
            emission.rateOverTime = 48f; // Densitas optimal membentuk aliran asap kontinu

            // Emitter kerucut sempit dengan bukaan gradual ke atas (realistic upward plume)
            var shape = smokeParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2.8f;   // Sudut kerucut lembut, melebar secara gradual semakin tinggi
            shape.radius = 0.25f; // Pangkal cerobong sempit di permukaan flare tanah
            shape.rotation = Vector3.zero;

            // Dorongan vertikal ke atas di world space
            var forceOverLifetime = smokeParticleSystem.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.space = ParticleSystemSimulationSpace.World;
            forceOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
            forceOverLifetime.y = new ParticleSystem.MinMaxCurve(1.6f); // Akselerasi konstan ke atas
            forceOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

            // Kurva ukuran bertumbuh realistis: kecil di pangkal flare -> melebar megah di langit tinggi
            var sizeOverLifetime = smokeParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.45f);   // 0% (tanah): jet semprotan kecil & rapat (~0.35m)
            sizeCurve.AddKey(0.25f, 0.95f); // 25% (ketinggian 6m): mulai membentuk pilar solid (~0.85m)
            sizeCurve.AddKey(0.6f, 1.85f);  // 60% (ketinggian 16m): mengembang alami (~1.6m)
            sizeCurve.AddKey(1f, 3.2f);    // 100% (ketinggian 28-30m): mengepul lebar di langit (~3m)
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Rotasi lembut kepulan asap saat membubung ke atas (volumetric swirling)
            var rotationOverLifetime = smokeParticleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);

            // Gradien warna & transparansi: pekat di badan pilar, memudar lembut di ujung atas
            var colorOverLifetime = smokeParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(smokeColor, 0f), 
                    new GradientColorKey(new Color(0.2f, 1f, 0.5f), 0.4f), 
                    new GradientColorKey(new Color(0.25f, 0.85f, 0.45f), 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f), 
                    new GradientAlphaKey(0.95f, 0.08f), // Cepat pekat setelah keluar dari cerobong
                    new GradientAlphaKey(0.85f, 0.65f), // Tetap pekat di udara
                    new GradientAlphaKey(0f, 1f)        // Dissolve lembut di puncak langit
                }
            );
            colorOverLifetime.color = grad;

            // Renderer partikel dengan tekstur kabut lembut prosedural
            ParticleSystemRenderer rend = smokeObj.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                Material smokeMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
                smokeMat.color = smokeColor;
                smokeMat.mainTexture = GetOrCreateSmokeTexture();
                rend.sharedMaterial = smokeMat;
            }

            // Lampu pendar hijau di pangkal asap
            baseGlowLight = smokeObj.GetComponent<Light>();
            if (baseGlowLight == null)
            {
                baseGlowLight = smokeObj.AddComponent<Light>();
            }
            baseGlowLight.type = LightType.Point;
            baseGlowLight.color = new Color(0.2f, 1.0f, 0.4f, 1f);
            baseGlowLight.intensity = 2.5f;
            baseGlowLight.range = 8f;
        }

        private bool IsRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ringColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = smokeColor;
            Gizmos.DrawRay(transform.position, Vector3.up * 25f);
        }
    }
}
