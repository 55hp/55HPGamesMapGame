namespace hp55games.Tools.HexDebugFramework
{
    public interface IHexAnalyzer
    {
        string Name { get; }
        DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology);
    }
}
