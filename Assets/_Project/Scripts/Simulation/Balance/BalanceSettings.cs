// -----------------------------------------------------------------------------
//  Framed Drift  -  Simulation.Balance
//  GDD 0.2  secoes 20.2, apendice B
// -----------------------------------------------------------------------------

namespace FramedDrift.Simulation.Balance
{
    /// <summary>
    /// As ~60 constantes das formulas da GDD, em C# puro.
    ///
    /// Este objeto e PRODUZIDO por <c>FramedDrift.Data.BalanceConfig</c> (o asset
    /// editavel sem recompilar exigido pela secao 20.2). Os campos ficam sem valor
    /// aqui de proposito: a fonte da verdade e o asset, nao este arquivo.
    ///
    /// TODO(Fase 0): preencher a partir da planilha de economia; TODO(Fase 1): calibrar
    /// com o prototipo de direcao manual.
    /// </summary>
    [System.Serializable]
    public class BalanceSettings
    {
        // --- grip efetivo (5.2) ------------------------------------------------
        public float GripBase;              // 0.60
        public float GripSpan;              // 0.60
        public float WearGripPenalty;       // 0.35

        // --- velocidade de curva (5.3) ----------------------------------------
        public float ArcadeSpeedFactor;     // k_arcade = 1.25

        // --- grip x drift (5.4) ------------------------------------------------
        public float DriftThresholdBase;    // 55
        public float DriftThresholdCurve;   // 20
        public float DriftSpeedFloor;       // 0.78
        public float DriftSpeedAngleSpan;   // 600  -> v_grip * (0.78 + Angle/600)
        public float TransitionSpeedBonus;  // 0.10

        // --- qualidade de execucao (5.5) --------------------------------------
        public float SkillSigmoidDivisor;   // 18
        public float PerfectCeiling;        // 0.68
        public float BadBase;               // 0.38
        public float QualityMultBad;        // 0.35
        public float QualityMultGood;       // 1.00
        public float QualityMultPerfect;    // 1.60
        public float BadExitSpeedPenalty;   // 0.12

        // --- drift score (6.1) -------------------------------------------------
        public float ScoreBasePerMeter;     // 10
        public float AngleMultBase;         // 0.50
        public float AngleMultSpan;         // 90
        public float SpeedMultDivisor;      // 120
        public float ComboMultStep;         // 0.08
        public int ComboMultCap;            // 25
        public float ProximityMultSpan;     // 0.30
        public float TransitionMult;        // 1.25

        // --- interacao presente (3.6) -----------------------------------------
        /// <summary>Duracao da janela de Entrada Perfeita, em segundos. GDD 3.6.1.</summary>
        public float PerfectEntryWindowSeconds;   // 0.45

        /// <summary>Piso do teto de uplift por presenca. GDD 3.6 / 6.2.</summary>
        public float PresenceUpliftMin;           // 0.20

        /// <summary>Teto do uplift por presenca. Acima disto o jogo vira clicker.</summary>
        public float PresenceUpliftMax;           // 0.30

        // --- camera (19.2) -----------------------------------------------------
        /// <summary>Altura da camera acima da linha de base do Horizon Chase. GDD 19.2.</summary>
        public float CameraHeightMultiplier;      // 1.30 - 1.40, calibrado na Fase 1

        // TODO: adicionar as constantes de 12 (risco e falha), 14 (progressao),
        // 15 (economia) e 17 (offline) conforme cada fase chegar.
    }
}
