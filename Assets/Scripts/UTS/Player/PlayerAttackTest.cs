using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Praktikum5.FSM
{
    public class PlayerAttackTest : MonoBehaviour
    {
        [Header("Target & Attack Settings")]
        [SerializeField] private EnemyHealth enemy;
        [SerializeField] private float attackDistance = 3f;
        [SerializeField] private float damage = 20f;

        private void Awake()
        {
            if (enemy == null)
            {
                enemy = FindAnyObjectByType<EnemyHealth>();
            }
        }

        private void Update()
        {
            if (IsAttackTriggered())
            {
                TryAttackEnemy();
            }
        }

        private bool IsAttackTriggered()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public void TryAttackEnemy()
        {
            if (enemy == null)
            {
                enemy = FindAnyObjectByType<EnemyHealth>();
                if (enemy == null) return;
            }

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance <= attackDistance)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"[PlayerAttackTest] Player menyerang Enemy! Jarak: {distance:F2}m (Damage: {damage})", this);
            }
            else
            {
                Debug.Log($"[PlayerAttackTest] Enemy terlalu jauh ({distance:F2}m > {attackDistance}m) untuk diserang.", this);
            }
        }
    }
}
