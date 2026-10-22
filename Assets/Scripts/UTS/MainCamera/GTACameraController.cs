using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    public class GTACameraController : MonoBehaviour
    {
        [Header("Target Tracking & Offsets")]
        [SerializeField] private Transform target;
        [Tooltip("Offset kamera saat berdiri normal (relatif terhadap root Player). Default: (0, 0.40, 0)")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.40f, 0f);
        [Tooltip("Offset kamera saat jongkok (relatif terhadap root Player). Default: (0, -0.05, 0)")]
        [SerializeField] private Vector3 crouchOffset = new Vector3(0f, -0.05f, 0f);
        [Tooltip("Kecepatan transisi interpolasi offset saat berdiri/jongkok")]
        [SerializeField] private float offsetTransitionSpeed = 8f;

        [Header("Aim Mode (Right Mouse Button)")]
        [SerializeField] private float aimDistance = 1.8f;
        [SerializeField] private float aimCrouchDistance = 1.6f;
        [SerializeField] private Vector3 aimOffset = new Vector3(0.48f, 0.42f, 0f);
        [SerializeField] private Vector3 aimCrouchOffset = new Vector3(0.48f, 0.05f, 0f);
        [SerializeField] private float aimZoomSpeed = 10f;

        [Header("Orbit & Distance")]
        [SerializeField] private float defaultDistance = 3.2f;
        [SerializeField] private float crouchDistance = 2.8f;
        [SerializeField] private float minDistance = 0.6f;
        [SerializeField] private float maxDistance = 6.0f;
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Mouse Sensitivity & Clamping")]
        [SerializeField] private float mouseSensitivityX = 2.5f;
        [SerializeField] private float mouseSensitivityY = 2.0f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 55f;

        [Header("Wall Collision Avoidance (Anti-Tembus Tembok)")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float collisionRadius = 0.22f;
        [SerializeField] private float collisionBuffer = 0.15f;
        [SerializeField] private float collisionSharpness = 30f;
        [SerializeField] private float returnSharpness = 8f;

        [Header("Indoor Ceiling Clearance Guard")]
        [SerializeField] private bool enableCeilingGuard = true;
        [SerializeField] private float ceilingRaycastHeight = 2.5f;
        [SerializeField] private float ceilingBuffer = 0.25f;

        [Header("Pause & Cursor Settings")]
        [SerializeField] private bool autoLockOnStart = true;

        private float yaw = 0f;
        private float pitch = 12f;
        private float desiredDistance = 3.2f;
        private float currentDistance = 3.2f;
        private bool isPaused = false;
        private bool isAiming = false;
        private Vector3 currentOffset;
        private Camera camComponent;
        private SimplePlayerController playerController;
        private readonly RaycastHit[] collisionHitBuffer = new RaycastHit[16];

        public bool IsPaused => isPaused;
        public bool IsAiming => isAiming;
        public Transform Target => target;
        public Vector3 TargetOffset { get => targetOffset; set => targetOffset = value; }
        public Vector3 CrouchOffset { get => crouchOffset; set => crouchOffset = value; }

        private void Awake()
        {
            camComponent = GetComponent<Camera>();
            if (camComponent != null)
            {
                camComponent.nearClipPlane = 0.1f;
            }

            if (Application.isPlaying && transform.parent != null)
            {
                transform.SetParent(null);
            }

            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            if (target != null)
            {
                playerController = target.GetComponent<SimplePlayerController>();
                yaw = target.eulerAngles.y;
            }

            if (collisionLayers == 0)
            {
                collisionLayers = LayerMask.GetMask("Obstacle", "Wall", "Default");
                if (collisionLayers == 0)
                {
                    collisionLayers = ~0; // Semua layer jika tidak ditemukan
                }
            }

            currentOffset = targetOffset;
            desiredDistance = defaultDistance;
            currentDistance = defaultDistance;
        }

        private void Start()
        {
            if (Application.isPlaying && autoLockOnStart)
            {
                SetCursorLock(true);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            HandlePauseAndCursor();

            if (!isPaused)
            {
                HandleZoomInput();
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Application.isPlaying && !isPaused)
            {
                isAiming = IsAimPressed();
                HandleMouseOrbit();
            }
            else
            {
                isAiming = false;
            }

            UpdateCameraPosition();
        }

        private void HandlePauseAndCursor()
        {
            // Jika zona ekstraksi selesai, biarkan kursor terbuka untuk pop-up kemenangan
            var zone = UTS.Environment.GreenSmokeExtractionZone.Instance;
            if (zone != null && zone.IsExtracted)
            {
                SetCursorLock(false);
                return;
            }

            // Tekan Escape ATAU P untuk Toggle Pause
            if (IsPauseTogglePressed())
            {
                PauseGame(!isPaused);
            }
        }

        public void SetPause(bool pause)
        {
            PauseGame(pause);
        }

        public void PauseGame(bool pause)
        {
            isPaused = pause;
            Time.timeScale = pause ? 0f : 1f;
            SetCursorLock(!pause);
        }

        private void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleZoomInput()
        {
            float scroll = ReadScrollDelta();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                desiredDistance = Mathf.Clamp(desiredDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }
        }

        private void HandleMouseOrbit()
        {
            Vector2 mouseDelta = ReadMouseDelta();

            yaw += mouseDelta.x * mouseSensitivityX;
            pitch -= mouseDelta.y * mouseSensitivityY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void UpdateCameraPosition()
        {
            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.02f;

            if (playerController == null && target != null)
            {
                playerController = target.GetComponent<SimplePlayerController>();
            }
            bool isCrouching = playerController != null && playerController.IsCrouching;

            Vector3 targetOffsetGoal;
            float targetBaseDist;

            if (isAiming)
            {
                targetOffsetGoal = isCrouching ? aimCrouchOffset : aimOffset;
                targetBaseDist = isCrouching ? aimCrouchDistance : aimDistance;
            }
            else
            {
                targetOffsetGoal = isCrouching ? crouchOffset : targetOffset;
                targetBaseDist = isCrouching ? crouchDistance : desiredDistance;
            }

            float transitionSpeed = isAiming ? aimZoomSpeed : offsetTransitionSpeed;
            currentOffset = Vector3.Lerp(currentOffset, targetOffsetGoal, dt * transitionSpeed);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * currentOffset.y + (rotation * Vector3.right) * currentOffset.x;
            Vector3 backwardDir = -(rotation * Vector3.forward);

            float targetDist = targetBaseDist;

            // Algoritma Spring-Arm SphereCast Non-Alloc: Deteksi dinding penghalang (filter target/player)
            int hitCount = Physics.SphereCastNonAlloc(pivot, collisionRadius, backwardDir, collisionHitBuffer, targetBaseDist, collisionLayers, QueryTriggerInteraction.Ignore);
            float closestObstacleDist = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = collisionHitBuffer[i];
                if (hit.collider == null) continue;
                Transform hitTransform = hit.collider.transform;

                // Abaikan jika collider milik player/target atau child-nya
                if (target != null && (hitTransform == target || hitTransform.IsChildOf(target)))
                {
                    continue;
                }

                if (hit.distance < closestObstacleDist)
                {
                    closestObstacleDist = hit.distance;
                }
            }

            if (closestObstacleDist < float.MaxValue)
            {
                // Tarik kamera ke depan dinding (kurangi buffer agar near clip plane tidak tembus)
                targetDist = Mathf.Clamp(closestObstacleDist - collisionBuffer, minDistance, targetBaseDist);
            }

            // Interpolasi jarak: Sangat cepat saat terbentur dinding, halus saat kembali ke ruang terbuka
            float lerpSpeed = (targetDist < currentDistance) ? collisionSharpness : returnSharpness;
            currentDistance = Mathf.Lerp(currentDistance, targetDist, dt * lerpSpeed);

            Vector3 calculatedPos = pivot + backwardDir * currentDistance;

            // Indoor Ceiling Clearance Guard: Cegah kamera menembus plafon atau menukik tajam
            if (enableCeilingGuard)
            {
                if (Physics.Raycast(pivot, Vector3.up, out RaycastHit ceilingHit, ceilingRaycastHeight, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    Transform ceilTransform = ceilingHit.collider.transform;
                    if (target == null || (ceilTransform != target && !ceilTransform.IsChildOf(target)))
                    {
                        float maxAllowedY = ceilingHit.point.y - collisionRadius - ceilingBuffer;
                        if (calculatedPos.y > maxAllowedY)
                        {
                            calculatedPos.y = maxAllowedY;
                        }
                    }
                }
            }

            transform.position = calculatedPos;
            transform.rotation = rotation;
        }

        private Vector2 ReadMouseDelta()
        {
            float mouseX = 0f;
            float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                mouseX = delta.x * 0.1f;
                mouseY = delta.y * 0.1f;
            }
#else
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
#endif
            return new Vector2(mouseX, mouseY);
        }

        private float ReadScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.scroll.ReadValue().y * 0.005f;
            }
            return 0f;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }

        private bool IsPauseTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
        }

        private bool IsLeftClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool IsAimPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private bool IsRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }
}
