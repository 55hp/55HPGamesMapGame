using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework.Editor
{
    /// <summary>
    /// Stato condiviso tra Editor Window, Scene Drawer e Scene Toolbar.
    /// La topology viene iniettata dal progetto consumer via RegisterTopology,
    /// poiché il framework non conosce le classi concrete del progetto.
    /// </summary>
    public static class HexDebugSession
    {
        public static IHexTopology Topology { get; private set; }
        public static HexGridRegistry Registry { get; } = new HexGridRegistry();
        public static DebugCluster[] Clusters { get; set; }
        public static int SelectedClusterIndex { get; set; } = -1;
        public static HexDrawSettings DrawSettings { get; } = new HexDrawSettings();

        public static void RegisterTopology(IHexTopology topology) => Topology = topology;

        public static void ClearClusters()
        {
            Clusters = null;
            SelectedClusterIndex = -1;
        }
    }

    public sealed class HexDrawSettings
    {
        public bool Enabled = true;
        public bool ShowLabels = true;
        public bool ShowConnections = true;
    }
}
