// -----------------------------------------------------------------------------
//  Framed Drift  -  Garage
//  GDD 0.2  secoes 8, 9, 5.7
// -----------------------------------------------------------------------------

using System;

namespace FramedDrift.Garage
{
    /// <summary>Um carro que o jogador possui: build, dano, XP e estado continuo.</summary>
    [Serializable]
    public class CarInstance
    {
        public string CarDataId;
        public int Xp;

        /// <summary>0-100. Reduz todas as stats proporcionalmente. GDD 5.7.</summary>
        public float Damage;

        /// <summary>Indice da build ativa em Builds.</summary>
        public int ActiveBuild;

        // TODO(Fase 7/8): pecas equipadas por slot, valores de ajuste fino do
        // diferencial (trava / aceleracao / desaceleracao - GDD 9.2) e estilo.
    }
}
