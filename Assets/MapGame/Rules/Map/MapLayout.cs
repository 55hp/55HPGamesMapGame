using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>Riga o colonna della plancia (05 §2).</summary>
    public readonly struct LineRef : System.IEquatable<LineRef>
    {
        public readonly bool IsRow;
        public readonly int Index;

        public LineRef(bool isRow, int index) { IsRow = isRow; Index = index; }

        public static LineRef Row(int r) => new LineRef(true, r);
        public static LineRef Col(int c) => new LineRef(false, c);

        /// <summary>Etichetta per il giocatore: colonne A–J, righe 1–10.</summary>
        public string Label => IsRow ? (Index + 1).ToString() : ((char)('A' + Index)).ToString();

        public bool Equals(LineRef o) => IsRow == o.IsRow && Index == o.Index;
        public override bool Equals(object obj) => obj is LineRef o && Equals(o);
        public override int GetHashCode() => (IsRow ? 1000 : 0) + Index;
        public override string ToString() => (IsRow ? "Row " : "Col ") + Label;
    }

    /// <summary>Risultato puro della generazione: contenuto per tile, partenza, Boss, conteggi per gli indizi.</summary>
    public sealed class MapLayout
    {
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public ulong Seed { get; internal set; }
        /// <summary>Contenuto per indice (row * Width + col).</summary>
        public ContentKind[] Tiles { get; internal set; }
        public int StartIndex { get; internal set; }
        public int BossIndex { get; internal set; }
        public bool Success { get; internal set; }
        public string FailureReason { get; internal set; }

        private readonly Dictionary<(ClueTarget, LineRef), int> _clueCounts = new Dictionary<(ClueTarget, LineRef), int>();

        public int Count => Width * Height;

        public ContentKind At(int col, int row) => Tiles[row * Width + col];

        /// <summary>Numero di tile del bersaglio sulla linea, sul contenuto originale (R-093).</summary>
        public int ClueCount(ClueTarget target, LineRef line) =>
            _clueCounts.TryGetValue((target, line), out int n) ? n : 0;

        internal void SetClueCount(ClueTarget target, LineRef line, int n) => _clueCounts[(target, line)] = n;
    }
}
