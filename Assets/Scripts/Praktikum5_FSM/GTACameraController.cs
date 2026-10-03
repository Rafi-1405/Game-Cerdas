using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum5.FSM
{
    [ExecuteAlways]
    public class GTACameraController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Orbit & Distance")]
        [SerializeField] private float defaultDistance = 4.5f;
        [SerializeField] private float minDistance = 0.8f;
        [SerializeField] private float maxDistance = 7.5f;
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Mouse Sensitivity & Clamping")]
        [SerializeField] private float mouseSensitivityX = 2.5f;
        [SerializeField] private float mouseSensitivityY = 2.0f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 60f;

        [Header("Wall Collision Avoidance (Anti-Tembus Tembok)")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private float collisionBuffer = 0.15f;
        [SerializeField] private float collisionSharpness = 30f;
        [SerializeField] private float returnSharpness = 8f;

        [Header("Pause & Cursor Settings")]
        [SerializeField] private bool autoLockOnStart = true;
        [SerializeField] private bool showPauseGUI = true;

        private float yaw = 0f;
        private float pitch = 20f;
        private float desiredDistance = 4.5f;
        private float currentDistance = 4.5f;
        private bool isPaused = false;
        private Camera camComponent;

        public bool IsPaused => isPaused;
        public Transform Target => target;

        private void Awake()
        {
            camComponent = GetComponent<Camera>();
            if (camComponent != null)
            {
                camComponent.nearClipPlane = 0.1f;
            }

            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            if (collisionLayers == 0)
            {
                collisionLayers = LayerMask.GetMask("Obstacle", "Wall", "Default");
                if (collisionLayers == 0)
                {
                    collisionLayers = ~0; // Semua layer jika tidak ditemukan
                }
            }

            desiredDistance = defaultDistance;
            currentDistance = defaultDistance;

            if (target != null)
            {
                yaw = target.eulerAngles.y;
            }
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
                HandleMouseOrbit();
            }

            UpdateCameraPosition();
        }

        private void HandlePauseAndCursor()
        {
            // Tekan Escape untuk Pause & memunculkan kursor
            if (IsEscapePressed())
            {
                if (!isPaused)
                {
                    PauseGame(true);
                }
            }

            // Jika sedang pause dan pengguna menekan tombol R, restart level
            if (isPaused && IsRestartPressed())
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
                return;
            }

            // Jika sedang pause dan pengguna klik kiri di layar, resume game
            if (isPaused && IsLeftClickPressed())
            {
                PauseGame(false);
            }
        }

        private void PauseGame(bool pause)
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
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + targetOffset;
            Vector3 backwardDir = -(rotation * Vector3.forward);

            float targetDist = desiredDistance;

            // Algoritma Spring-Arm SphereCast: Deteksi dinding penghalang
            if (Physics.SphereCast(pivot, collisionRadius, backwardDir, out RaycastHit hit, desiredDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                // Tarik kamera ke depan dinding (kurangi buffer agar near clip plane tidak tembus)
                targetDist = Mathf.Clamp(hit.distance - collisionBuffer, minDistance, desiredDistance);
            }

            // Interpolasi jarak: Sangat cepat saat terbentur dinding (agar tidak ada 1 frame tembus), halus saat kembali ke ruang terbuka
            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.02f;
            float lerpSpeed = (targetDist < currentDistance) ? collisionSharpness : returnSharpness;
            currentDistance = Mathf.Lerp(currentDistance, targetDist, dt * lerpSpeed);

            transform.position = pivot + backwardDir * currentDistance;
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

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
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

        private bool IsRestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !showPauseGUI || !isPaused) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.yellow;

            float w = 520f;
            float h = 110f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), "GAME PAUSED\n[Klik Kiri]: Lanjut Bermain (Resume)\n[R]: Restart Level", style);
        }
    }
}
