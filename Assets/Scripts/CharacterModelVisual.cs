using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class CharacterModelVisual : MonoBehaviour
{
    private const string VisualChildName = "YBot_Visual";
    private const string PreviousVisualChildName = "Walking_Visual";

    [SerializeField] private string modelResourcePath = "Walking";
    [SerializeField] private string animationResourcePath = "Walking";
    [SerializeField] private Vector3 localPosition = new Vector3(0f, -1f, 0f);
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [Header("Animation Speed")]
    [SerializeField] private float sneakAnimationSpeed = 0.75f;
    [SerializeField] private float playerWalkAnimationSpeed = 1.25f;
    [SerializeField] private float playerRunAnimationSpeed = 1.75f;
    [SerializeField] private float npcPatrolAnimationSpeed = 1.2f;
    [SerializeField] private float npcChaseAnimationSpeed = 1.75f;

    private Renderer sourceRenderer;
    private PlayerController playerController;
    private SteeringAgent steeringAgent;
    private Renderer[] modelRenderers;
    private AnimationState walkState;

    private void Awake()
    {
        sourceRenderer = GetComponent<Renderer>();
        playerController = GetComponent<PlayerController>();
        steeringAgent = GetComponent<SteeringAgent>();

        if (Application.isPlaying)
        {
            DestroyVisual(VisualChildName);
            DestroyVisual(PreviousVisualChildName);
        }

        Transform visual = CreateVisual();
        if (visual == null)
        {
            return;
        }

        modelRenderers = visual.GetComponentsInChildren<Renderer>();
        PrepareWalkAnimation(visual);
        CopyCurrentColor();
    }

    private void Update()
    {
        if (walkState != null)
        {
            walkState.speed = GetAnimationSpeed();
        }

        CopyCurrentColor();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        DestroyVisual(VisualChildName);
        DestroyVisual(PreviousVisualChildName);

        Transform visual = CreateVisual();
        if (visual == null)
        {
            return;
        }

        modelRenderers = visual.GetComponentsInChildren<Renderer>();
        PrepareWalkAnimation(visual);
        CopyCurrentColor();
    }

    private float GetAnimationSpeed()
    {
        if (playerController != null)
        {
            switch (playerController.CurrentState)
            {
                case PlayerMovementState.Sneak:
                    return sneakAnimationSpeed;
                case PlayerMovementState.Walk:
                    return playerWalkAnimationSpeed;
                case PlayerMovementState.Run:
                    return playerRunAnimationSpeed;
                default:
                    return 0f;
            }
        }

        if (steeringAgent == null || steeringAgent.Velocity.sqrMagnitude < 0.0025f)
        {
            return 0f;
        }

        return Mathf.Lerp(
            npcPatrolAnimationSpeed,
            npcChaseAnimationSpeed,
            Mathf.InverseLerp(2f, 4f, steeringAgent.Velocity.magnitude)
        );
    }

    private Transform CreateVisual()
    {
        if (sourceRenderer != null)
        {
            sourceRenderer.enabled = false;
        }

        Transform existingVisual = transform.Find(VisualChildName);
        if (existingVisual == null)
        {
            existingVisual = transform.Find(PreviousVisualChildName);
            if (existingVisual != null)
            {
                existingVisual.name = VisualChildName;
            }
        }

        if (existingVisual != null)
        {
            return existingVisual;
        }

        GameObject modelPrefab = Resources.Load<GameObject>(modelResourcePath);
        if (modelPrefab == null)
        {
            Debug.LogError("Walking.fbx tidak ditemukan di Assets/Resources.", this);
            return null;
        }

        GameObject visual = Instantiate(modelPrefab, transform);
        visual.name = VisualChildName;
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.Euler(localEulerAngles);
        visual.transform.localScale = localScale;

        return visual.transform;
    }

    private void DestroyVisual(string visualName)
    {
        Transform visual = transform.Find(visualName);
        if (visual != null)
        {
            DestroyImmediate(visual.gameObject);
        }
    }

    private void PrepareWalkAnimation(Transform visual)
    {
        Animation animationComponent = visual.GetComponent<Animation>();
        if (animationComponent == null)
        {
            animationComponent = visual.gameObject.AddComponent<Animation>();
        }

        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(animationResourcePath))
        {
            if (clip == null || clip.length <= 0f)
            {
                continue;
            }

            clip.wrapMode = WrapMode.Loop;
            animationComponent.AddClip(clip, clip.name);
            animationComponent.Play(clip.name);
            walkState = animationComponent[clip.name];
            break;
        }

        if (walkState == null)
        {
            Debug.LogError($"{animationResourcePath}.fbx tidak memiliki clip animasi.", this);
        }
    }

    private void CopyCurrentColor()
    {
        if (sourceRenderer == null || modelRenderers == null)
        {
            return;
        }

        Color sourceColor = sourceRenderer.material.color;
        foreach (Renderer modelRenderer in modelRenderers)
        {
            foreach (Material material in modelRenderer.materials)
            {
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", sourceColor);
                }

                if (material.HasProperty("_Color"))
                {
                    material.color = sourceColor;
                }
            }
        }
    }
}
