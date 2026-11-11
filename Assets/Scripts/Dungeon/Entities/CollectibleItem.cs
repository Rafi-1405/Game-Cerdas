using DungeonGenerator.Visuals;
using UnityEngine;

namespace DungeonGenerator.Entities
{
    /// <summary>
    /// Collectible dungeon item with rotation, bobbing, and trigger interaction.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CollectibleItem : MonoBehaviour
    {
        #region CONFIG
        public static class ITEM_CONFIG
        {
            // Animation values
            public const float ROTATE_SPEED = 90.0f;
            public const float BOB_SPEED = 3.0f;
            public const float BOB_HEIGHT = 0.15f;

            // Trigger radius
            public const float TRIGGER_RADIUS = 0.8f;
        }
        #endregion

        private Vector3 _startPosition;
        private float _timeOffset;
        private bool _isCollected;

        private void Start()
        {
            _startPosition = transform.position;
            _timeOffset = Random.Range(0.0f, 6.28f);

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            // Rotate around vertical Y-axis
            transform.Rotate(Vector3.up, ITEM_CONFIG.ROTATE_SPEED * Time.deltaTime, Space.World);

            // Sine wave floating bob
            float newY = _startPosition.y + (Mathf.Sin((Time.time * ITEM_CONFIG.BOB_SPEED) + _timeOffset) * ITEM_CONFIG.BOB_HEIGHT);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            if (other.GetComponent<PlayerMovement>() != null || other.CompareTag("Player"))
            {
                _isCollected = true;
                DungeonManager.Instance?.OnItemCollected(this);
                Destroy(gameObject);
            }
        }
    }
}
