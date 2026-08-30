// -----------------------------------------------------------------------------
//  Framed Drift  -  Progression
//  GDD 0.2  secao 14.7
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Core;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;

namespace FramedDrift.Progression
{
    public sealed class Achievement
    {
        public string Id;
        public string DisplayName;
        public string Hint;

        /// <summary>Secreta: nao aparece na lista ate ser conquistada.</summary>
        public bool Secret;
    }

    /// <summary>
    /// Conquistas, incluindo as secretas da secao 14.7.
    ///
    /// Como as missoes, toda conquista precisa ser alcancavel sem o jogador estar
    /// presente - "Perto Demais" e sobre o que o CARRO fez, nao sobre o que o jogador
    /// clicou.
    /// </summary>
    public sealed class AchievementSystem
    {
        private readonly SaveData _save;

        public AchievementSystem(SaveData save)
        {
            _save = save;
        }

        public static readonly Achievement[] All =
        {
            new Achievement { Id = "first_race", DisplayName = "Primeira Volta", Hint = "Termine uma corrida." },
            new Achievement { Id = "first_part", DisplayName = "Ferro-Velho", Hint = "Encontre a primeira peca." },
            new Achievement { Id = "combo_25", DisplayName = "Corrente", Hint = "Chegue a combo x25." },
            new Achievement { Id = "score_1m", DisplayName = "Sete Digitos", Hint = "Faca 1.000.000 numa corrida." },

            // As quatro secretas da secao 14.7.
            new Achievement { Id = "no_lights", DisplayName = "Sem Luzes", Secret = true,
                              Hint = "Venca na madrugada sem sofrer um Bad." },
            new Achievement { Id = "too_close", DisplayName = "Perto Demais", Secret = true,
                              Hint = "Raspe a parede a corrida inteira sem bater." },
            new Achievement { Id = "insane", DisplayName = "Insano", Secret = true,
                              Hint = "Termine uma corrida em risco EXTREMO sem falhar." },
            new Achievement { Id = "garage_king", DisplayName = "Rei da Garagem", Secret = true,
                              Hint = "Tenha a frota inteira operando ao mesmo tempo." },
        };

        public bool Has(string id)
        {
            return _save.Progress.Achievements.Contains(id);
        }

        public bool Grant(string id)
        {
            if (Has(id)) return false;

            Achievement achievement = Find(id);
            if (achievement == null) return false;

            _save.Progress.Achievements.Add(id);
            EventBus.Publish(new UnlockGranted
            {
                Kind = "conquista",
                Id = id,
                DisplayName = achievement.DisplayName,
            });
            return true;
        }

        /// <summary>Avalia uma corrida terminada contra todas as conquistas de corrida.</summary>
        public void Evaluate(RaceResult result, RaceInstance race, float riskIndex,
                             Simulation.Balance.BalanceSettings balance)
        {
            Grant("first_race");

            if (result.MaxCombo >= balance.ComboMultCap) Grant("combo_25");
            if (result.Rewards != null && result.Rewards.DriftScore >= 1000000L) Grant("score_1m");

            if (race.Conditions.TimeOfDay == TimeOfDay.Night && result.Won && result.BadSegments == 0)
                Grant("no_lights");

            if (balance.Band(riskIndex) == RiskBand.Extreme && result.Failures == 0)
                Grant("insane");

            if (result.Collisions == 0 && CountProximity(result) >= 4)
                Grant("too_close");
        }

        private static int CountProximity(RaceResult result)
        {
            if (result.Timeline == null) return 0;
            int n = 0;
            for (int i = 0; i < result.Timeline.Length; i++)
                if (result.Timeline[i].Proximity > 0.4f) n++;
            return n;
        }

        /// <summary>Lista para a UI: secretas so aparecem depois de conquistadas.</summary>
        public List<Achievement> Visible()
        {
            var list = new List<Achievement>();
            for (int i = 0; i < All.Length; i++)
                if (!All[i].Secret || Has(All[i].Id)) list.Add(All[i]);
            return list;
        }

        public static Achievement Find(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }
    }
}
