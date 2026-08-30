// -----------------------------------------------------------------------------
//  Framed Drift  -  Progression
//  GDD 0.2  secoes 14.5, 17.3
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using UnityEngine;

namespace FramedDrift.Progression
{
    /// <summary>Um no da arvore de Fama. GDD 14.5.</summary>
    public sealed class FameUpgrade
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int MaxLevel;
        public long BaseCost;
        public float CostGrowth;

        public long CostAt(int level)
        {
            return (long)(BaseCost * Mathf.Pow(CostGrowth, level));
        }
    }

    /// <summary>
    /// Prestigio: "Nova Temporada" ao atingir Reputacao 100.
    ///
    /// A Fama compra melhorias numa ARVORE, nao um multiplicador linear (GDD 14.5). A
    /// diferenca importa: um multiplicador linear so responde "quanto mais forte", e a
    /// arvore responde "mais forte EM QUE" - comecar mais rico, mais rapido, com melhor
    /// loot, ou com automacao mais avancada. E a mesma logica do pilar P4 aplicada ao
    /// meta-jogo.
    /// </summary>
    public sealed class PrestigeSystem
    {
        public const int ReputationToPrestige = 100;

        private readonly SaveData _save;

        public PrestigeSystem(SaveData save)
        {
            _save = save;
        }

        public static readonly FameUpgrade[] Tree =
        {
            new FameUpgrade
            {
                Id = "extra_shift",
                DisplayName = "Turno Extra",
                Description = "+2 h no teto de operacao sem supervisao.",
                MaxLevel = 8, BaseCost = 3, CostGrowth = 1.6f,
            },
            new FameUpgrade
            {
                Id = "score_mult",
                DisplayName = "Reputacao de Rua",
                Description = "+8% de Drift Score permanente.",
                MaxLevel = 10, BaseCost = 2, CostGrowth = 1.5f,
            },
            new FameUpgrade
            {
                Id = "head_start",
                DisplayName = "Caixa Dois",
                Description = "Comeca a temporada com mais cash.",
                MaxLevel = 5, BaseCost = 2, CostGrowth = 1.8f,
            },
            new FameUpgrade
            {
                Id = "loot_quality",
                DisplayName = "Contatos no Ferro-Velho",
                Description = "+5% de chance de drop raro.",
                MaxLevel = 6, BaseCost = 4, CostGrowth = 1.7f,
            },
        };

        public bool CanPrestige { get { return _save.Reputation >= ReputationToPrestige; } }

        /// <summary>
        /// Fama que a temporada atual renderia. Escala com a reputacao acumulada, entao
        /// esticar a temporada alem do limiar vale a pena - reiniciar precisa ser
        /// DESEJAVEL, nunca punitivo (criterio de saida da Fase 14).
        /// </summary>
        public long PendingFame
        {
            get
            {
                if (!CanPrestige) return 0L;
                return (long)Mathf.Floor(Mathf.Sqrt(_save.Reputation / 10f) * 3f);
            }
        }

        public int LevelOf(string upgradeId)
        {
            switch (upgradeId)
            {
                case "extra_shift": return _save.Prestige.ExtraShiftLevels;
                case "score_mult": return Mathf.RoundToInt((_save.Prestige.ScoreMultiplier - 1f) / 0.08f);
                default: return 0;
            }
        }

        public bool Buy(string upgradeId)
        {
            FameUpgrade upgrade = Find(upgradeId);
            if (upgrade == null) return false;

            int level = LevelOf(upgradeId);
            if (level >= upgrade.MaxLevel) return false;

            long cost = upgrade.CostAt(level);
            long available = _save.Prestige.Fame - _save.Prestige.FameSpent;
            if (available < cost) return false;

            _save.Prestige.FameSpent += cost;

            switch (upgradeId)
            {
                case "extra_shift": _save.Prestige.ExtraShiftLevels++; break;
                case "score_mult": _save.Prestige.ScoreMultiplier += 0.08f; break;
            }

            return true;
        }

        /// <summary>
        /// Reinicia a temporada.
        ///
        /// PERDE: cash, pecas, nivel dos carros, progresso de regiao.
        /// MANTEM: carros desbloqueados, blueprints, cosmeticos, conquistas.
        /// GANHA: Fama.
        ///
        /// A lista do que se mantem e o que faz o reinicio parecer avanco e nao perda -
        /// o jogador volta com a colecao inteira e uma garagem melhor.
        /// </summary>
        public void Prestige(List<string> keepCarIds)
        {
            if (!CanPrestige) return;

            _save.Prestige.Fame += PendingFame;
            _save.Prestige.Season++;

            _save.Cash = 0;
            _save.Scrap = 0;
            _save.Reputation = 0;

            _save.Inventory.Clear();
            _save.NextPartUid = 1;

            for (int i = _save.Cars.Count - 1; i >= 0; i--)
            {
                SavedCar car = _save.Cars[i];
                car.Xp = 0;
                car.Damage = 0f;
                car.Builds.Clear();
                car.ActiveBuild = 0;

                // Carros desbloqueados permanecem (GDD 14.5); os NAO desbloqueados nesta
                // temporada saem da garagem, nao da colecao.
                if (keepCarIds != null && !keepCarIds.Contains(car.CarId))
                    _save.Cars.RemoveAt(i);
            }

            _save.Progress.RegionId = "city";
            _save.Progress.TrackId = "city_loop";
            _save.Progress.TierIndex = 0;
            _save.Progress.Stage = 1;
            _save.Progress.UnlockedTracks.Clear();
            _save.Progress.AutomationUnlocks.Clear();

            EventBus.Publish(new UnlockGranted
            {
                Kind = "temporada",
                Id = "season_" + _save.Prestige.Season,
                DisplayName = "Temporada " + _save.Prestige.Season,
            });
        }

        public static FameUpgrade Find(string id)
        {
            for (int i = 0; i < Tree.Length; i++)
                if (Tree[i].Id == id) return Tree[i];
            return null;
        }
    }
}
