// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation
//  GDD 0.2  secao 17
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.Simulation.Balance;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using FramedDrift.Simulation.Outcomes;
using FramedDrift.Simulation.Rng;
using FramedDrift.Simulation.Util;

namespace FramedDrift.Simulation
{
    /// <summary>Uma linha do log de retorno. GDD 17.4.</summary>
    public struct OfflineLogEntry
    {
        /// <summary>Segundos desde o inicio da ausencia.</summary>
        public float AtSeconds;
        public string Text;
    }

    /// <summary>O relatorio da tela de retorno. GDD 17.4.</summary>
    public sealed class OfflineReport
    {
        public float ElapsedSeconds;
        public float OperatedSeconds;
        public bool HitCap;

        /// <summary>Preenchido quando a operacao parou por regra, nao por teto. GDD 16.2.</summary>
        public string StoppedReason;
        public float StoppedAtSeconds;

        public int Races;
        public int Wins;
        public int Losses;

        public long DriftScore;
        public long Cash;
        public int Xp;
        public int Reputation;
        public long Scrap;

        public int Failures;
        public float DamageTaken;
        public long RepairCost;

        /// <summary>Contagem por raridade, indexado por <see cref="Rarity"/>.</summary>
        public int[] DropsByRarity = new int[5];

        public List<PartDrop> KeptDrops = new List<PartDrop>();

        /// <summary>Um partId por pedaco de planta juntado na ausencia. GDD 10.6.</summary>
        public List<string> BlueprintFragments = new List<string>();
        public List<OfflineLogEntry> Highlights = new List<OfflineLogEntry>();

        public int TotalDrops
        {
            get
            {
                int n = 0;
                for (int i = 0; i < DropsByRarity.Length; i++) n += DropsByRarity[i];
                return n;
            }
        }
    }

    /// <summary>As regras de automacao que a reconstrucao offline precisa consultar. GDD 16.2.</summary>
    public struct OfflineRules
    {
        public bool AutoRepair;
        public float AutoRepairThreshold;
        public bool AutoSalvageCommon;
        public bool AutoSalvageUncommon;
        public float StopIfDamageAbove;
        public int StopAfterConsecutiveFails;
        public int InventoryFreeSlots;

        public static OfflineRules Default
        {
            get
            {
                return new OfflineRules
                {
                    AutoRepair = false,
                    AutoRepairThreshold = 40f,
                    AutoSalvageCommon = false,
                    AutoSalvageUncommon = false,
                    StopIfDamageAbove = 80f,
                    StopAfterConsecutiveFails = 0,
                    InventoryFreeSlots = int.MaxValue,
                };
            }
        }
    }

    /// <summary>
    /// Agrega N corridas de uma ausencia num unico resultado.
    ///
    /// Roda o MESMO simulador (D-01) - offline rende 100% do que renderia online,
    /// limitado pelo teto de horas (D-02). Coletaveis da secao 3.6.2 NAO sao gerados
    /// aqui: eles nao sao "perdidos" na ausencia, simplesmente nao existem offline.
    ///
    /// Simular 30 dias corrida a corrida e inviavel, entao o metodo e o da secao 17.2:
    /// K corridas de amostra dao media e desvio de cada metrica, e os totais sao
    /// amostrados das distribuicoes correspondentes. O passo que faz o relatorio parecer
    /// VIVO em vez de uma multiplicacao e o 5: as regras de automacao sao aplicadas
    /// sequencialmente sobre o resultado, entao o jogador ve que a operacao parou as
    /// 4h12 e por que.
    /// </summary>
    public sealed class OfflineAggregator
    {
        private readonly ContentDatabase _content;
        private readonly BalanceSettings _balance;
        private readonly RaceResolver _resolver;

        public OfflineAggregator(ContentDatabase content, RaceResolver resolver)
        {
            _content = content;
            _balance = content.Balance;
            _resolver = resolver;
        }

        /// <summary>
        /// Teto acumulavel. Nunca escondido do jogador: ao atingi-lo o relatorio diz
        /// "Sua garagem parou as 08:00" e oferece o upgrade (GDD 17.3).
        /// </summary>
        public float CapSeconds(int extraShiftLevels)
        {
            var b = _balance;
            float hours = b.OfflineCapHoursBase + extraShiftLevels * b.OfflineCapHoursPerUpgrade;
            return MathUtil.Min(hours, b.OfflineCapHoursMax) * 3600f;
        }

