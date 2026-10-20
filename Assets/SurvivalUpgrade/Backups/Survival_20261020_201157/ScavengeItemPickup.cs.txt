using UnityEngine;

namespace UTS.Inventory
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class ScavengeItemPickup : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private ScavengeItemType itemType = ScavengeItemType.Bandage;

        [Header("Animation Settings")]
        [SerializeField] private bool enableAnimation = true;
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobFrequency = 2.5f;

        private Vector3 initialPosition;
        private Renderer itemRenderer;
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public ScavengeItemType ItemType => itemType;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            itemRenderer = GetComponent<Renderer>();
            ApplyItemAppearance();
        }

        private void Start()
        {
            initialPosition = transform.position;
            ApplyItemAppearance();
        }

        private void OnValidate()
        {
            ApplyItemAppearance();
        }

        public void SetItemType(ScavengeItemType type)
        {
            itemType = type;
            ApplyItemAppearance();
        }

        private void ApplyItemAppearance()
        {
            if (itemRenderer == null)
            {
                itemRenderer = GetComponent<Renderer>();
            }

            if (itemRenderer != null)
            {
                Color targetColor = ScavengeItemInfo.GetColor(itemType);

                if (propBlock == null)
                {
                    propBlock = new MaterialPropertyBlock();
                }

                itemRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(BaseColorId, targetColor);
                propBlock.SetColor(ColorId, targetColor);
                itemRenderer.SetPropertyBlock(propBlock);
            }

            transform.localScale = ScavengeItemInfo.GetScale(itemType);
        }

        private void Update()
        {
            if (!Application.isPlaying || !enableAnimation) return;

            // Efek berputar
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Efek melayang naik-turun (bobbing)
            float newY = initialPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Application.isPlaying) return;

            // Deteksi player
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory == null && other.attachedRigidbody != null)
            {
                inventory = other.attachedRigidbody.GetComponent<PlayerInventory>();
            }

            if (inventory == null)
            {
                // Cek apakah yang menabrak memiliki SimplePlayerController
                Praktikum5.FSM.SimplePlayerController player = other.GetComponent<Praktikum5.FSM.SimplePlayerController>();
                if (player != null)
                {
                    inventory = player.GetComponent<PlayerInventory>();
                    if (inventory == null)
                    {
                        inventory = player.gameObject.AddComponent<PlayerInventory>();
                    }
                }
            }

            if (inventory != null)
            {
                if (inventory.TryAddItem(itemType))
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ScavengeItemInfo.GetColor(itemType);
            Gizmos.DrawWireCube(transform.position, ScavengeItemInfo.GetScale(itemType) * 1.1f);
        }
    }
}
