using UnityEngine;

namespace UTS.Inventory
{
    public enum ScavengeItemType
    {
        Bandage,
        FirstAidKit,
        Medkit
    }

    public static class ScavengeItemInfo
    {
        public static string GetName(ScavengeItemType type)
        {
            switch (type)
            {
                case ScavengeItemType.Bandage:
                    return "Bandage";
                case ScavengeItemType.FirstAidKit:
                    return "First Aid Kit";
                case ScavengeItemType.Medkit:
                    return "Medkit";
                default:
                    return "Unknown Item";
            }
        }

        public static int GetValue(ScavengeItemType type)
        {
            switch (type)
            {
                case ScavengeItemType.Bandage:
                    return 10;
                case ScavengeItemType.FirstAidKit:
                    return 20;
                case ScavengeItemType.Medkit:
                    return 30;
                default:
                    return 0;
            }
        }

        public static float GetWeight(ScavengeItemType type)
        {
            switch (type)
            {
                case ScavengeItemType.Bandage:
                    return 3f;
                case ScavengeItemType.FirstAidKit:
                    return 6f;
                case ScavengeItemType.Medkit:
                    return 9f;
                default:
                    return 0f;
            }
        }

        public static Color GetColor(ScavengeItemType type)
        {
            switch (type)
            {
                case ScavengeItemType.Bandage:
                    return new Color(0.62f, 0.62f, 0.62f, 1f); // Grey
                case ScavengeItemType.FirstAidKit:
                    return new Color(0.18f, 0.78f, 0.28f, 1f); // Green
                case ScavengeItemType.Medkit:
                    return new Color(0.68f, 0.18f, 0.88f, 1f); // Purple
                default:
                    return Color.white;
            }
        }

        public static Vector3 GetScale(ScavengeItemType type)
        {
            switch (type)
            {
                case ScavengeItemType.Bandage:
                    return new Vector3(0.4f, 0.4f, 0.4f);
                case ScavengeItemType.FirstAidKit:
                    return new Vector3(0.5f, 0.5f, 0.5f);
                case ScavengeItemType.Medkit:
                    return new Vector3(0.6f, 0.6f, 0.6f);
                default:
                    return new Vector3(0.5f, 0.5f, 0.5f);
            }
        }
    }
}