        /// <param name="prototype">A configuracao ativa: carro, pista, clima, estilo.</param>
        /// <param name="elapsedSeconds">
        /// Tempo real desde o ultimo save. Ja deve vir grampeado em zero pelo chamador se
        /// o relogio andou para tras - offline nao pune, nem acusa (GDD 20.5).
        /// </param>
        public OfflineReport Aggregate(RaceInstance prototype, float elapsedSeconds,
                                       int extraShiftLevels, OfflineRules rules,
                                       float currentDamage, ulong seed)
        {
            var b = _balance;
            var report = new OfflineReport { ElapsedSeconds = MathUtil.Max(0f, elapsedSeconds) };

            float cap = CapSeconds(extraShiftLevels);
            float operated = MathUtil.Min(report.ElapsedSeconds, cap);
            report.OperatedSeconds = operated;
            report.HitCap = report.ElapsedSeconds > cap;

            if (operated <= 0f) return report;

            var rng = new DeterministicRng(seed ^ 0x0FF11E5EED0FF11EUL);
            Sample sample = RunSamples(prototype, seed);
            if (sample.AvgRaceTime <= 0f) return report;

            float perRace = sample.AvgRaceTime + b.InterRaceDelaySeconds;
            int n = (int)(operated / perRace);
            if (n <= 0) return report;

            // Passo 4: amostra os totais das distribuicoes (GDD 17.2).
            double scoreTotal = SampleNormalTotal(rng, n, sample.AvgScore, sample.SdScore);
            double cashTotal = SampleNormalTotal(rng, n, sample.AvgCash, sample.SdCash);
            double xpTotal = SampleNormalTotal(rng, n, sample.AvgXp, sample.SdXp);

            int wins = rng.Binomial(n, sample.WinRate);
            int failures = rng.Poisson(n * (double)sample.FailuresPerRace);
            float damage = n * sample.DamagePerRace;

            report.Races = n;
            report.Wins = wins;
            report.Losses = n - wins;
            report.DriftScore = (long)scoreTotal;
            report.Cash = (long)cashTotal;
            report.Xp = (int)xpTotal;
            report.Reputation = (int)(wins * (double)b.ReputationPerWin);
            report.Failures = failures;
            report.DamageTaken = damage;

            RollDrops(report, rng, n, sample, prototype);
            RollBlueprintFragments(report, rng, n, prototype);

            // Passo 5: aplica as regras de automacao sequencialmente sobre o resultado.
            ApplyRules(report, rules, currentDamage, perRace, sample);

            return report;
        }

        // --- passo 3: K corridas de amostra --------------------------------------------

        private struct Sample
        {
            public float AvgRaceTime;
            public double AvgScore, SdScore;
            public double AvgCash, SdCash;
            public double AvgXp, SdXp;
            public double WinRate;
            public float FailuresPerRace;

            /// <summary>
            /// Dano medio POR CORRIDA, nao por falha.
            ///
            /// Por corrida e a medida certa porque dano tambem vem de colisao por
            /// proximidade (GDD 12.3), que e muito mais frequente que falha mecanica.
            /// Dividir o dano total pelo numero de falhas inflaria o dano por um fator de
            /// dez e faria a regra "parar se dano > 80" disparar quase sempre.
            /// </summary>
            public float DamagePerRace;

            public double DropsPerRace;
            public double[] RarityShare;
        }

        private Sample RunSamples(RaceInstance prototype, ulong seed)
        {
            var b = _balance;
            int k = b.OfflineSampleK < 1 ? 1 : b.OfflineSampleK;

            double time = 0, score = 0, score2 = 0, cash = 0, cash2 = 0, xp = 0, xp2 = 0;
            double wins = 0, fails = 0, damage = 0, drops = 0;
            var rarity = new double[5];

            for (int i = 0; i < k; i++)
            {
                prototype.Seed = seed + (ulong)i * 0x9E3779B97F4A7C15UL;
                RaceResult r = _resolver.ResolveComplete(prototype);
                RaceRewards rw = r.Rewards;

                time += r.TotalTime;
                score += rw.DriftScore; score2 += (double)rw.DriftScore * rw.DriftScore;
                cash += rw.Cash; cash2 += (double)rw.Cash * rw.Cash;
                xp += rw.Xp; xp2 += (double)rw.Xp * rw.Xp;
                if (r.Won) wins++;
                fails += r.Failures;
                damage += r.Damage;
                drops += rw.Drops.Length;
                for (int d = 0; d < rw.Drops.Length; d++) rarity[(int)rw.Drops[d].Rarity]++;
            }

            var s = new Sample
            {
                AvgRaceTime = (float)(time / k),
                AvgScore = score / k,
                AvgCash = cash / k,
                AvgXp = xp / k,
                WinRate = wins / k,
                FailuresPerRace = (float)(fails / k),
                DamagePerRace = (float)(damage / k),
                DropsPerRace = drops / k,
                RarityShare = rarity,
            };

            s.SdScore = StdDev(score, score2, k);
            s.SdCash = StdDev(cash, cash2, k);
            s.SdXp = StdDev(xp, xp2, k);

            double totalDrops = 0;
            for (int i = 0; i < rarity.Length; i++) totalDrops += rarity[i];
            if (totalDrops > 0)
                for (int i = 0; i < rarity.Length; i++) rarity[i] /= totalDrops;

            return s;
        }

