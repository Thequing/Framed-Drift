// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Balance
//  GDD 0.2  secao 21.1
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;

namespace FramedDrift.Simulation.Balance
{
    /// <summary>
    /// Roda N corridas com uma configuracao e reporta as distribuicoes.
    ///
    /// "Sem essa ferramenta, balancear este jogo a mao e inviavel. Ela e a peca de tooling
    /// de maior retorno do projeto inteiro e deve vir antes de qualquer conteudo"
    /// (GDD 21.1).
    ///
    /// Mora no assembly puro de proposito: a janela de editor da Fase 3 e os testes de
    /// balanceamento da 21.2 precisam do MESMO relatorio. Se fossem dois codigos, o numero
    /// que o designer ve e o numero que o teste verifica poderiam divergir - e o teste
    /// serviria para nada.
    /// </summary>
    public sealed class BalanceReport
    {
        public string Label;
        public int Races;

        public readonly Stat Time = new Stat();
        public readonly Stat Score = new Stat();
        public readonly Stat Cash = new Stat();
        public readonly Stat Xp = new Stat();
        public readonly Stat Position = new Stat();
        public readonly Stat Risk = new Stat();
        public readonly Stat DriftSegments = new Stat();
        public readonly Stat MaxCombo = new Stat();
        public readonly Stat Damage = new Stat();

        public int Wins;
        public int RacesWithFailure;
        public int TotalFailures;
        public int TotalCollisions;

        /// <summary>Contagem de curvas por qualidade rolada. Indexado por DriftQuality.</summary>
        public readonly long[] QualityCounts = new long[3];

        /// <summary>Curvas que abriram janela de Entrada Perfeita. GDD 3.6.1.</summary>
        public long PromotableCurves;

        public readonly long[] DropsByRarity = new long[5];
        public long TotalDrops;

        /// <summary>Soma dos scores sem e com todas as promocoes. GDD 6.2.</summary>
        public double ScoreWithoutPromotions;
        public double ScoreWithAllPromotions;

        public double WinRate { get { return Races == 0 ? 0 : Wins / (double)Races; } }
        public double FailureRate { get { return Races == 0 ? 0 : RacesWithFailure / (double)Races; } }

        /// <summary>
        /// O uplift da secao 6.2. Precisa cair em [presenceUpliftMin, presenceUpliftMax].
        /// Fora disso, o parafuso e promotableCurveFraction ou a janela de input.
        /// </summary>
        public double Uplift
        {
            get
            {
                if (ScoreWithoutPromotions <= 0.0) return 0.0;
                return (ScoreWithAllPromotions - ScoreWithoutPromotions) / ScoreWithoutPromotions;
            }
        }

        public double CashPerHour
        {
            get
            {
                double seconds = Time.Sum;
                return seconds <= 0.0 ? 0.0 : Cash.Sum / (seconds / 3600.0);
            }
        }

        public double QualityShare(DriftQuality q)
        {
            long total = QualityCounts[0] + QualityCounts[1] + QualityCounts[2];
            return total == 0 ? 0.0 : QualityCounts[(int)q] / (double)total;
        }

        public double DropsPerRace { get { return Races == 0 ? 0 : TotalDrops / (double)Races; } }

        /// <summary>Media, desvio e extremos de uma metrica.</summary>
        public sealed class Stat
        {
            public double Sum;
            public double SumSquares;
            public double Min = double.MaxValue;
            public double Max = double.MinValue;
            public int Count;

            public void Add(double v)
            {
                Sum += v;
                SumSquares += v * v;
                if (v < Min) Min = v;
                if (v > Max) Max = v;
                Count++;
            }

            public double Mean { get { return Count == 0 ? 0.0 : Sum / Count; } }

            public double StdDev
            {
                get
                {
                    if (Count == 0) return 0.0;
                    double variance = SumSquares / Count - Mean * Mean;
                    return variance <= 0.0 ? 0.0 : Math.Sqrt(variance);
                }
            }

            public override string ToString()
            {
                var c = CultureInfo.InvariantCulture;
                return Mean.ToString("N1", c) + " +-" + StdDev.ToString("N1", c)
                       + "  [" + Min.ToString("N1", c) + " .. " + Max.ToString("N1", c) + "]";
            }
        }

        // --- execucao ---------------------------------------------------------------

