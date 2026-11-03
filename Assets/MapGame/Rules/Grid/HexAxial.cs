using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Coordinata assiale pura (stessa convenzione di HexCoord in Features: flat-top, odd-q).
    /// In Rules le tile si indicizzano per (colonna, riga) offset; questa struct serve per distanze e vicini.
    /// </summary>
    public readonly struct HexAxial : IEquatable<HexAxial>
    {
        public readonly int Q;
        public readonly int R;

        public HexAxial(int q, int r) { Q = q; R = r; }

        private static readonly HexAxial[] Directions =
        {
            new HexAxial(+1, 0), new HexAxial(+1, -1), new HexAxial(0, -1),
            new HexAxial(-1, 0), new HexAxial(-1, +1), new HexAxial(0, +1)
        };

        public static HexAxial FromOffsetOddQ(int col, int row) => new HexAxial(col, row - (col - (col & 1)) / 2);

        public void ToOffsetOddQ(out int col, out int row)
        {
            col = Q;
            row = R + (Q - (Q & 1)) / 2;
        }

        public int DistanceTo(HexAxial o)
        {
            int dq = Math.Abs(Q - o.Q);
            int dr = Math.Abs(R - o.R);
            int ds = Math.Abs((-Q - R) - (-o.Q - o.R));
            return Math.Max(dq, Math.Max(dr, ds));
        }

        public HexAxial Neighbor(int dir)
        {
            var d = Directions[((dir % 6) + 6) % 6];
            return new HexAxial(Q + d.Q, R + d.R);
        }

        public bool Equals(HexAxial o) => Q == o.Q && R == o.R;
        public override bool Equals(object obj) => obj is HexAxial o && Equals(o);
        public override int GetHashCode() => unchecked(Q * 397 ^ R);
        public override string ToString() => "(" + Q + "," + R + ")";
    }

    /// <summary>Utilità su una griglia rettangolare odd-q indicizzata index = row * width + col.</summary>
    public static class HexGridMath
    {
        public static int Index(int col, int row, int width) => row * width + col;

        public static HexAxial ToAxial(int index, int width) => HexAxial.FromOffsetOddQ(index % width, index / width);

        public static int Distance(int a, int b, int width) => ToAxial(a, width).DistanceTo(ToAxial(b, width));

        /// <summary>Indici dei vicini esistenti in griglia, in ordine di direzione 0..5.</summary>
        public static List<int> Neighbors(int index, int width, int height)
        {
            var list = new List<int>(6);
            var a = ToAxial(index, width);
            for (int d = 0; d < 6; d++)
            {
                a.Neighbor(d).ToOffsetOddQ(out int c, out int r);
                if (c >= 0 && c < width && r >= 0 && r < height) list.Add(Index(c, r, width));
            }
            return list;
        }
    }
}
