using UnityEditor;
using UnityEngine;

namespace hp55games.Tools.HexDebugFramework.Editor
{
    public static class HexSceneDrawer
    {
        private const float DiscRadius = 0.25f;
        private const float LabelVerticalOffset = 0.5f;

        public static void Draw(DebugCluster[] clusters, HexDrawSettings settings, int selectedIndex)
        {
            for (int i = 0; i < clusters.Length; i++)
            {
                var cluster = clusters[i];
                bool isSelected = i == selectedIndex;

                var baseColor = cluster.Severity != DebugSeverity.Info
                    ? HexDebugColorGenerator.ForSeverity(cluster.Severity)
                    : HexDebugColorGenerator.FromId(i);

                Handles.color = isSelected ? Color.yellow : baseColor;

                foreach (var cell in cluster.Cells)
                {
                    Handles.DrawSolidDisc(cell.WorldPosition, Vector3.up, DiscRadius);

                    if (settings.ShowLabels)
                        Handles.Label(cell.WorldPosition + Vector3.up * LabelVerticalOffset, cluster.Name);
                }

                if (settings.ShowConnections)
                {
                    foreach (var edge in cluster.Edges)
                        Handles.DrawLine(edge.From.WorldPosition, edge.To.WorldPosition);
                }
            }
        }
    }
}
