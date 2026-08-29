// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 11.2
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>Dado artesanal. GDD 11.2. Campos de arte ficam vazios de proposito.</summary>
    [CreateAssetMenu(menuName = "Framed Drift/Track Module", fileName = "ModuleData")]
    public sealed class ModuleData : ScriptableObject
    {
        public string Id;

        [Tooltip("O segmento que este modulo representa na simulacao. GDD 5.1.")]
        public Segment Segment;

        // TODO: regras de adjacencia (GDD 11.2 - quais modulos podem se seguir)

        [Header("Arte")]
        [Tooltip("Deixado em branco - a geometria entra depois. " +
                 "D-05: modulos autorais, nunca ruido procedural.")]
        public GameObject Prefab;
    }
}
