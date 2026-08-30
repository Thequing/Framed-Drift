// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Content
//  GDD 0.2  secoes 11.3, 20.2, 21.2
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using FramedDrift.Simulation.Model;

namespace FramedDrift.Simulation.Content
{
    /// <summary>
    /// Falha o carregamento quando o conteudo esta inconsistente.
    ///
    /// Roda no carregamento e nao so nos testes: um id de modulo digitado errado precisa
    /// aparecer como uma frase, na inicializacao, e nao como NullReference no meio de uma
    /// corrida de oito horas sem supervisao.
    /// </summary>
    public static class ContentValidator
    {
        public static void Validate(ContentDatabase db)
        {
            var errors = new List<string>();

            ValidateBalance(db, errors);
            ValidateReferences(db, errors);
            ValidateTracks(db, errors);

            if (errors.Count == 0) return;

            var sb = new StringBuilder();
            sb.Append("Conteudo invalido (").Append(errors.Count).Append("):");
            foreach (string e in errors) sb.Append("\n  - ").Append(e);
            throw new ContentException(sb.ToString());
        }

        private static void ValidateBalance(ContentDatabase db, List<string> errors)
        {
            var b = db.Balance;
            if (b == null) { errors.Add("balance.json nao produziu constantes."); return; }

            Positive(b.ArcadeSpeedFactor, "arcadeSpeedFactor", errors);
            Positive(b.Gravity, "gravity", errors);
            Positive(b.MsToKmh, "msToKmh", errors);
            Positive(b.SkillSigmoidDivisor, "skillSigmoidDivisor", errors);
            Positive(b.ScoreBasePerMeter, "scoreBasePerMeter", errors);
            Positive(b.SpeedMultDivisor, "speedMultDivisor", errors);
            Positive(b.AngleMultSpan, "angleMultSpan", errors);
            Positive(b.DriftSpeedAngleSpan, "driftSpeedAngleSpan", errors);
            Positive(b.CurvatureReferenceRadius, "curvatureReferenceRadius", errors);
            Positive(b.OfflineSampleK, "offlineSampleK", errors);
            Positive(b.GridSize, "gridSize", errors);

            if (b.PositionMultipliers == null || b.PositionMultipliers.Length < b.GridSize)
                errors.Add("positionMultipliers precisa de um valor por posicao do grid (gridSize = " + b.GridSize + ").");

            // O teto da secao 3.6 nao e uma sugestao: abaixo de ~10% ninguem se importa,
            // acima de ~40% o jogo deixa de ser idle.
            if (b.PresenceUpliftMin >= b.PresenceUpliftMax)
                errors.Add("presenceUpliftMin precisa ser menor que presenceUpliftMax.");
            if (b.PresenceUpliftMax > 0.40f)
                errors.Add("presenceUpliftMax acima de 0.40 quebra a regra da secao 3.6 (o jogo deixa de ser idle).");

            if (b.PerfectEntryWindowSeconds <= 0f)
                errors.Add("perfectEntryWindowSeconds precisa ser positivo.");

            // Bad nunca pode pagar mais que Good, e Perfect nunca menos.
            if (b.Quality(DriftQuality.Bad) >= b.Quality(DriftQuality.Good))
                errors.Add("qualityMult.Bad precisa ser menor que qualityMult.Good.");
            if (b.Quality(DriftQuality.Perfect) <= b.Quality(DriftQuality.Good))
                errors.Add("qualityMult.Perfect precisa ser maior que qualityMult.Good.");

            if (b.OfflineCapHoursBase <= 0f)
                errors.Add("offlineCapHoursBase precisa ser positivo (D-02 depende do teto).");
        }