        private static double StdDev(double sum, double sumSq, int n)
        {
            double mean = sum / n;
            double variance = sumSq / n - mean * mean;
            return variance <= 0.0 ? 0.0 : System.Math.Sqrt(variance);
        }

        /// <summary>
        /// Total de n corridas ~ Normal(n*mu, sqrt(n)*sigma). GDD 17.2.
        /// Grampeado em zero: uma cauda negativa nao pode virar cash negativo.
        /// </summary>
        private static double SampleNormalTotal(DeterministicRng rng, int n, double mean, double sd)
        {
            double total = rng.NextGaussian(n * mean, System.Math.Sqrt(n) * sd);
            return total < 0.0 ? 0.0 : total;
        }

        private void RollDrops(OfflineReport report, DeterministicRng rng, int n, Sample sample,
                               RaceInstance prototype)
        {
            int totalDrops = rng.Poisson(n * sample.DropsPerRace);
            if (totalDrops <= 0) return;

            for (int r = 0; r < report.DropsByRarity.Length; r++)
            {
                double share = sample.RarityShare[r];
                if (share <= 0.0) continue;
                report.DropsByRarity[r] = rng.Binomial(totalDrops, share);
            }

            // Pecas Epic+ viram destaque nomeado no log; o resto entra so como contagem.
            // O log completo vale mais que o numero agregado - e ele que cria a relacao
            // com o carro (GDD 17.4).
            for (int r = (int)Rarity.Epic; r < report.DropsByRarity.Length; r++)
            {
                for (int i = 0; i < report.DropsByRarity[r]; i++)
                {
                    var drop = new PartDrop
                    {
                        BaseId = prototype.PartPool[rng.Range(0, prototype.PartPool.Length)],
                        Rarity = (Rarity)r,
                        ItemLevel = _content.Tier((TierRank)prototype.TierIndex).ItemLevelCap,
                        Seed = rng.NextULong(),
                    };
                    report.KeptDrops.Add(drop);
                    report.Highlights.Add(new OfflineLogEntry
                    {
                        AtSeconds = rng.Range(0f, report.OperatedSeconds),
                        Text = _content.Part(drop.BaseId).DisplayName + " " + (Rarity)r,
                    });
                }
            }
        }

        /// <summary>
        /// Pedacos de planta na ausencia (GDD 10.6).
        ///
        /// A ausencia NAO pode render menos que o mesmo tempo jogado - e a promessa D-02.
        /// Como a chance e por corrida e independente, o total de n corridas e binomial;
        /// o alvo usa o MESMO peso inverso do caminho online, senao a planta que a
        /// ausencia junta seria de outra peca que a que o jogo acordado daria.
        /// </summary>
        private void RollBlueprintFragments(OfflineReport report, DeterministicRng rng,
                                            int races, RaceInstance prototype)
        {
            var b = _balance;
            if (b.BlueprintFragmentChance <= 0f || races <= 0) return;

            string[] pool = prototype.PartPool;
            if (pool == null || pool.Length == 0) return;

            int maxTier = MathUtil.Clamp(prototype.TierIndex, 0, (int)TierRank.S);

            var weights = new float[pool.Length];
            bool any = false;
            for (int i = 0; i < pool.Length; i++)
            {
                PartDef def = _content.Part(pool[i]);
                if ((int)def.Tier > maxTier || def.DropWeight <= 0f) continue;
                if (!def.HasBlueprint) continue;   // peca de serie: planta dela nao vale nada

                weights[i] = 1f / def.DropWeight;
                any = true;
            }
            if (!any) return;

            int fragments = rng.Binomial(races, b.BlueprintFragmentChance);
            for (int i = 0; i < fragments; i++)
            {
                int index = rng.WeightedIndex(weights);
                if (index >= 0) report.BlueprintFragments.Add(pool[index]);
            }
        }

