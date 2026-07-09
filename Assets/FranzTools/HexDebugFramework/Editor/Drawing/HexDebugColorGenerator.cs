using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework.Editor
{
    public static class HexDebugColorGenerator
    {
        // Il rapporto aureo distribuisce i colori in modo massimamente distante nello spazio HSV.
        private const float GoldenRatioConjugate = 0.618033988749895f;
        private const float Saturation = 0.75f;
        private const float Value = 0.9f;

        public static Color FromId(int id)
        {
            float hue = (id * GoldenRatioConjugate) % 1f;
            return Color.HSVToRGB(hue, Saturation, Value);
        }

        public static Color ForSeverity(DebugSeverity severity)
        {
            switch (severity)
            {
                case DebugSeverity.Error:   return new Color(1f, 0.25f, 0.25f);
                case DebugSeverity.Warning: return new Color(1f, 0.75f, 0.1f);
                default:                    return new Color(0.6f, 0.9f, 0.6f);
            }
        }
    }
}
