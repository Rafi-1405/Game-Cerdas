using System;
using UnityEngine;

namespace DeadDepths.AI
{
    /// <summary>
    /// Visual directional indicator that tracks the player during the Charger telegraph phase.
    /// ASD-STE100: This indicator shows the direction of the Charger attack.
    /// Follows Section 5.4 of the specification.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ChargerTelegraph : MonoBehaviour
    {
        #region CONFIG
        [Serializable]
        public class Config
        {
            public float Length = 12f;
            public float Width = 0.6f;
            public Color TelegraphColor = new Color(1f, 0.2f, 0.1f, 0.75f);
        }
        #endregion

        [SerializeField] private Config telegraphConfig = new Config();
        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = telegraphConfig.Width;
            line.endWidth = telegraphConfig.Width * 1.5f;

            // Simple unlit material for line
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = telegraphConfig.TelegraphColor;
            line.material = mat;
            line.startColor = telegraphConfig.TelegraphColor;
            line.endColor = telegraphConfig.TelegraphColor;
            line.enabled = false;
        }

        public void ShowTelegraph(Vector3 start, Vector3 direction)
        {
            if (line == null) line = GetComponent<LineRenderer>();
            line.enabled = true;

            Vector3 groundStart = new Vector3(start.x, start.y + 0.1f, start.z);
            Vector3 groundEnd = groundStart + direction.normalized * telegraphConfig.Length;

            line.SetPosition(0, groundStart);
            line.SetPosition(1, groundEnd);
        }

        public void HideTelegraph()
        {
            if (line != null) line.enabled = false;
        }
    }
}
