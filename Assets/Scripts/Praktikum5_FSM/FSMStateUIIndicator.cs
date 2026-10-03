using UnityEngine;

namespace Praktikum5.FSM
{
    [RequireComponent(typeof(EnemyFSM))]
    public class FSMStateUIIndicator : MonoBehaviour
    {
        [Header("Position Offset")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.3f, 0f);

        private EnemyFSM fsm;
        private EnemyHealth health;
        private GameObject indicatorObject;
        private TextMesh textMesh;

        private void Awake()
        {
            fsm = GetComponent<EnemyFSM>();
            health = GetComponent<EnemyHealth>();
            SetupTextMesh();
        }

        private void OnEnable()
        {
            if (fsm != null) fsm.OnStateChanged += HandleStateChanged;
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (fsm != null) fsm.OnStateChanged -= HandleStateChanged;
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
        }

        private void SetupTextMesh()
        {
            indicatorObject = new GameObject("FSM_State_Indicator");
            indicatorObject.transform.SetParent(transform);
            indicatorObject.transform.localPosition = offset;
            indicatorObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            textMesh = indicatorObject.AddComponent<TextMesh>();
            textMesh.fontSize = 50;
            textMesh.characterSize = 0.12f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.fontStyle = FontStyle.Bold;

            UpdateDisplayText();
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null && indicatorObject != null)
            {
                indicatorObject.transform.rotation = cam.transform.rotation;
            }
        }

        private void HandleStateChanged(EnemyState state)
        {
            UpdateDisplayText();
        }

        private void HandleHealthChanged(float current, float max)
        {
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            if (textMesh == null || fsm == null) return;

            string stateName = fsm.CurrentState.ToString().ToUpper();
            float curHP = health != null ? health.CurrentHealth : 100f;
            float maxHP = health != null ? health.MaxHealth : 100f;

            textMesh.text = $"[{stateName}]\nHP: {curHP:F0}/{maxHP:F0}";

            switch (fsm.CurrentState)
            {
                case EnemyState.Patrol:
                    textMesh.color = Color.green;
                    break;
                case EnemyState.Chase:
                    textMesh.color = new Color(1f, 0.55f, 0f); // Orange
                    break;
                case EnemyState.Attack:
                    textMesh.color = Color.red;
                    break;
                case EnemyState.Flee:
                    textMesh.color = Color.yellow;
                    break;
                case EnemyState.Dead:
                    textMesh.color = Color.gray;
                    break;
            }
        }
    }
}
