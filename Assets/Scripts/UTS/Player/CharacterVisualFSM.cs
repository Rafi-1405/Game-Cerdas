using UnityEngine;
using UnityEngine.AI;

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CharacterVisualFSM : MonoBehaviour
    {
        private const string VisualChildName = "FSM_YBot_Visual";

        [Header("Model & Animation Resource Paths")]
        [SerializeField] private string modelResourcePath = "Walking";
        [SerializeField] private string animationResourcePath = "Walking";

        [Header("Transform Offsets")]
        [SerializeField] private Vector3 localPosition = new Vector3(0f, -0.92f, 0f);
        [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 localScale = Vector3.one;

        [Header("Animation Speeds")]
        [SerializeField] private float walkAnimSpeed = 1.2f;
        [SerializeField] private float runAnimSpeed = 1.8f;
        [SerializeField] private float sprintAnimSpeed = 2.2f;

        [Header("Color Settings")]
        [SerializeField] private bool syncStateColor = true;
        [SerializeField] private Color defaultModelColor = Color.white;

        private Renderer sourceRenderer;
        private SimplePlayerController playerController;
        private EnemyFSM enemyFSM;
        private NavMeshAgent navMeshAgent;
        private Renderer[] modelRenderers;
        private AnimationState walkState;

        private void Awake()
        {
            sourceRenderer = GetComponent<Renderer>();
            playerController = GetComponent<SimplePlayerController>();
            enemyFSM = GetComponent<EnemyFSM>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            // Atur warna default otomatis
            if (defaultModelColor == Color.white)
            {
                if (playerController != null) defaultModelColor = new Color(0.15f, 0.65f, 1f, 1f); // Cyan Player
                else if (enemyFSM != null) defaultModelColor = new Color(1f, 0.2f, 0.2f, 1f); // Red Enemy
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

            if (syncStateColor)
            {
                UpdateColor();
            }
        }

        private void InitVisual()
        {
            Transform visual = CreateVisualInstance();
            if (visual == null) return;

            modelRenderers = visual.GetComponentsInChildren<Renderer>();
            SetupAnimation(visual);
            ApplyModelColor(defaultModelColor);
        }

        private Transform CreateVisualInstance()
        {
            if (sourceRenderer != null)
            {
                sourceRenderer.enabled = false;
            }

            Transform existingVisual = transform.Find(VisualChildName);
            if (existingVisual != null) return existingVisual;

            GameObject modelPrefab = Resources.Load<GameObject>(modelResourcePath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"[CharacterVisualFSM] Model '{modelResourcePath}' tidak ditemukan di Assets/Resources.", this);
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
            // Kasus Player
            if (playerController != null)
            {
                if (!playerController.IsMoving) return 0f;
                return Mathf.Lerp(0.8f, runAnimSpeed, playerController.CurrentSpeed / 8f);
            }

            // Kasus Enemy FSM
            if (enemyFSM != null)
            {
                if (enemyFSM.CurrentState == EnemyState.Dead) return 0f;
                if (enemyFSM.CurrentState == EnemyState.Attack) return 0f;

                if (navMeshAgent != null)
                {
                    float currentSpeed = navMeshAgent.velocity.magnitude;
                    if (currentSpeed < 0.05f) return 0f;

                    switch (enemyFSM.CurrentState)
                    {
                        case EnemyState.Patrol:
                            return walkAnimSpeed;
                        case EnemyState.Chase:
                            return runAnimSpeed;
                        case EnemyState.Flee:
                            return sprintAnimSpeed;
                        default:
                            return 1f;
                    }
                }
            }

            return 0f;
        }

        private void UpdateColor()
        {
            if (enemyFSM != null)
            {
                Color stateColor = defaultModelColor;
                switch (enemyFSM.CurrentState)
                {
                    case EnemyState.Patrol:
                        stateColor = new Color(0.2f, 0.85f, 0.2f); // Hijau
                        break;
                    case EnemyState.Chase:
                        stateColor = new Color(1f, 0.55f, 0f); // Oranye
                        break;
                    case EnemyState.Attack:
                        stateColor = new Color(1f, 0.15f, 0.15f); // Merah
                        break;
                    case EnemyState.Flee:
                        stateColor = new Color(1f, 0.92f, 0.01f); // Kuning
                        break;
                    case EnemyState.Dead:
                        stateColor = new Color(0.35f, 0.35f, 0.35f); // Abu-abu
                        break;
                }

                Transform visual = transform.Find(VisualChildName);
                if (visual != null)
                {
                    if (enemyFSM.CurrentState == EnemyState.Dead)
                    {
                        visual.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                        visual.localPosition = new Vector3(0f, -0.85f, 0f);
                    }
                    else
                    {
                        visual.localRotation = Quaternion.Euler(localEulerAngles);
                        visual.localPosition = localPosition;
                    }
                }

                ApplyModelColor(stateColor);
            }
            else
            {
                ApplyModelColor(defaultModelColor);
            }
        }

        private void ApplyModelColor(Color targetColor)
        {
            if (modelRenderers == null || modelRenderers.Length == 0) return;

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
