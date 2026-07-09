namespace hp55games.FranzTools.HexDebugFramework
{
    public interface IHexAnalyzer
    {
        string Name { get; }
        DebugCluster[] Analyze(HexGridRegistry registry, IHexTopology topology);
    }
}
