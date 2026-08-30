// -----------------------------------------------------------------------------
//  Framed Drift  -  Core
//  GDD 0.2  secoes 11.4, 17.1, 20.4
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.Core
{
    /// <summary>
    /// O relogio do jogo e o orcamento de CPU por estado de execucao.
    ///
    /// 1 dia de jogo = 2 horas reais (GDD 11.4). Isso da ritmo ao dia e cria janelas de
    /// oportunidade - "a madrugada paga x1,9" - SEM exigir presenca: o jogador pode
    /// esperar, pagar para forcar um horario, ou simplesmente deixar a automacao pegar a
    /// janela por ele (D-06).
    /// </summary>
    public sealed class TimeManager
    {
        private readonly float _realSecondsPerGameDay;

        public TimeManager(float realSecondsPerGameDay)
        {
            _realSecondsPerGameDay = realSecondsPerGameDay <= 0f ? 7200f : realSecondsPerGameDay;
        }

        /// <summary>Hora do jogo, 0-24.</summary>
        public float ClockHours { get; private set; }

        public void SetClock(float hours)
        {
            ClockHours = Mathf.Repeat(hours, 24f);
        }

        public void Tick(float deltaSeconds)
        {
            ClockHours = Mathf.Repeat(ClockHours + deltaSeconds * (24f / _realSecondsPerGameDay), 24f);
        }

        /// <summary>Avanca o relogio por uma ausencia, sem simular nada. GDD 17.2.</summary>
        public void Advance(float elapsedSeconds)
        {
            Tick(elapsedSeconds);
        }

        /// <summary>
        /// Os quatro periodos da secao 11.4 reduzidos aos dois do MVP (secao 22.1).
        ///
        /// Por do sol e madrugada ficam como faixas do relogio desde ja: quando o MVP
        /// crescer para quatro periodos, so o mapeamento muda - o relogio ja esta certo.
        /// </summary>
        public TimeOfDay Period
        {
            get { return IsNight(ClockHours) ? TimeOfDay.Night : TimeOfDay.Day; }
        }

        public static bool IsNight(float hours)
        {
            return hours >= 20f || hours < 6f;
        }

        /// <summary>Rotulo de UI: "14:20".</summary>
        public string ClockLabel
        {
            get
            {
                int h = Mathf.FloorToInt(ClockHours);
                int m = Mathf.FloorToInt((ClockHours - h) * 60f);
                return h.ToString("00") + ":" + m.ToString("00");
            }
        }

        /// <summary>Horas de jogo ate o proximo periodo. Alimenta o "chuva chegando" da UI.</summary>
        public float HoursUntilPeriodChange
        {
            get
            {
                float target = IsNight(ClockHours) ? 6f : 20f;
                float delta = target - ClockHours;
                return delta <= 0f ? delta + 24f : delta;
            }
        }
    }

    /// <summary>Os tres estados de execucao da secao 17.1.</summary>
    public enum ExecutionState
    {
        /// <summary>3D completa, 60 fps.</summary>
        Active,

        /// <summary>HUD 2D, 15-30 fps, sem GPU relevante.</summary>
        Compact,

        /// <summary>Nenhuma renderizacao, tick agregado a 1 Hz.</summary>
        Background,
    }

    /// <summary>
    /// Aplica o orcamento de desempenho da secao 20.4 por estado de execucao.
    ///
    /// O orcamento e medido a partir da Fase 5, nao da 15 (risco R8): descobrir na
    /// ultima fase que o modo Taskbar come 15% de CPU seria descobrir tarde demais para
    /// mudar a arquitetura.
    /// </summary>
    public static class ExecutionBudget
    {
        public static void Apply(ExecutionState state)
        {
            switch (state)
            {
                case ExecutionState.Active:
                    Application.targetFrameRate = 60;
                    UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 1;
                    break;

                case ExecutionState.Compact:
                    Application.targetFrameRate = 30;
                    UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 2;
                    break;

                case ExecutionState.Background:
                    // Tick agregado a 1 Hz e render desligado: menos de 1% de CPU.
                    Application.targetFrameRate = 10;
                    UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = 10;
                    break;
            }
        }
    }
}
