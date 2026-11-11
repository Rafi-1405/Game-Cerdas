using UnityEngine;

namespace DungeonGenerator.Visuals
{
    /// <summary>
    /// Factory for generating modern Universal Render Pipeline (URP) stylized materials.
    /// </summary>
    public static class DungeonMaterials
    {
        #region CONFIG
        public static class MATERIAL_CONFIG
        {
            // Standard shader names
            public const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";
            public const string FALLBACK_STANDARD_SHADER = "Standard";

            // Color palette definition
            public static readonly Color ROOM_FLOOR_COLOR = new Color(0.18f, 0.20f, 0.25f, 1.0f);     // Dark slate
            public static readonly Color CORRIDOR_FLOOR_COLOR = new Color(0.24f, 0.27f, 0.33f, 1.0f); // Lighter slate
            public static readonly Color WALL_COLOR = new Color(0.11f, 0.13f, 0.17f, 1.0f);           // Deep obsidian
            public static readonly Color WALL_TOP_COLOR = new Color(0.14f, 0.17f, 0.22f, 1.0f);       // Subtle accent
            public static readonly Color START_PAD_COLOR = new Color(0.06f, 0.72f, 0.50f, 1.0f);       // Emerald green
            public static readonly Color GOAL_PAD_COLOR = new Color(0.96f, 0.62f, 0.04f, 1.0f);        // Radiant gold
            public static readonly Color GOAL_EMISSION_COLOR = new Color(1.0f, 0.75f, 0.1f, 1.0f);    // Gold glow
            public static readonly Color ENEMY_COLOR = new Color(0.93f, 0.26f, 0.26f, 1.0f);          // Crimson
            public static readonly Color ITEM_COLOR = new Color(0.23f, 0.51f, 0.96f, 1.0f);           // Sapphire blue
            public static readonly Color ITEM_EMISSION_COLOR = new Color(0.3f, 0.6f, 1.0f, 1.0f);     // Cyan-blue glow
            public static readonly Color PLAYER_COLOR = new Color(0.02f, 0.71f, 0.83f, 1.0f);         // High-tech cyan
            public static readonly Color PLAYER_VISOR_COLOR = new Color(1.0f, 0.95f, 0.3f, 1.0f);     // Visor yellow

            // Material properties
            public const float DEFAULT_SMOOTHNESS = 0.45f;
            public const float DEFAULT_METALLIC = 0.10f;
            public const float EMISSION_INTENSITY = 1.8f;
        }
        #endregion

        private static Shader FindValidShader()
        {
            Shader shader = Shader.Find(MATERIAL_CONFIG.URP_LIT_SHADER);
            if (shader == null)
            {
                shader = Shader.Find(MATERIAL_CONFIG.FALLBACK_STANDARD_SHADER);
            }
            return shader;
        }

        public static Material CreateLitMaterial(Color baseColor, float smoothness = MATERIAL_CONFIG.DEFAULT_SMOOTHNESS, float metallic = MATERIAL_CONFIG.DEFAULT_METALLIC)
        {
            Material mat = new Material(FindValidShader())
            {
                color = baseColor
            };

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", metallic);
            }

            return mat;
        }

        public static Material CreateEmissiveMaterial(Color baseColor, Color emissionColor, float intensity = MATERIAL_CONFIG.EMISSION_INTENSITY)
        {
            Material mat = CreateLitMaterial(baseColor, 0.8f, 0.2f);
            Color finalEmission = emissionColor * intensity;

            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", finalEmission);
            }

            return mat;
        }
    }
}
