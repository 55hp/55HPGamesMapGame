using System;

namespace hp55games.FranzTools.HexDebugFramework
{
    [Flags]
    public enum HexDebugFlags
    {
        None             = 0,
        Road             = 1 << 0,
        River            = 1 << 1,
        Boundary         = 1 << 2,
        PointOfInterest  = 1 << 3,
        City             = 1 << 4,

        // Bit riservati 24-31 per uso specifico di ogni progetto consumer.
        // Ogni progetto assegna il proprio significato a questi bit nel proprio adapter,
        // senza modificare questo file.
        Custom0 = 1 << 24,
        Custom1 = 1 << 25,
        Custom2 = 1 << 26,
        Custom3 = 1 << 27,
        Custom4 = 1 << 28,
        Custom5 = 1 << 29,
        Custom6 = 1 << 30,
        Custom7 = unchecked((int)(1u << 31)),
    }
}
