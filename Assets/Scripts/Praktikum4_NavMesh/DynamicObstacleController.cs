using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum4.NavMeshNavigation
{
    [RequireComponent(typeof(NavMeshObstacle))]
    public class DynamicObstacleController : MonoBehaviour
    {
        [Header("Carve Settings")]
        [SerializeField] private bool carveOnStart = false;

        [Header("Hotkeys (In-Game Toggle)")]
        [Tooltip("Tekan tombol C atau Space saat Play Mode untuk toggle Carve ON / OFF")]
        [SerializeField] private bool enableKeyboardToggle = true;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer obstacleRenderer;
        [SerializeField] private Color carveOnColor = new Color(0.1f, 0.85f, 0.3f);  // Hijau (Carve Aktif - Memotong NavMesh)
        [SerializeField] private Color carveOffColor = new Color(0.9f, 0.2f, 0.2f); // Merah (Carve Nonaktif - RVO Only)

        private NavMeshObstacle obstacle;
        private Material instanceMaterial;

        public bool IsCarving => obstacle != null && obstacle.carving;

        private void Awake()
        {
            obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = Vector3.one;
                obstacle.carveOnlyStationary = false; // Memotong NavMesh seketika tanpa delay
            }

            if (obstacleRenderer == null)
            {
                obstacleRenderer = GetComponent<Renderer>();
            }

            if (obstacleRenderer != null)
            {
                // Menggunakan instance material agar tidak merusak aset material asli
                instanceMaterial = obstacleRenderer.material;
            }
        }

        private void Start()
        {
            SetCarving(carveOnStart);
        }

        private void Update()
        {
            if (!enableKeyboardToggle) return;

            if (IsTogglePressed())
            {
                ToggleCarve();
            }
        }

        private bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard.cKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
            }
            return false;
#else
            return Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        [ContextMenu("Toggle Carve")]
        public void ToggleCarve()
        {
            if (obstacle == null) return;
            SetCarving(!obstacle.carving);
        }

        public void SetCarving(bool state)
        {
            if (obstacle == null) return;

            obstacle.carving = state;
            UpdateVisualColor();

            Debug.Log($"[NavMeshObstacle] Carve: {(state ? "ON (Memotong Lubang di NavMesh)" : "OFF (NavMesh Tetap Utuh / RVO Mode)")} | Objek: {gameObject.name}", this);
        }

        private void UpdateVisualColor()
        {
            if (instanceMaterial == null) return;

            Color targetColor = obstacle.carving ? carveOnColor : carveOffColor;
            if (instanceMaterial.HasProperty("_BaseColor"))
            {
                instanceMaterial.SetColor("_BaseColor", targetColor);
            }
            if (instanceMaterial.HasProperty("_Color"))
            {
                instanceMaterial.color = targetColor;
            }
        }

        private void OnDestroy()
        {
            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
            }
        }
    }
}
