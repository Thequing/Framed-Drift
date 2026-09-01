// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secao 20.5
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Core
{
    /// <summary>Uma peca no inventario. So a seed - a rolagem e reconstruida. GDD 10.1.</summary>
    [Serializable]
    public class SavedPart
    {
        public string BaseId;
        public Rarity Rarity;
        public int ItemLevel;
        public ulong Seed;

        /// <summary>Identidade estavel dentro do save; e como builds referenciam pecas.</summary>
        public int Uid;

        /// <summary>Marcada pelo jogador para nunca ser desmontada automaticamente.</summary>
        public bool Locked;
    }

    /// <summary>Progresso parcial de uma planta. GDD 10.6.</summary>
    [Serializable]
    public class SavedBlueprint
    {
        public string PartId;
        public int Fragments;
    }

    /// <summary>Um preset nomeado: pecas + ajuste fino + estilo. GDD 9.5.</summary>
    [Serializable]
    public class SavedBuild
    {
        public string Name;

        /// <summary>Uid da peca por slot, na ordem de <see cref="PartSlot"/>. 0 = vazio.</summary>
        public int[] SlotUids = new int[8];

        public float TuneLock = 0.5f;
        public float TuneAccel = 0.5f;
        public float TuneDecel = 0.5f;
        public DriftStyle Style = DriftStyle.Balanced;
    }

    /// <summary>Um carro que o jogador possui. GDD 8, 5.7.</summary>
    [Serializable]
    public class SavedCar
    {
        public string CarId;
        public int Xp;

        /// <summary>0-100. Reduz todas as stats proporcionalmente. GDD 5.7.</summary>
        public float Damage;

        public int ActiveBuild;
        public List<SavedBuild> Builds = new List<SavedBuild>();

        /// <summary>Estatisticas do carro - a voz do relatorio fala DELE (GDD 17.5).</summary>
        public int RacesRun;
        public int Wins;
        public long BestScore;
    }

    /// <summary>Uma vaga de garagem: carro -> pista -> regras. GDD 16.4.</summary>
    [Serializable]
    public class SavedFleetSlot
    {
        /// <summary>Indice em <see cref="SaveData.Cars"/>. -1 quando vazia.</summary>
        public int CarIndex = -1;

        public string TrackId;
        public bool Running;
    }

    /// <summary>Regras de automacao, globais ou por carro. GDD 16.2.</summary>
    [Serializable]
    public class SavedAutomation
    {
        public bool AutoStartRace;
        public bool AutoRepair;
        public float AutoRepairThreshold = 40f;
        public bool AutoSalvageCommon;
        public bool AutoSalvageUncommon;
        public bool AutoEquipBetterPart;
        public int EquipObjective;
        public bool AcceptEvents = true;
        public bool AcceptRivalChallenges;
        public bool SwapToRainBuild;
        public float MinimumRewardMultiplier = 1f;
        public float StopIfDamageAbove = 80f;
        public int StopAfterConsecutiveFails = 3;
    }

    /// <summary>
    /// Quantas vezes o jogador encontrou e derrotou um rival. GDD 13.1.
    ///
    /// A CONTAGEM e o campo que faltava: "derrotar o mesmo rival tres vezes desbloqueia
    /// seu carro" nao cabe numa lista de ids, e era isso que RivalsDefeated era.
    /// </summary>
    [Serializable]
    public class SavedRivalRecord
    {
        public string Id;
        public int Encounters;
        public int Defeats;
    }

    /// <summary>Progressao. GDD 14.</summary>
    [Serializable]
    public class SavedProgress
    {
        public string RegionId = "city";
        public string TrackId = "city_loop";
        public int TierIndex;
        public int Stage = 1;

        public List<string> UnlockedTracks = new List<string>();
        public List<string> UnlockedCars = new List<string>();

        /// <summary>Plantas COMPLETAS, prontas para o craft. Uma some ao ser usada.</summary>
        public List<string> Blueprints = new List<string>();

        /// <summary>
        /// Pedacos de planta ainda incompletos: "1/5, 2/5..." da GDD 10.6.
        ///
        /// Separado das completas de proposito. O craft consome a planta INTEIRA, e
        /// guardar as duas coisas na mesma lista faria "tenho a planta" e "estou juntando
        /// a planta" virarem a mesma pergunta - que e exatamente a distincao que faz o
        /// progresso parcial ser legivel na UI.
        /// </summary>
        public List<SavedBlueprint> BlueprintFragments = new List<SavedBlueprint>();

        /// <summary>
        /// Rivais ja derrotados ao menos uma vez.
        ///
        /// Continua existindo, e nao e redundante com <see cref="Rivals"/>: e esta lista
        /// que `UnlockRule.RivalDefeatedId` le (GDD 8.2), e a pergunta que ela responde -
        /// "ja derrotou?" - e diferente de "quantas vezes".
        /// </summary>
        public List<string> RivalsDefeated = new List<string>();

        /// <summary>Encontros e derrotas por rival. GDD 13.1.</summary>
        public List<SavedRivalRecord> Rivals = new List<SavedRivalRecord>();

        /// <summary>
        /// Plantas de CARRO, de "derrotar tres vezes desbloqueia seu carro" (GDD 13.1).
        ///
        /// Lista separada das plantas de peca de proposito: a bancada itera as de peca
        /// para montar o catalogo de craft, e um id de carro no meio delas viraria uma
        /// peca inexistente na tela da garagem.
        /// </summary>
        public List<string> CarBlueprints = new List<string>();
        public List<string> Achievements = new List<string>();

        /// <summary>Degraus da escada de automacao ja comprados. GDD 16.1.</summary>
        public List<string> AutomationUnlocks = new List<string>();
    }

    /// <summary>Prestigio / temporada. GDD 14.5.</summary>
    [Serializable]
    public class SavedPrestige
    {
        public int Season = 1;
        public long Fame;
        public long FameSpent;

        /// <summary>Niveis do upgrade "Turno Extra": +2 h de teto offline cada. GDD 17.3.</summary>
        public int ExtraShiftLevels;

        /// <summary>Multiplicador permanente comprado com Fama. GDD 6.2.</summary>
        public float ScoreMultiplier = 1f;
    }

    [Serializable]
    public class SavedSettings
    {
        public int WindowMode;
        public float MusicVolume = 0.7f;
        public float SfxVolume = 1f;
        public bool ShowDamageNumbers = true;
    }

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
        /// <summary>Suba junto com toda mudanca de formato e escreva a migracao.</summary>
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public string LastTimestampUtc;
        public double PlaySeconds;

        // --- moedas: cinco no jogo inteiro (GDD 15.1) -------------------------
        public long Cash;
        public long Scrap;
        public int Reputation;

        public List<SavedCar> Cars = new List<SavedCar>();
        public List<SavedPart> Inventory = new List<SavedPart>();
        public List<SavedFleetSlot> Fleet = new List<SavedFleetSlot>();

        public SavedAutomation Automation = new SavedAutomation();
        public SavedProgress Progress = new SavedProgress();
        public SavedPrestige Prestige = new SavedPrestige();
        public SavedSettings Settings = new SavedSettings();

        /// <summary>Cap inicial de 60 slots, expansivel. GDD 10.7.</summary>
        public int InventoryCap = 60;

        /// <summary>Proximo uid livre. Nunca reutilizado: uid reciclado quebraria builds.</summary>
        public int NextPartUid = 1;

        /// <summary>Horas do relogio de jogo, 0-24. 1 dia de jogo = 2 h reais. GDD 11.4.</summary>
        public float GameClockHours = 14f;

        public int ActiveCarIndex;

        public DateTime LastTimestamp
        {
            get
            {
                DateTime parsed;
                if (DateTime.TryParse(LastTimestampUtc, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal
                        | System.Globalization.DateTimeStyles.AssumeUniversal, out parsed))
                    return parsed;
                return DateTime.UtcNow;
            }
            set { LastTimestampUtc = value.ToString("o", System.Globalization.CultureInfo.InvariantCulture); }
        }

        public SavedCar ActiveCar
        {
            get
            {
                if (Cars.Count == 0) return null;
                int i = ActiveCarIndex < 0 || ActiveCarIndex >= Cars.Count ? 0 : ActiveCarIndex;
                return Cars[i];
            }
        }

        public SavedPart FindPart(int uid)
        {
            if (uid <= 0) return null;
            for (int i = 0; i < Inventory.Count; i++)
                if (Inventory[i].Uid == uid) return Inventory[i];
            return null;
        }

        public bool InventoryFull { get { return Inventory.Count >= InventoryCap; } }
        public int InventoryFreeSlots { get { return InventoryCap - Inventory.Count; } }
    }
}
