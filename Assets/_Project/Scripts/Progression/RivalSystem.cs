// -----------------------------------------------------------------------------
//  Framed Drift  -  Progression
//  GDD 0.2  secoes 13.1, 8.2, 14.7
// -----------------------------------------------------------------------------

using FramedDrift.Core;
using FramedDrift.Simulation;
using FramedDrift.Simulation.Content;

namespace FramedDrift.Progression
{
    /// <summary>O que o jogador leva de um encontro com rival. GDD 13.1.</summary>
    public struct RivalReward
    {
        /// <summary>A peca que ele estava usando. Null quando o jogador nao venceu.</summary>
        public string SignaturePartId;

        /// <summary>Derrotas acumuladas contra este rival, ja incluindo esta.</summary>
        public int Defeats;

        /// <summary>
        /// Pedacos da planta da assinatura (GDD 10.6). Zero quando o jogador nao venceu.
        ///
        /// A peca vem inteira na derrota; os pedacos sao para as COPIAS seguintes. Sem
        /// eles, insistir no mesmo rival depois da primeira vitoria nao renderia nada
        /// alem da contagem para a planta de carro.
        /// </summary>
        public int BlueprintFragments;

        /// <summary>A planta do carro dele fechou AGORA? So vale na derrota de numero tres.</summary>
        public bool UnlockedCarBlueprint;

        public string CarBlueprintId;
    }

    /// <summary>
    /// A contagem de rivais e o que ela desbloqueia (GDD 13.1).
    ///
    /// Ate aqui `Progress.RivalsDefeated` era uma lista que TRES sistemas liam e NENHUM
    /// escrevia: a missao semanal "derrote um rival" era incumprivel, a regra de
    /// desbloqueio por rival de <c>MeetsUnlock</c> nunca podia passar, e "derrotar tres
    /// vezes desbloqueia o carro dele" nao tinha onde contar.
    ///
    /// Derrota se mede em Drift Score (D-03). Quem decide isso e
    /// <see cref="RivalOutcome.PlayerWon"/>; aqui so se conta.
    /// </summary>
    public sealed class RivalSystem
    {
        private readonly SaveData _save;
        private readonly ContentDatabase _content;

        public RivalSystem(SaveData save, ContentDatabase content)
        {
            _save = save;
            _content = content;
        }

        public int DefeatsOf(string rivalId)
        {
            SavedRivalRecord record = Find(rivalId);
            return record == null ? 0 : record.Defeats;
        }

        public int EncountersOf(string rivalId)
        {
            SavedRivalRecord record = Find(rivalId);
            return record == null ? 0 : record.Encounters;
        }

        public bool HasCarBlueprint(string carId)
        {
            return _save.Progress.CarBlueprints.Contains(carId);
        }

        /// <summary>Quantas derrotas faltam para a planta do carro dele. Zero quando ja saiu.</summary>
        public int DefeatsUntilCarBlueprint(string rivalId)
        {
            RivalDef rival = _content.RivalOrNull(rivalId);
            if (rival == null || HasCarBlueprint(rival.CarId)) return 0;

            int missing = _content.Balance.RivalDefeatsForCarBlueprint - DefeatsOf(rivalId);
            return missing < 0 ? 0 : missing;
        }

        /// <summary>
        /// Contabiliza um encontro resolvido, ganho ou perdido.
        ///
        /// O ENCONTRO conta sempre; a derrota so quando o jogador venceu no score. Contar
        /// so as vitorias faria a tela do rival dizer "0 encontros" para quem acabou de
        /// perder tres corridas seguidas para ele.
        /// </summary>
        public RivalReward Record(RivalOutcome outcome)
        {
            var reward = new RivalReward();
            if (outcome == null) return reward;

            SavedRivalRecord record = Find(outcome.RivalId);
            if (record == null)
            {
                record = new SavedRivalRecord { Id = outcome.RivalId };
                _save.Progress.Rivals.Add(record);
            }

            record.Encounters++;
            reward.Defeats = record.Defeats;

            if (!outcome.PlayerWon) return reward;

            record.Defeats++;
            reward.Defeats = record.Defeats;
            reward.SignaturePartId = outcome.SignaturePartId;
            reward.BlueprintFragments = _content.Balance.RivalDefeatFragments;

            if (!_save.Progress.RivalsDefeated.Contains(outcome.RivalId))
                _save.Progress.RivalsDefeated.Add(outcome.RivalId);

            EventBus.Publish(new RivalDefeated
            {
                RivalId = outcome.RivalId,
                DisplayName = outcome.RivalDisplayName,
                Defeats = record.Defeats,
                SignaturePartId = outcome.SignaturePartId,
            });

            GrantCarBlueprint(outcome, record, ref reward);
            return reward;
        }

        /// <summary>
        /// "Derrotar o mesmo rival tres vezes desbloqueia seu carro como blueprint"
        /// (GDD 13.1), literal - o numero mora em balance.json, nao aqui.
        /// </summary>
        private void GrantCarBlueprint(RivalOutcome outcome, SavedRivalRecord record,
                                       ref RivalReward reward)
        {
            if (record.Defeats < _content.Balance.RivalDefeatsForCarBlueprint) return;

            string carId = outcome.CarId;
            if (string.IsNullOrEmpty(carId) || _save.Progress.CarBlueprints.Contains(carId)) return;

            _save.Progress.CarBlueprints.Add(carId);

            reward.UnlockedCarBlueprint = true;
            reward.CarBlueprintId = carId;

            CarDef car = _content.CarOrNull(carId);
            EventBus.Publish(new CarBlueprintUnlocked
            {
                CarId = carId,
                DisplayName = car == null ? carId : car.DisplayName,
                RivalId = outcome.RivalId,
            });
        }

        private SavedRivalRecord Find(string rivalId)
        {
            for (int i = 0; i < _save.Progress.Rivals.Count; i++)
                if (_save.Progress.Rivals[i].Id == rivalId) return _save.Progress.Rivals[i];
            return null;
        }
    }
}
