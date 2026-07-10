namespace hp55games.Tools.HexDebugFramework
{
    public readonly struct HexGraphEdge
    {
        public IHexCell From { get; }
        public IHexCell To { get; }

        public HexGraphEdge(IHexCell from, IHexCell to)
        {
            From = from;
            To = to;
        }
    }
}
