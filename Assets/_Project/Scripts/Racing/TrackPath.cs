// -----------------------------------------------------------------------------
//  Framed Drift  -  Racing
//  GDD 0.2  secoes 5.1, 20.6
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Racing
{
    /// <summary>
    /// A linha de centro da pista, derivada dos segmentos.
    ///
    /// Existe porque o simulador nao conhece geometria: para ele um segmento e
    /// comprimento, raio e sentido (GDD 5.1). O tracado 3D e uma CONSEQUENCIA disso,
    /// calculada aqui - e nunca o contrario. Se a geometria fosse a fonte, o resultado
    /// da corrida passaria a depender de como o cenario foi montado, e D-01 cairia.
    /// </summary>
    public sealed class TrackPath
    {
        /// <summary>Amostras a cada ~4 m. Fino o bastante para a curva nao facetar.</summary>
        public const float SampleSpacing = 4f;

        public readonly List<Vector3> Points = new List<Vector3>();
        public readonly List<float> Headings = new List<float>();
        public readonly List<float> Distances = new List<float>();

        /// <summary>Distancia acumulada no INICIO de cada segmento.</summary>
        public readonly List<float> SegmentStarts = new List<float>();

        public float TotalLength { get; private set; }

        public static TrackPath Build(Segment[] segments)
        {
            var path = new TrackPath();
            var position = Vector3.zero;
            float heading = 0f;         // radianos, 0 = +Z
            float distance = 0f;

            path.Points.Add(position);
            path.Headings.Add(heading);
            path.Distances.Add(0f);

            for (int i = 0; i < segments.Length; i++)
            {
                path.SegmentStarts.Add(distance);
                Segment seg = segments[i];

                int steps = Mathf.Max(1, Mathf.CeilToInt(seg.LengthM / SampleSpacing));
                float step = seg.LengthM / steps;

                // Curvatura em radianos por metro. Reta = 0; curva = 1/R, com o sinal do
                // sentido. E a unica traducao entre o modelo do simulador e o mundo.
                float curvature = 0f;
                if (seg.IsCurve && seg.Radius > 0.01f)
                {
                    curvature = 1f / seg.Radius;
                    if (seg.Direction == TurnDirection.Left) curvature = -curvature;
                }

                for (int s = 0; s < steps; s++)
                {
                    heading += curvature * step;
                    position += new Vector3(Mathf.Sin(heading), 0f, Mathf.Cos(heading)) * step;
                    distance += step;

                    path.Points.Add(position);
                    path.Headings.Add(heading);
                    path.Distances.Add(distance);
                }
            }

            path.TotalLength = distance;
            return path;
        }

        /// <summary>Posicao na distancia dada, interpolada entre amostras.</summary>
        public Vector3 PositionAt(float distance)
        {
            int i = IndexAt(distance);
            if (i >= Points.Count - 1) return Points[Points.Count - 1];

            float span = Distances[i + 1] - Distances[i];
            float t = span <= 0.0001f ? 0f : (distance - Distances[i]) / span;
            return Vector3.Lerp(Points[i], Points[i + 1], t);
        }

        /// <summary>Direcao da pista na distancia dada, em graus (Y do transform).</summary>
        public float HeadingDegreesAt(float distance)
        {
            int i = IndexAt(distance);
            if (i >= Headings.Count - 1) return Headings[Headings.Count - 1] * Mathf.Rad2Deg;

            float span = Distances[i + 1] - Distances[i];
            float t = span <= 0.0001f ? 0f : (distance - Distances[i]) / span;
            return Mathf.LerpAngle(Headings[i] * Mathf.Rad2Deg, Headings[i + 1] * Mathf.Rad2Deg, t);
        }

        public float SegmentStart(int index)
        {
            if (SegmentStarts.Count == 0) return 0f;
            return SegmentStarts[Mathf.Clamp(index, 0, SegmentStarts.Count - 1)];
        }

        private int IndexAt(float distance)
        {
            if (Distances.Count < 2) return 0;
            if (distance <= 0f) return 0;
            if (distance >= TotalLength) return Distances.Count - 2;

            // Busca binaria: a pista tem ~500 amostras e isto e consultado por frame.
            int low = 0;
            int high = Distances.Count - 1;
            while (low < high - 1)
            {
                int mid = (low + high) / 2;
                if (Distances[mid] <= distance) low = mid;
                else high = mid;
            }
            return low;
        }
    }
}
