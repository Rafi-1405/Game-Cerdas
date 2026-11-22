using UnityEngine;

namespace DeadDepths.Core
{
    /// <summary>
    /// Lightweight pooled URP particle burst manager for melee impacts, wood splinters,
    /// dodge/charger dust, wall collision shockwaves, acid splashes, and healing pulses.
    /// </summary>
    public class CombatVfxManager : MonoBehaviour
    {
        public static CombatVfxManager Instance { get; private set; }

        [SerializeField] private Shader bundledParticleShader;
        [SerializeField] private Shader bundledFallbackShader;

        private const int PoolSize = 16;
        private ParticleSystem[] burstPool;
        private Material particleMaterial;
        private Texture2D softCircleTexture;
        private int poolCursor = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (particleMaterial != null)
            {
                if (Application.isPlaying) Destroy(particleMaterial);
                else DestroyImmediate(particleMaterial);
                particleMaterial = null;
            }
            if (softCircleTexture != null)
            {
                if (Application.isPlaying) Destroy(softCircleTexture);
                else DestroyImmediate(softCircleTexture);
                softCircleTexture = null;
            }
        }

        private void InitializePool()
        {
            if (burstPool != null) return;

            softCircleTexture = CreateSoftDiscTexture(32);
            Shader shader = bundledParticleShader != null
                ? bundledParticleShader
                : (Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                   Shader.Find("Particles/Standard Unlit") ??
                   bundledFallbackShader ??
                   Shader.Find("Universal Render Pipeline/Unlit") ??
                   Shader.Find("Sprites/Default") ??
                   Shader.Find("Universal Render Pipeline/Lit"));
            if (shader != null)
            {
                particleMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (particleMaterial.HasProperty("_Surface")) particleMaterial.SetFloat("_Surface", 1f);
                if (particleMaterial.HasProperty("_Blend")) particleMaterial.SetFloat("_Blend", 0f);
                if (particleMaterial.HasProperty("_SrcBlend")) particleMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (particleMaterial.HasProperty("_DstBlend")) particleMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (particleMaterial.HasProperty("_ZWrite")) particleMaterial.SetFloat("_ZWrite", 0f);
                particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                particleMaterial.SetOverrideTag("RenderType", "Transparent");
                particleMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                particleMaterial.mainTexture = softCircleTexture;
            }

            burstPool = new ParticleSystem[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject go = new GameObject($"VfxBurst_{i}");
                go.transform.SetParent(transform, false);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 32;
                main.startLifetime = 0.35f;
                main.startSpeed = 4f;
                main.startSize = 0.2f;
                main.gravityModifier = 0.8f;

                var emission = ps.emission;
                emission.enabled = false;

                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                AnimationCurve curve = new AnimationCurve();
                curve.AddKey(0f, 1f);
                curve.AddKey(1f, 0.1f);
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

                var rend = go.GetComponent<ParticleSystemRenderer>();
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                rend.sharedMaterial = particleMaterial;

                burstPool[i] = ps;
            }
        }

        private static Texture2D CreateSoftDiscTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(0.5f, 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2((x + 0.5f) / size, (y + 0.5f) / size), c) * 2f;
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private ParticleSystem NextSystem()
        {
            if (burstPool == null) InitializePool();
            var ps = burstPool[poolCursor];
            poolCursor = (poolCursor + 1) % PoolSize;
            return ps;
        }

        private void EmitBurst(Vector3 pos, Color color, int count, float speedMin, float speedMax, float sizeMin, float sizeMax, float lifetime, float gravity, float radius)
        {
            var ps = NextSystem();
            if (ps == null) return;

            ps.transform.position = pos;
            var main = ps.main;
            main.startColor = color;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startLifetime = lifetime;
            main.gravityModifier = gravity;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            ps.Emit(count);
        }

        public void PlayPunchSwing(Vector3 origin, Vector3 forward, bool isFinisher)
        {
            Color col = isFinisher ? new Color(1f, 0.85f, 0.45f, 0.65f) : new Color(0.85f, 0.90f, 1f, 0.45f);
            EmitBurst(origin + forward * 0.9f + Vector3.up * 1.1f, col, isFinisher ? 8 : 5, 2.2f, 4.5f, 0.14f, 0.28f, 0.18f, 0f, 0.25f);
        }

        public void PlayHitBlood(Vector3 pos, bool isCrit)
        {
            Color blood = isCrit ? new Color(1f, 0.55f, 0.12f, 0.95f) : new Color(0.75f, 0.08f, 0.10f, 0.92f);
            EmitBurst(pos + Vector3.up * 1.0f, blood, isCrit ? 16 : 10, 2.5f, 6.0f, 0.12f, isCrit ? 0.30f : 0.22f, 0.35f, 1.1f, 0.18f);
        }

        public void PlayWoodSplinter(Vector3 pos, bool destroyed)
        {
            Color wood = new Color(0.72f, 0.52f, 0.28f, 0.95f);
            EmitBurst(pos + Vector3.up * 0.8f, wood, destroyed ? 18 : 8, 2.0f, 5.5f, 0.12f, 0.28f, 0.42f, 1.4f, 0.35f);
        }

        public void PlayDodgeDust(Vector3 pos)
        {
            Color dust = new Color(0.65f, 0.62f, 0.56f, 0.55f);
            EmitBurst(pos + Vector3.up * 0.18f, dust, 10, 1.4f, 3.4f, 0.24f, 0.45f, 0.36f, -0.15f, 0.30f);
        }

        public void PlayWallShockwave(Vector3 pos)
        {
            Color dust = new Color(0.92f, 0.72f, 0.48f, 0.80f);
            EmitBurst(pos + Vector3.up * 0.4f, dust, 20, 3.5f, 8.0f, 0.28f, 0.55f, 0.48f, 0.4f, 0.45f);
        }

        public void PlayAcidSplash(Vector3 pos)
        {
            Color acid = new Color(0.25f, 1.0f, 0.38f, 0.92f);
            EmitBurst(pos, acid, 14, 2.0f, 5.2f, 0.15f, 0.34f, 0.38f, 0.9f, 0.22f);
        }

        public void PlayHealBurst(Vector3 pos)
        {
            Color heal = new Color(0.32f, 0.96f, 0.55f, 0.85f);
            EmitBurst(pos + Vector3.up * 0.9f, heal, 12, 1.2f, 2.8f, 0.16f, 0.32f, 0.48f, -0.35f, 0.35f);
        }
    }
}
