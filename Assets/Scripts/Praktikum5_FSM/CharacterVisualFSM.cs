using UnityEngine;
using UnityEngine.AI;

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CharacterVisualFSM : MonoBehaviour
    {
        private const string VisualChildName = "FSM_YBot_Visual";
        private const string WalkStateName = "FSM_Walk";
        private const string PunchStateName = "FSM_Punch";

        [Header("Model & Animation Resource Paths")]
        [SerializeField] private string modelResourcePath = "Walking";
        [SerializeField] private string animationResourcePath = "Walking";
        [SerializeField] private string punchAnimationResourcePath = "Punching";

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
        private Animation modelAnimation;
        private AnimationState walkState;
        private AnimationState punchState;
        private bool isPunching;

        private void Awake()
        {
            CacheComponents();

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

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheComponents();
            SubscribeEnemyAttack();
        }

        private void OnDisable()
        {
            if (enemyFSM != null)
            {
                enemyFSM.OnAttack -= PlayPunch;
            }
        }

        private void Start()
        {
            if (!Application.isPlaying) return;

            CacheComponents();
            SubscribeEnemyAttack();
            if (transform.Find(VisualChildName) == null || modelAnimation == null || walkState == null)
            {
                DestroyVisual();
                InitVisual();
            }
        }

        private void CacheComponents()
        {
            sourceRenderer = GetComponent<Renderer>();
            playerController = GetComponent<SimplePlayerController>();
            enemyFSM = GetComponent<EnemyFSM>();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void SubscribeEnemyAttack()
        {
            if (enemyFSM == null) return;
            enemyFSM.OnAttack -= PlayPunch;
            enemyFSM.OnAttack += PlayPunch;
        }

        private void Update()
        {
            if (isPunching && modelAnimation != null && !modelAnimation.IsPlaying(PunchStateName))
            {
                isPunching = false;
                if (walkState != null) modelAnimation.Play(WalkStateName);
            }

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
            if (anim == null) anim = visual.GetComponentInChildren<Animation>();
            if (anim == null)
            {
                anim = visual.gameObject.AddComponent<Animation>();
            }
            modelAnimation = anim;
            walkState = null;
            punchState = null;
            isPunching = false;

            // The imported Walking model can keep its clip on the Animation component
            // even when Resources.LoadAll<AnimationClip>("Walking") returns no clips.
            AnimationClip walkClip = anim.clip;
            if (walkClip == null || walkClip.length <= 0f)
            {
                foreach (AnimationState state in anim)
                {
                    if (state.clip == null || state.clip.length <= 0f || state.name == PunchStateName) continue;
                    walkClip = state.clip;
                    break;
                }
            }
            if (walkClip == null || walkClip.length <= 0f)
            {
                foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(animationResourcePath))
                {
                    if (clip == null || clip.length <= 0f) continue;
                    walkClip = clip;
                    break;
                }
            }
            if (walkClip != null && walkClip.length > 0f)
            {
                anim.AddClip(walkClip, WalkStateName);
                walkState = anim[WalkStateName];
                walkState.wrapMode = WrapMode.Loop;
                anim.Play(WalkStateName);
            }

            string punchPath = string.IsNullOrWhiteSpace(punchAnimationResourcePath) ? "Punching" : punchAnimationResourcePath;
            AnimationClip[] punchClips = Resources.LoadAll<AnimationClip>(punchPath);
            foreach (AnimationClip clip in punchClips)
            {
                if (clip == null || clip.length <= 0f) continue;

                anim.AddClip(clip, PunchStateName);
                punchState = anim[PunchStateName];
                punchState.wrapMode = WrapMode.Once;
                break;
            }

            if (walkState == null)
            {
                Debug.LogWarning($"[CharacterVisualFSM] Clip berjalan tidak ditemukan pada model atau Resources/{animationResourcePath}.", this);
            }

            if (punchState == null)
            {
                Debug.LogWarning($"[CharacterVisualFSM] Animasi pukul tidak ditemukan di Resources/{punchPath}.", this);
            }
        }

        public void PlayPunch()
        {
            if (!Application.isPlaying || modelAnimation == null || punchState == null) return;

            modelAnimation.Stop(PunchStateName);
            punchState.time = 0f;
            punchState.speed = 1f;
            isPunching = modelAnimation.Play(PunchStateName, PlayMode.StopAll);
            if (!isPunching && walkState != null) modelAnimation.Play(WalkStateName);
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
