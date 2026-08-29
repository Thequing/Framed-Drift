// -----------------------------------------------------------------------------
//  Framed Drift  -  Data
//  GDD 0.2  secao 20.2
// -----------------------------------------------------------------------------

using FramedDrift.Simulation.Balance;
using UnityEngine;

namespace FramedDrift.Data
{
    /// <summary>
    /// O asset unico com as ~60 constantes de balanceamento (GDD 20.2).
    /// Precisa ser editavel e recarregavel SEM RECOMPILAR - por isso e um asset,
    /// e por isso nenhum numero de balanceamento pode aparecer em codigo de gameplay.
    ///
    /// TODO(Fase 0): criar o asset em Assets/_Project/Settings/BalanceConfig.asset
    /// e preencher a partir da planilha de economia.
    /// </summary>
    [CreateAssetMenu(menuName = "Framed Drift/Balance Config", fileName = "BalanceConfig")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [SerializeField] private BalanceSettings _settings = new BalanceSettings();

        /// <summary>O POCO puro que o assembly de Simulation consome.</summary>
        public BalanceSettings Settings { get { return _settings; } }
    }
}
