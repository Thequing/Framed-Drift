// -----------------------------------------------------------------------------
//  Framed Drift  -  Progression
//  GDD 0.2  secoes 14.1, 14.4, 16.1, 22.3
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Progression
{
    /// <summary>Publicado quando qualquer coisa e destravada. A UI transforma em toast.</summary>
    public struct UnlockGranted
    {
        public string Kind;
        public string Id;
        public string DisplayName;
    }

    /// <summary>
    /// Reputacao como LIMIAR, nao como moeda (GDD 15.1): ela nunca e gasta, so
    /// ultrapassada. Cada limiar destrava pista, carro ou degrau de automacao.
    ///
    /// A regra que governa esta classe e a progressao horizontal da secao 14.4: cada
    /// bloco de ~45 min precisa destravar ao menos UM item da lista. Por isso os limiares
    /// sao densos no comeco - a primeira hora da secao 22.3 depende disso.
    /// </summary>
    public sealed class ReputationSystem
    {
        private readonly SaveData _save;
        private readonly ContentDatabase _content;

        public ReputationSystem(SaveData save, ContentDatabase content)
        {
            _save = save;
            _content = content;
        }

        public int Reputation { get { return _save.Reputation; } }

        /// <summary>
        /// A escada de automacao da secao 16.1.
        ///
        /// Cada degrau e conquistado DEPOIS que o jogador sentiu o atrito de fazer aquilo
        /// a mao - automatizar algo que nunca incomodou nao e recompensa. Por isso a
        /// auto-corrida so aparece aos 30-45 min (GDD 22.3), e nao no minuto zero.
        /// </summary>
        public static readonly string[] AutomationLadder =
        {
            "auto_race",       // 30-45 min
            "auto_repair",
            "auto_salvage",
            "auto_equip",
            "auto_build",
            "auto_route",
            "auto_event",
        };

        public static readonly int[] AutomationThresholds = { 8, 14, 20, 30, 42, 58, 75 };

        /// <summary>Reavalia todos os limiares. Chamado sempre que a reputacao sobe.</summary>
        public List<UnlockGranted> Evaluate()
        {
            var granted = new List<UnlockGranted>();

            PromoteTier(granted);

            for (int i = 0; i < _content.TrackList.Count; i++)
            {
                TrackDef track = _content.TrackList[i];
                if (_save.Reputation < track.ReputationRequired) continue;
                if (_save.Progress.UnlockedTracks.Contains(track.Id)) continue;

                _save.Progress.UnlockedTracks.Add(track.Id);
                granted.Add(Publish("pista", track.Id, track.DisplayName));
            }

            for (int i = 0; i < _content.CarList.Count; i++)
            {
                CarDef car = _content.CarList[i];
                if (_save.Progress.UnlockedCars.Contains(car.Id)) continue;
                if (!MeetsUnlock(car)) continue;

                _save.Progress.UnlockedCars.Add(car.Id);
                granted.Add(Publish("carro", car.Id, car.DisplayName));
            }

            for (int i = 0; i < AutomationLadder.Length; i++)
            {
                if (_save.Reputation < AutomationThresholds[i]) continue;
                if (_save.Progress.AutomationUnlocks.Contains(AutomationLadder[i])) continue;

                _save.Progress.AutomationUnlocks.Add(AutomationLadder[i]);
                granted.Add(Publish("automacao", AutomationLadder[i], AutomationLabel(AutomationLadder[i])));
            }

            return granted;
        }

        /// <summary>
        /// Promove o jogador ao maior tier cuja reputacao ele ja alcancou (GDD 14.2).
        ///
        /// `tiers.json` sempre trouxe `reputationRequired` (C 20, B 45, A 70, S 90) e
        /// NADA lia esse campo: `Progress.TierIndex` nascia 0 e so era escrito de volta a
        /// 0 pelo prestigio. Na pratica o jogador ficava preso no tier D para sempre - o
        /// que nao aparecia porque todo o conteudo tambem era tier D.
        ///
        /// So SOBE. Cair de tier por perder reputacao tiraria pecas ja compradas da
        /// vitrine e do inventario util; a unica descida e o prestigio, que reinicia a
        /// temporada inteira de proposito (GDD 14.5).
        /// </summary>
        private void PromoteTier(List<UnlockGranted> granted)
        {
            int current = _save.Progress.TierIndex;
            int target = current;

            for (int i = current + 1; i < _content.Tiers.Length; i++)
            {
                TierDef tier = _content.Tiers[i];
                if (tier == null || _save.Reputation < tier.ReputationRequired) break;
                target = i;
            }

            for (int i = current + 1; i <= target; i++)
                granted.Add(Publish("tier", ((TierRank)i).ToString(), "Tier " + (TierRank)i));

            _save.Progress.TierIndex = target;
        }

        /// <summary>
        /// Um carro so aparece quando TODAS as condicoes da regra batem: reputacao,
        /// blueprint e rival derrotado (GDD 8.2). O carro ainda precisa ser comprado.
        /// </summary>
        public bool MeetsUnlock(CarDef car)
        {
            // A planta do carro (tres derrotas do dono, GDD 13.1) e um ATALHO: ela
            // dispensa a reputacao que o carro pedia. Sem isso, "derrotar o rival te da o
            // carro dele" nao seria recompensa nenhuma - o jogador ja ia receber o mesmo
            // carro por reputacao, e a derrota so teria adiantado o que ja vinha.
            if (_save.Progress.CarBlueprints.Contains(car.Id)) return true;

            UnlockRule rule = car.Unlock;
            if (rule == null) return true;

            if (_save.Reputation < rule.ReputationRequired) return false;

            if (!string.IsNullOrEmpty(rule.BlueprintId)
                && !_save.Progress.Blueprints.Contains(rule.BlueprintId)) return false;

            if (!string.IsNullOrEmpty(rule.RivalDefeatedId)
                && !_save.Progress.RivalsDefeated.Contains(rule.RivalDefeatedId)) return false;

            return true;
        }

        public bool HasAutomation(string id)
        {
            return _save.Progress.AutomationUnlocks.Contains(id);
        }

        public bool TrackUnlocked(string id)
        {
            return _save.Progress.UnlockedTracks.Contains(id);
        }

        /// <summary>Reputacao que falta para o proximo degrau. Alimenta a barra da UI.</summary>
        public int NextThreshold()
        {
            int best = int.MaxValue;

            for (int i = 0; i < _content.TrackList.Count; i++)
            {
                int required = _content.TrackList[i].ReputationRequired;
                if (required > _save.Reputation && required < best) best = required;
            }
            for (int i = 0; i < AutomationThresholds.Length; i++)
            {
                int required = AutomationThresholds[i];
                if (required > _save.Reputation && required < best) best = required;
            }
            for (int i = 0; i < _content.Tiers.Length; i++)
            {
                if (_content.Tiers[i] == null) continue;
                int required = _content.Tiers[i].ReputationRequired;
                if (required > _save.Reputation && required < best) best = required;
            }
            for (int i = 0; i < _content.CarList.Count; i++)
            {
                UnlockRule rule = _content.CarList[i].Unlock;
                if (rule == null) continue;
                if (rule.ReputationRequired > _save.Reputation && rule.ReputationRequired < best)
                    best = rule.ReputationRequired;
            }

            return best == int.MaxValue ? _save.Reputation : best;
        }

        public static string AutomationLabel(string id)
        {
            switch (id)
            {
                case "auto_race": return "Auto-corrida";
                case "auto_repair": return "Auto-reparo";
                case "auto_salvage": return "Auto-desmontar";
                case "auto_equip": return "Auto-equipar";
                case "auto_build": return "Auto-build por pista";
                case "auto_route": return "Auto-rota";
                case "auto_event": return "Auto-evento";
                default: return id;
            }
        }

        private static UnlockGranted Publish(string kind, string id, string name)
        {
            var unlock = new UnlockGranted { Kind = kind, Id = id, DisplayName = name };
            EventBus.Publish(unlock);
            return unlock;
        }
    }
}