        // --- passo 5: regras de automacao --------------------------------------------------

        private void ApplyRules(OfflineReport report, OfflineRules rules, float startingDamage,
                                float secondsPerRace, Sample sample)
        {
            var b = _balance;
            float damage = startingDamage;
            float repairedTotal = 0f;

            // A operacao para quando o dano passa do limite e o auto-reparo esta desligado.
            if (rules.AutoRepair)
            {
                repairedTotal = report.DamageTaken;
                damage = startingDamage;
                report.RepairCost = (long)(repairedTotal * b.RepairCostPerDamage);
                report.Cash -= report.RepairCost;
                if (report.Cash < 0) report.Cash = 0;
            }
            else
            {
                damage += report.DamageTaken;
                if (damage > rules.StopIfDamageAbove && report.DamageTaken > 0f)
                {
                    float fractionUntilStop = MathUtil.Clamp01(
                        (rules.StopIfDamageAbove - startingDamage) / MathUtil.Max(0.01f, report.DamageTaken));

                    StopAt(report, fractionUntilStop,
                           "Dano passou de " + (int)rules.StopIfDamageAbove + "; a operacao parou.");
                }
            }

            // Inventario lotado para a operacao (risco R7). O auto-desmontar existe para
            // isto e sai junto com o inventario, nunca depois (GDD 10.7).
            if (rules.AutoSalvageCommon || rules.AutoSalvageUncommon)
            {
                long scrap = 0;
                if (rules.AutoSalvageCommon)
                {
                    scrap += SalvageBatch(report, Rarity.Common);
                    report.DropsByRarity[(int)Rarity.Common] = 0;
                }
                if (rules.AutoSalvageUncommon)
                {
                    scrap += SalvageBatch(report, Rarity.Uncommon);
                    report.DropsByRarity[(int)Rarity.Uncommon] = 0;
                }
                report.Scrap += scrap;
            }

            if (report.TotalDrops > rules.InventoryFreeSlots)
            {
                float fraction = rules.InventoryFreeSlots / (float)MathUtil.Max(1, report.TotalDrops);
                StopAt(report, fraction, "Inventario lotado; a operacao parou.");
            }
        }

        private long SalvageBatch(OfflineReport report, Rarity rarity)
        {
            int count = report.DropsByRarity[(int)rarity];
            if (count <= 0) return 0;

            var drop = new PartDrop { Rarity = rarity, ItemLevel = 1 };
            return _resolver.Loot.SalvageValue(drop) * count;
        }

        /// <summary>
        /// Trunca o relatorio na fracao em que a operacao parou, e diz por que.
        ///
        /// E este passo, e nao a matematica, que faz o offline parecer vivo: o jogador ve
        /// "parou as 4h12 porque o dano passou de 80", nao um numero menor sem explicacao.
        /// </summary>
        private static void StopAt(OfflineReport report, float fraction, string reason)
        {
            fraction = MathUtil.Clamp01(fraction);
            if (!string.IsNullOrEmpty(report.StoppedReason)) return;

            report.StoppedReason = reason;
            report.StoppedAtSeconds = report.OperatedSeconds * fraction;
            report.OperatedSeconds *= fraction;

            report.Races = (int)(report.Races * fraction);
            report.Wins = (int)(report.Wins * fraction);
            report.Losses = report.Races - report.Wins;
            report.DriftScore = (long)(report.DriftScore * fraction);
            report.Cash = (long)(report.Cash * fraction);
            report.Xp = (int)(report.Xp * fraction);
            report.Reputation = (int)(report.Reputation * fraction);
            report.Scrap = (long)(report.Scrap * fraction);

            for (int i = 0; i < report.DropsByRarity.Length; i++)
                report.DropsByRarity[i] = (int)(report.DropsByRarity[i] * fraction);

            int keep = (int)(report.KeptDrops.Count * fraction);
            if (keep < report.KeptDrops.Count)
                report.KeptDrops.RemoveRange(keep, report.KeptDrops.Count - keep);

            int keepFragments = (int)(report.BlueprintFragments.Count * fraction);
            if (keepFragments < report.BlueprintFragments.Count)
                report.BlueprintFragments.RemoveRange(
                    keepFragments, report.BlueprintFragments.Count - keepFragments);

            report.Highlights.RemoveAll(h => h.AtSeconds > report.StoppedAtSeconds);
        }
    }
}
