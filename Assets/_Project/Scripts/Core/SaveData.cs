// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secao 20.5
// -----------------------------------------------------------------------------

using System;

namespace FramedDrift.Core
{
    /// <summary>
    /// Save em JSON versionado. Regras da secao 20.5: escrita atomica, 3 backups
    /// rotativos, autosave a cada 60 s, migracao explicita por versao.
    ///
    /// Timestamp em UTC. Se o relogio andar para tras, faz clamp em zero e NAO pune
    /// o jogador - sem acusacao de trapaca (GDD 20.5).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int Version = 1;
        public string LastTimestampUtc;
        public double PlaySeconds;

        // TODO(Fase 6): Currencies, Cars[], Inventory[], Builds[], Fleet[],
        // Automation, Progress, Prestige, Settings. Ver a arvore completa em 20.5.
    }
}
