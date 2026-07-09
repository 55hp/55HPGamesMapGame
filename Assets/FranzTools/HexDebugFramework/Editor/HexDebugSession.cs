using System;
using System.Collections.Generic;
using UnityEngine;

namespace hp55games.FranzTools.HexDebugFramework.Editor
{
    /// <summary>
    /// Stato condiviso tra Editor Window, Scene Drawer e Scene Toolbar.
    /// La topology e la populate strategy vengono iniettate dal progetto consumer.
    /// Gli analyzer registrati appaiono nel dropdown della Editor Window.
    /// </summary>
    public static class HexDebugSession
    {
        public static IHexTopology Topology { get; private set; }
        public static HexGridRegistry Registry { get; } = new HexGridRegistry();
        public static DebugCluster[] Clusters { get; set; }
        public static int SelectedClusterIndex { get; set; } = -1;
        public static HexDrawSettings DrawSettings { get; } = new HexDrawSettings();

        // Strategia di populate iniettata dal progetto consumer.
        // Se null, la Editor Window usa PopulateFromScene() come fallback.
        public static Action<HexGridRegistry> PopulateStrategy { get; set; }

        // Analyzer registrati dal progetto consumer (es. MapGame Phase 5).
        private static readonly List<IHexAnalyzer> _registeredAnalyzers = new List<IHexAnalyzer>();
        public static IReadOnlyList<IHexAnalyzer> RegisteredAnalyzers => _registeredAnalyzers;

        public static void RegisterTopology(IHexTopology topology) => Topology = topology;
        public static void RegisterAnalyzer(IHexAnalyzer analyzer) => _registeredAnalyzers.Add(analyzer);

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
