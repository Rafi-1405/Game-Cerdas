using DungeonGenerator.Visuals;
using UnityEngine;

namespace DungeonGenerator.Entities
{
    /// <summary>
    /// Interactive goal shrine that triggers dungeon completion on player arrival.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DungeonGoalTrigger : MonoBehaviour
    {
        #region CONFIG
        public static class GOAL_CONFIG
        {
            // Core animation values
            public const float CORE_ROTATION_SPEED = 45.0f;
            public const float LIGHT_PULSE_SPEED = 2.5f;
            public const float LIGHT_MIN_INTENSITY = 1.0f;
            public const float LIGHT_MAX_INTENSITY = 3.0f;
        }
        #endregion

        [SerializeField] private Transform _crystalCore;
        [SerializeField] private Light _beaconLight;

        private bool _isGoalReached;

        private void Start()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (_crystalCore == null)
            {
                _crystalCore = transform.Find("CrystalCore");
            }

            if (_beaconLight == null)
            {
                _beaconLight = GetComponentInChildren<Light>();
            }
        }

        private void Update()
        {
            // Rotate crystal core
            if (_crystalCore != null)
            {
                _crystalCore.Rotate(Vector3.up, GOAL_CONFIG.CORE_ROTATION_SPEED * Time.deltaTime, Space.World);
            }

            // Pulse beacon light
            if (_beaconLight != null)
            {
                float t = (Mathf.Sin(Time.time * GOAL_CONFIG.LIGHT_PULSE_SPEED) + 1.0f) * 0.5f;
                _beaconLight.intensity = Mathf.Lerp(GOAL_CONFIG.LIGHT_MIN_INTENSITY, GOAL_CONFIG.LIGHT_MAX_INTENSITY, t);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isGoalReached) return;

            if (other.GetComponent<PlayerMovement>() != null || other.CompareTag("Player"))
            {
                _isGoalReached = true;
                DungeonManager.Instance?.OnGoalReached();
            }
        }

        public void ResetGoal()
        {
            _isGoalReached = false;
        }
    }
}