        private static void ValidateReferences(ContentDatabase db, List<string> errors)
        {
            for (int i = 0; i < db.Tiers.Length; i++)
            {
                TierDef t = db.Tiers[i];
                if (t == null) { errors.Add("Tier " + (TierRank)i + " ausente em tiers.json."); continue; }
                if (t.RarityWeights == null || t.RarityWeights.Length != 5)
                    errors.Add("Tier " + t.Rank + ": rarityWeights precisa ter 5 entradas (GDD 10.2).");
            }

            foreach (PartDef p in db.PartList)
            {
                if (string.IsNullOrEmpty(p.Id)) errors.Add("Peca sem id.");
                if (!string.IsNullOrEmpty(p.SetId) && db.SetOrNull(p.SetId) == null)
                    errors.Add("Peca " + p.Id + " aponta para o set inexistente " + p.SetId + ".");
                if (p.Slot == PartSlot.Tires && !p.IsTire)
                    errors.Add("Peca " + p.Id + " ocupa o slot Tires mas nao declara tireProfile (GDD 9.3).");
                if (p.Slot == PartSlot.Differential && !p.IsDifferential)
                    errors.Add("Peca " + p.Id + " ocupa o slot Differential mas nao declara differentialType (GDD 9.2).");
            }

            foreach (CarDef c in db.CarList)
            {
                if (c.SlotProfile == null || c.SlotProfile.Length == 0)
                    errors.Add("Carro " + c.Id + " nao aceita nenhum slot.");
                if (c.BaseStats.Weight <= 0f)
                    errors.Add("Carro " + c.Id + " tem peso zero - PowerRatio explodiria.");
                if (c.Unlock != null && !string.IsNullOrEmpty(c.Unlock.RivalDefeatedId)
                    && !db.Rivals.ContainsKey(c.Unlock.RivalDefeatedId))
                    errors.Add("Carro " + c.Id + " exige derrotar o rival inexistente " + c.Unlock.RivalDefeatedId + ".");
            }

            foreach (RivalDef r in db.RivalList)
            {
                if (!db.Cars.ContainsKey(r.CarId))
                    errors.Add("Rival " + r.Id + " usa o carro inexistente " + r.CarId + ".");
                if (!string.IsNullOrEmpty(r.SignaturePartId) && !db.Parts.ContainsKey(r.SignaturePartId))
                    errors.Add("Rival " + r.Id + " dropa a peca inexistente " + r.SignaturePartId + ".");
            }

            foreach (RegionDef r in db.RegionList)
            {
                foreach (string id in r.ModuleIds)
                    if (!db.Modules.ContainsKey(id))
                        errors.Add("Regiao " + r.Id + " lista o modulo inexistente " + id + ".");
                foreach (string id in r.PartPool)
                    if (!db.Parts.ContainsKey(id))
                        errors.Add("Regiao " + r.Id + " lista a peca inexistente " + id + ".");
                if (r.Weather == null || r.Weather.Length == 0)
                    errors.Add("Regiao " + r.Id + " nao tem clima possivel.");
            }

            foreach (ModuleDef m in db.Modules.Values)
            {
                foreach (string id in m.AllowedNext)
                    if (!db.Modules.ContainsKey(id))
                        errors.Add("Modulo " + m.Id + " permite o sucessor inexistente " + id + ".");
                if (m.Segment.LengthM <= 0f)
                    errors.Add("Modulo " + m.Id + " tem comprimento zero.");
                if (m.Segment.IsCurve && m.Segment.Radius <= 0f)
                    errors.Add("Modulo " + m.Id + " e curva mas tem raio nao positivo.");
            }
        }

        private static void ValidateTracks(ContentDatabase db, List<string> errors)
        {
            foreach (TrackDef t in db.TrackList)
            {
                if (!db.Regions.ContainsKey(t.RegionId))
                    errors.Add("Pista " + t.Id + " aponta para a regiao inexistente " + t.RegionId + ".");

                if (t.ModuleIds == null || t.ModuleIds.Length == 0)
                {
                    errors.Add("Pista " + t.Id + " nao tem modulos.");
                    continue;
                }

                // A escala da secao 5.1: 18-30 segmentos, ~1,2-2,0 km.
                if (t.ModuleIds.Length < 12)
                    errors.Add("Pista " + t.Id + " tem " + t.ModuleIds.Length + " segmentos; a secao 5.1 pede 18-30.");

                for (int i = 0; i < t.ModuleIds.Length; i++)
                {
                    if (!db.Modules.ContainsKey(t.ModuleIds[i]))
                    {
                        errors.Add("Pista " + t.Id + " usa o modulo inexistente " + t.ModuleIds[i] + ".");
                        continue;
                    }

                    if (i == 0) continue;
                    ModuleDef prev = db.Modules[t.ModuleIds[i - 1]];
                    if (prev.AllowedNext.Length == 0) continue;

                    bool ok = false;
                    for (int k = 0; k < prev.AllowedNext.Length; k++)
                        if (prev.AllowedNext[k] == t.ModuleIds[i]) { ok = true; break; }

                    if (!ok)
                        errors.Add("Pista " + t.Id + ": " + t.ModuleIds[i] + " nao pode seguir "
                                   + prev.Id + " (regra de adjacencia, GDD 11.3).");
                }

                if (t.BaseTimeSeconds <= 0f)
                    errors.Add("Pista " + t.Id + " tem baseTimeSeconds nao positivo; o grid da 5.6 depende disso.");
            }
        }

        private static void Positive(float v, string name, List<string> errors)
        {
            if (v <= 0f) errors.Add(name + " precisa ser positivo (esta em " + v + ").");
        }

        private static void Positive(int v, string name, List<string> errors)
        {
            if (v <= 0) errors.Add(name + " precisa ser positivo (esta em " + v + ").");
        }
    }
}
