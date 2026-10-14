using UnityEngine;

namespace Praktikum6.BT
{
    [RequireComponent(typeof(EnemyBTController))]
    public class BTActionUIIndicator : MonoBehaviour
    {
        [Header("Position Offset")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.3f, 0f);
        [SerializeField] private bool enableWorldTextMesh = false;

        private EnemyBTController bt;
        private EnemyHealth health;
        private GameObject indicatorObject;
        private TextMesh textMesh;

        private void Awake()
        {
            bt = GetComponent<EnemyBTController>();
            health = GetComponent<EnemyHealth>();
            if (enableWorldTextMesh) SetupTextMesh();
        }

        private void OnEnable()
        {
            if (bt != null) bt.OnActionChanged += HandleActionChanged;
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (bt != null) bt.OnActionChanged -= HandleActionChanged;
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
        }

        private void SetupTextMesh()
        {
            indicatorObject = new GameObject("BT_Action_Indicator");
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

        private void HandleActionChanged(EnemyAction state)
        {
            UpdateDisplayText();
        }

        private void HandleHealthChanged(float current, float max)
        {
            UpdateDisplayText();
        }

        private void UpdateDisplayText()
        {
            if (textMesh == null || bt == null) return;

            string stateName = bt.CurrentAction.ToString().ToUpper();
            float curHP = health != null ? health.CurrentHealth : 100f;
            float maxHP = health != null ? health.MaxHealth : 100f;

            textMesh.text = $"[{stateName}]\nHP: {curHP:F0}/{maxHP:F0}";

            switch (bt.CurrentAction)
            {
                case EnemyAction.Patrol:
                    textMesh.color = Color.green;
                    break;
                case EnemyAction.Chase:
                    textMesh.color = new Color(1f, 0.55f, 0f); // Orange
                    break;
                case EnemyAction.Attack:
                    textMesh.color = Color.red;
                    break;
                case EnemyAction.Flee:
                    textMesh.color = Color.yellow;
                    break;
                case EnemyAction.Dead:
                    textMesh.color = Color.gray;
                    break;
            }
        }
    }
}
