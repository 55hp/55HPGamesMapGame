using System;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Coordinata assiale (q, r) per la griglia esagonale.
    /// La distanza calcolata è la distanza esagonale standard, equivalente concettualmente
    /// alla Chebyshev distance in coordinate cubiche: max(|x|,|y|,|z|).
    /// Usata per query di adiacenza e per il piazzamento pesato di Start/End.
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int Q;
        public int R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        // Coordinate cubiche derivate (x + y + z = 0), usate solo per il calcolo della distanza.
        public int X => Q;
        public int Z => R;
        public int Y => -Q - R;

        public int DistanceTo(HexCoord other)
        {
            int dx = Mathf.Abs(X - other.X);
            int dy = Mathf.Abs(Y - other.Y);
            int dz = Mathf.Abs(Z - other.Z);
            return Mathf.Max(dx, Mathf.Max(dy, dz));
        }

        private static readonly HexCoord[] Directions =
        {
            new HexCoord(+1, 0), new HexCoord(+1, -1), new HexCoord(0, -1),
            new HexCoord(-1, 0), new HexCoord(-1, +1), new HexCoord(0, +1)
        };

        public HexCoord GetNeighbor(int direction) => this + Directions[((direction % 6) + 6) % 6];

        public static HexCoord operator +(HexCoord a, HexCoord b) => new HexCoord(a.Q + b.Q, a.R + b.R);
        
        /// <summary>
        /// Converts offset "odd-q" (col, row) to axial coordinates.
        /// For flat-top hex grids: odd columns are shifted down by half a cell
        /// (consistent with Unity Grid Cell Layout = Hexagon, Cell Swizzle = YXZ = flat-top).
        /// </summary>
        public static HexCoord FromOffsetOddQ(int col, int row)
        {
            int q = col;
            int r = row - (col - (col & 1)) / 2;
            return new HexCoord(q, r);
        }

        public void ToOffsetOddQ(out int col, out int row)
        {
            col = Q;
            row = R + (Q - (Q & 1)) / 2;
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => (Q, R).GetHashCode();
        public override string ToString() => $"({Q},{R})";
    }
}