        /// <summary>
        /// Roda <paramref name="races"/> corridas variando so a seed.
        ///
        /// Nao aplica nenhuma promocao: o relatorio mede o jogo AUSENTE, que e o modo de
        /// uso principal (D-06). O efeito da presenca aparece separado, em
        /// <see cref="Uplift"/>.
        /// </summary>
        public static BalanceReport Run(RaceResolver resolver, RaceInstance prototype,
                                        int races, ulong seedBase, string label)
        {
            var report = new BalanceReport { Label = label, Races = races };
            DriftScorer.Context scoreCtx = resolver.Scorer.BuildContext(prototype);

            for (int i = 0; i < races; i++)
            {
                prototype.Seed = seedBase + (ulong)i * 0x9E3779B97F4A7C15UL;

                RaceResult result = resolver.Simulate(prototype);
                RaceRewards rewards = resolver.Finalize(prototype, result, CollectibleHaul.None);

                report.Time.Add(result.TotalTime);
                report.Score.Add(rewards.DriftScore);
                report.Cash.Add(rewards.Cash);
                report.Xp.Add(rewards.Xp);
                report.Position.Add(result.Position);
                report.Risk.Add(result.RiskIndex);
                report.DriftSegments.Add(result.DriftSegments);
                report.MaxCombo.Add(result.MaxCombo);
                report.Damage.Add(result.Damage);

                if (result.Won) report.Wins++;
                if (result.Failures > 0) report.RacesWithFailure++;
                report.TotalFailures += result.Failures;
                report.TotalCollisions += result.Collisions;

                for (int s = 0; s < result.Timeline.Length; s++)
                {
                    if (!result.Timeline[s].Drift) continue;
                    report.QualityCounts[(int)result.Timeline[s].Quality]++;
                    if (result.Timeline[s].Promotable) report.PromotableCurves++;
                }

                for (int d = 0; d < rewards.Drops.Length; d++)
                {
                    report.DropsByRarity[(int)rewards.Drops[d].Rarity]++;
                    report.TotalDrops++;
                }

                report.ScoreWithoutPromotions += resolver.Scorer.ScoreWithoutPromotions(result, in scoreCtx);
                report.ScoreWithAllPromotions += resolver.Scorer.ScoreWithAllPromotions(result, in scoreCtx);
            }

            return report;
        }

        // --- apresentacao -------------------------------------------------------------

        public override string ToString()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.Append("== ").Append(Label).Append("  (").Append(Races).AppendLine(" corridas)");
            sb.Append("  tempo        ").AppendLine(Time.ToString());
            sb.Append("  drift score  ").AppendLine(Score.ToString());
            sb.Append("  cash         ").AppendLine(Cash.ToString());
            sb.Append("  posicao      ").AppendLine(Position.ToString());
            sb.Append("  risco        ").Append(Risk.ToString()).AppendLine();
            sb.Append("  curvas drift ").AppendLine(DriftSegments.ToString());
            sb.Append("  combo max    ").AppendLine(MaxCombo.ToString());
            sb.Append("  dano         ").AppendLine(Damage.ToString());
            sb.AppendLine();

            sb.Append("  vitorias     ").Append((WinRate * 100).ToString("0.0", c)).AppendLine("%");
            sb.Append("  falha/corrida").Append((FailureRate * 100).ToString("0.00", c)).AppendLine("%");
            sb.Append("  colisoes     ").Append((TotalCollisions / (double)Math.Max(1, Races)).ToString("0.00", c)).AppendLine("/corrida");
            sb.Append("  cash/hora    ").AppendLine(CashPerHour.ToString("N0", c));
            sb.AppendLine();

            sb.Append("  qualidade    Bad ").Append((QualityShare(DriftQuality.Bad) * 100).ToString("0.0", c))
              .Append("%  Good ").Append((QualityShare(DriftQuality.Good) * 100).ToString("0.0", c))
              .Append("%  Perfect ").Append((QualityShare(DriftQuality.Perfect) * 100).ToString("0.0", c))
              .AppendLine("%");
            sb.Append("  promoviveis  ").Append((PromotableCurves / (double)Math.Max(1, Races)).ToString("0.00", c))
              .AppendLine(" curvas/corrida");
            sb.Append("  UPLIFT       ").Append((Uplift * 100).ToString("0.00", c)).AppendLine("%");
            sb.AppendLine();

            sb.Append("  drops        ").Append(DropsPerRace.ToString("0.00", c)).Append("/corrida  [");
            for (int i = 0; i < DropsByRarity.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(((Rarity)i).ToString()).Append(' ').Append(DropsByRarity[i]);
            }
            sb.AppendLine("]");

            return sb.ToString();
        }

        /// <summary>
        /// Linha unica para comparar varias configuracoes lado a lado - o modo em que a
        /// ferramenta e de fato usada (GDD 21.1).
        /// </summary>
        public string ToLine()
        {
            var c = CultureInfo.InvariantCulture;
            return Label.PadRight(28)
                   + Score.Mean.ToString("N0", c).PadLeft(10)
                   + Cash.Mean.ToString("N0", c).PadLeft(9)
                   + Time.Mean.ToString("0.0", c).PadLeft(8) + "s"
                   + (WinRate * 100).ToString("0.0", c).PadLeft(8) + "%"
                   + Risk.Mean.ToString("0.0", c).PadLeft(8)
                   + (FailureRate * 100).ToString("0.00", c).PadLeft(8) + "%"
                   + (Uplift * 100).ToString("0.0", c).PadLeft(8) + "%"
                   + DropsPerRace.ToString("0.00", c).PadLeft(7);
        }

        public static string LineHeader()
        {
            return "configuracao".PadRight(28)
                   + "score".PadLeft(10) + "cash".PadLeft(9) + "tempo".PadLeft(9)
                   + "vitoria".PadLeft(9) + "risco".PadLeft(8) + "falha".PadLeft(9)
                   + "uplift".PadLeft(9) + "drops".PadLeft(7);
        }
    }
}
