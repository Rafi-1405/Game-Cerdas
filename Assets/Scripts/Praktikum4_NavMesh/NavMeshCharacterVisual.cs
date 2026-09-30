using UnityEngine;
using UnityEngine.AI;

namespace Praktikum4.NavMeshNavigation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class NavMeshCharacterVisual : MonoBehaviour
    {
        private const string VisualChildName = "NavMesh_YBot_Visual";

        [Header("Model & Animation Resource Paths")]
        [SerializeField] private string modelResourcePath = "Walking";
        [SerializeField] private string animationResourcePath = "Walking";

        [Header("Transform Offsets")]
        [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.92f, 0f);
        [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 localScale = Vector3.one;

        [Header("Animation Speed Settings")]
        [SerializeField] private float sneakAnimationSpeed = 0.75f;
        [SerializeField] private float walkAnimationSpeed = 1.25f;
        [SerializeField] private float runAnimationSpeed = 1.8f;

        [Header("Color Tint")]
        [SerializeField] private bool syncSourceColor = false;
        [SerializeField] private Color defaultModelColor = Color.white;

        private Renderer sourceRenderer;
        private PlayerTargetMovement playerMovement;
        private NavMeshAgent navMeshAgent;
        private Renderer[] modelRenderers;
        private AnimationState walkState;

        private void Awake()
        {
            sourceRenderer = GetComponent<Renderer>();
            playerMovement = GetComponent<PlayerTargetMovement>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            // Auto-assign warna kontras jika masih putih/default
            if (defaultModelColor == Color.white)
            {
                if (playerMovement != null) defaultModelColor = new Color(0.15f, 0.65f, 1.0f, 1f); // Cyan Player
                else if (navMeshAgent != null) defaultModelColor = new Color(1.0f, 0.2f, 0.2f, 1f); // Red NPC
            }

            if (Application.isPlaying)
            {
                DestroyVisual();
                InitVisual();
            }
        }

        private void Start()
        {
            if (Application.isPlaying && transform.Find(VisualChildName) == null)
            {
                InitVisual();
            }
        }

        private void Update()
        {
            if (walkState != null)
            {
                walkState.speed = CalculateAnimationSpeed();
            }

            if (syncSourceColor)
            {
                ApplyModelColor();
            }
        }

        private void InitVisual()
        {
            Transform visual = CreateVisualInstance();
            if (visual == null) return;

            modelRenderers = visual.GetComponentsInChildren<Renderer>();
            SetupAnimation(visual);
            ApplyModelColor();
        }

        private Transform CreateVisualInstance()
        {
            // Sembunyikan kapsul bawaan Unity agar hanya model 3D yang terlihat
            if (sourceRenderer != null)
            {
                sourceRenderer.enabled = false;
            }

            Transform existingVisual = transform.Find(VisualChildName);
            if (existingVisual != null)
            {
                return existingVisual;
            }

            GameObject modelPrefab = Resources.Load<GameObject>(modelResourcePath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[NavMeshCharacterVisual] Model '{modelResourcePath}' tidak ditemukan di Assets/Resources. Menggunakan visual dasar.", this);
                if (sourceRenderer != null) sourceRenderer.enabled = true;
                return null;
            }

            GameObject visualObj = Instantiate(modelPrefab, transform);
            visualObj.name = VisualChildName;
            visualObj.transform.localPosition = localPosition;
            visualObj.transform.localRotation = Quaternion.Euler(localEulerAngles);
            visualObj.transform.localScale = localScale;

            return visualObj.transform;
        }

        private void SetupAnimation(Transform visual)
        {
            Animation anim = visual.GetComponent<Animation>();
            if (anim == null)
            {
                anim = visual.gameObject.AddComponent<Animation>();
            }

            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(animationResourcePath);
            foreach (AnimationClip clip in clips)
            {
                if (clip == null || clip.length <= 0f) continue;

                clip.wrapMode = WrapMode.Loop;
                anim.AddClip(clip, clip.name);
                anim.Play(clip.name);
                walkState = anim[clip.name];
                break;
            }
        }

        private float CalculateAnimationSpeed()
        {
            // Kasus 1: Karakter adalah Player Target
            if (playerMovement != null)
            {
                switch (playerMovement.CurrentState)
                {
                    case TargetMovementState.Sneak:
                        return sneakAnimationSpeed;
                    case TargetMovementState.Walk:
                        return walkAnimationSpeed;
                    case TargetMovementState.Run:
                        return runAnimationSpeed;
                    default:
                        return 0f; // Diam
                }
            }

            // Kasus 2: Karakter adalah NPC NavMeshAgent
            if (navMeshAgent != null)
            {
                float currentSpeed = navMeshAgent.velocity.magnitude;
                if (currentSpeed < 0.05f)
                {
                    return 0f; // NPC berhenti/idle
                }

                // Interpolasi mulus antara kecepatan jalan dan lari sesuai kecepatan agen
                float normalizedSpeed = Mathf.Clamp01(currentSpeed / Mathf.Max(navMeshAgent.speed, 1f));
                return Mathf.Lerp(walkAnimationSpeed, runAnimationSpeed, normalizedSpeed);
            }

            return 0f;
        }

        private void ApplyModelColor()
        {
            if (modelRenderers == null || modelRenderers.Length == 0) return;

            Color targetColor = defaultModelColor;
            if (syncSourceColor && sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            {
                if (!sourceRenderer.sharedMaterial.name.Contains("Default"))
                {
                    targetColor = Application.isPlaying ? sourceRenderer.material.color : sourceRenderer.sharedMaterial.color;
                }
            }

            foreach (Renderer r in modelRenderers)
            {
                if (r == null) continue;
                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", targetColor);
                    if (mat.HasProperty("_Color")) mat.color = targetColor;
                }
            }
        }

        private void DestroyVisual()
        {
            Transform visual = transform.Find(VisualChildName);
            if (visual != null)
            {
                DestroyImmediate(visual.gameObject);
            }
        }
    }
}
