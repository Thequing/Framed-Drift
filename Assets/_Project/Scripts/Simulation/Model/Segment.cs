// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Model
//  GDD 0.2  secao 5.1
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Model
{
    /// <summary>
    /// Um trecho de pista. Uma corrida curta tem 18-30 segmentos, ~1,2-2,0 km.
    /// Montado a partir dos modulos autorais da secao 11.2 - nunca por ruido (D-05).
    /// </summary>
    [System.Serializable]
    public struct Segment
    {
        public SegmentType Type;

        /// <summary>Comprimento em metros.</summary>
        public float LengthM;

        /// <summary>Raio da curva em metros. float.PositiveInfinity em retas.</summary>
        public float Radius;

        public TurnDirection Direction;
        public SurfaceType Surface;
        public WallConfig Walls;

        /// <summary>0-100. Alimenta 'challenge' no calculo de qualidade. GDD 5.5.</summary>
        public float Difficulty;

        public bool IsCurve
        {
            get { return Direction != TurnDirection.None && !float.IsPositiveInfinity(Radius); }
        }
    }
}
