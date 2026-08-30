// -----------------------------------------------------------------------------
//  Framed Drift  -  UI
//  GDD 0.2  secoes 7.3, 18.4, 18.5
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using FramedDrift.App;
using FramedDrift.Core;
using FramedDrift.Garage;
using FramedDrift.Simulation.Content;
using FramedDrift.Simulation.Model;
using UnityEngine;

namespace FramedDrift.UI
{
    /// <summary>
    /// A tela central do jogo (GDD 18.4).
    ///
    /// Precisa responder as TRES PERGUNTAS da secao 3.4 sem navegacao extra:
    ///   1. Qual carro eu uso?      -> seletor no topo
    ///   2. Como eu configuro?      -> 8 slots + ajuste fino, na mesma tela
    ///   3. Onde eu mando correr?   -> aba MAPA, um clique
    ///
    /// As 12 stats aparecem agrupadas em 4 BARRAS-RESUMO (GDD 7.3): o jogador casual le
    /// 4 numeros, o de build le 12 no hover. Nenhuma stat aparece sem a frase que a
    /// explica (GDD 18.5).
    /// </summary>
    public sealed class GarageUI : MonoBehaviour
    {
        private Vector2 _partScroll;
        private PartSlot _selectedSlot = PartSlot.Engine;
        private string _hoverTooltip;

        public void Draw(Rect area)
        {
            GameManager game = GameManager.Instance;
            CarInstance car = game != null ? game.ActiveCar() : null;
            if (car == null) return;

            _hoverTooltip = null;

            GUILayout.BeginArea(area);
            DrawHeader(game, car);
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            DrawStatsColumn(game, car);
            GUILayout.Space(16f);
            DrawSlotsColumn(game, car);
            GUILayout.Space(16f);
            DrawPartsColumn(game, car);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            DrawTooltip();
        }

        // --- cabecalho --------------------------------------------------------------

        private void DrawHeader(GameManager game, CarInstance car)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("GARAGEM - " + car.DisplayName.ToUpperInvariant(), UiSkin.Title, GUILayout.Width(360f));

            if (game.Cars.Count > 1 && GUILayout.Button("TROCAR CARRO", GUILayout.Width(140f)))
            {
                game.Save.ActiveCarIndex = (game.Save.ActiveCarIndex + 1) % game.Cars.Count;
                game.SaveNow();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Dano " + car.Damage.ToString("0") + "/100", UiSkin.Label, GUILayout.Width(110f));

            long repair = game.Crafting.RepairCost(car, game.Save.Progress.TierIndex);
            GUI.enabled = car.Damage > 0.5f && game.Economy.CanAfford(repair);
            if (GUILayout.Button("REPARAR (" + UiSkin.Number(repair) + ")", GUILayout.Width(170f)))
                game.Crafting.Repair(car, game.Save.Progress.TierIndex);
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        // --- as 4 barras-resumo (7.3) --------------------------------------------------

        private void DrawStatsColumn(GameManager game, CarInstance car)
        {
            GUILayout.BeginVertical(GUILayout.Width(380f));

            CarLoadout loadout = car.Loadout;
            ResolvedStats stats = loadout.Stats;
            var balance = game.Content.Balance;

            var groups = new[] { StatGroup.Power, StatGroup.Grip, StatGroup.Drift, StatGroup.Reliability };
            var colors = new[]
            {
                new Color(1f, 0.55f, 0.3f), new Color(0.4f, 0.8f, 1f),
                UiSkin.Accent, UiSkin.Positive,
            };

            for (int i = 0; i < groups.Length; i++)
            {
                float value = StatResolver.Group(in stats, groups[i], balance);
                Rect row = GUILayoutUtility.GetRect(370f, 24f);
                UiSkin.Bar(row, StatResolver.GroupName(groups[i]), value / 100f, value.ToString("0"), colors[i]);
            }

            GUILayout.Space(4f);
            GUILayout.Label("Peso   " + stats.Weight.ToString("0") + " kg", UiSkin.Label);
            GUILayout.Label(StatResolver.DerivedSummary(in stats, balance), UiSkin.Mono);

            GUILayout.Space(10f);
            GUILayout.Label("DETALHE", UiSkin.Label);

            // O jogador de build le as 12. Cada linha carrega a frase da tabela 7.1.
            for (int i = 0; i < StatOps.Count; i++)
            {
                var id = (StatId)i;
                Rect row = GUILayoutUtility.GetRect(370f, 17f);
                GUI.Label(row,
                    StatOps.DisplayName(id).PadRight(16) + StatOps.Get(in stats, id).ToString("0.#"),
                    UiSkin.Mono);

                if (row.Contains(Event.current.mousePosition))
                    _hoverTooltip = StatOps.DisplayName(id) + "\n" + StatResolver.Tooltip(id);
            }

            GUILayout.EndVertical();
        }

        // --- os 8 slots (9.1) -----------------------------------------------------------

        private void DrawSlotsColumn(GameManager game, CarInstance car)
        {
            GUILayout.BeginVertical(GUILayout.Width(330f));
            GUILayout.Label("SLOTS", UiSkin.Title);

            var slots = (PartSlot[])System.Enum.GetValues(typeof(PartSlot));
            for (int i = 0; i < slots.Length; i++)
            {
                PartSlot slot = slots[i];
                PartInstance equipped = car.Equipped(slot);

                GUILayout.BeginHorizontal();

                bool selected = _selectedSlot == slot;
                GUI.color = selected ? UiSkin.Accent : Color.white;
                if (GUILayout.Button(SlotName(slot), GUILayout.Width(120f))) _selectedSlot = slot;
                GUI.color = Color.white;

                if (equipped != null)
                {
                    GUI.color = UiSkin.RarityColor(equipped.Rarity);
                    GUILayout.Label(equipped.DisplayName, UiSkin.Label, GUILayout.Width(160f));
                    GUI.color = Color.white;

                    Rect last = GUILayoutUtility.GetLastRect();
                    if (last.Contains(Event.current.mousePosition)) _hoverTooltip = equipped.Describe();
                }
                else
                {
                    GUILayout.Label("- vazio -", UiSkin.Mono, GUILayout.Width(160f));
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            DrawTuning(game, car);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// O ajuste fino do diferencial - o slot de assinatura (GDD 9.2).
        ///
        /// Cada eixo mostra o que GANHA e o que PERDE, lado a lado. Nenhum slider e
        /// "mais e melhor" (GDD 9.4), e a UI precisa dizer isso sem tooltip.
        /// </summary>
        private void DrawTuning(GameManager game, CarInstance car)
        {
            GUILayout.Label("AJUSTE FINO", UiSkin.Title);
            SavedBuild build = car.ActiveBuild;
            TuningRange range = car.Definition.TuningRange;

            build.TuneLock = Axis("Trava", "Angulo + / Aderencia -", build.TuneLock, range.LockMin, range.LockMax, car);
            build.TuneAccel = Axis("Aceleracao", "Entrada + / Estabilidade -", build.TuneAccel, range.AccelMin, range.AccelMax, car);
            build.TuneDecel = Axis("Desaceleracao", "Transicao + / risco de Bad +", build.TuneDecel, range.DecelMin, range.DecelMax, car);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Estilo", UiSkin.Label, GUILayout.Width(70f));

            var styles = (DriftStyle[])System.Enum.GetValues(typeof(DriftStyle));
            for (int i = 0; i < styles.Length; i++)
            {
                GUI.color = build.Style == styles[i] ? UiSkin.Accent : Color.white;
                if (GUILayout.Button(styles[i].ToString(), GUILayout.Width(58f)))
                {
                    build.Style = styles[i];
                    car.MarkDirty();
                }
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }

        private float Axis(string label, string tradeoff, float value, float min, float max, CarInstance car)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, UiSkin.Label, GUILayout.Width(96f));

            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120f));
            GUILayout.Label((next * 100f).ToString("0") + "%", UiSkin.Mono, GUILayout.Width(44f));
            GUILayout.EndHorizontal();

            GUILayout.Label("    " + tradeoff, UiSkin.Mono);

            if (!Mathf.Approximately(next, value)) car.MarkDirty();
            return next;
        }

        // --- inventario do slot selecionado (18.5) -------------------------------------------

        private void DrawPartsColumn(GameManager game, CarInstance car)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("PECAS - " + SlotName(_selectedSlot).ToUpperInvariant(), UiSkin.Title);
            GUILayout.Label(game.Inventory.Count + "/" + game.Inventory.Capacity + " slots", UiSkin.Mono);

            _partScroll = GUILayout.BeginScrollView(_partScroll);

            List<PartInstance> parts = game.Inventory.InSlot(_selectedSlot);
            int equippedUid = car.ActiveBuild.SlotUids[(int)_selectedSlot];

            for (int i = 0; i < parts.Count; i++)
            {
                PartInstance part = parts[i];
                bool isEquipped = part.Uid == equippedUid;

                GUILayout.BeginHorizontal();

                GUI.color = UiSkin.RarityColor(part.Rarity);
                GUILayout.Label((isEquipped ? "> " : "  ") + part.DisplayName, UiSkin.Label, GUILayout.Width(190f));
                GUI.color = Color.white;

                Rect row = GUILayoutUtility.GetLastRect();
                if (row.Contains(Event.current.mousePosition)) _hoverTooltip = BuildComparison(game, car, part);

                // Equipar nao pede confirmacao: e reversivel (GDD 18.5).
                GUI.enabled = !isEquipped;
                if (GUILayout.Button("EQUIPAR", GUILayout.Width(80f)))
                {
                    car.Equip(part);
                    game.SaveNow();
                }
                GUI.enabled = true;

                // Desmontar Legendary PEDE confirmacao - e a excecao da mesma regra.
                bool needsConfirm = part.Rarity >= Rarity.Legendary;
                GUI.enabled = !isEquipped && !part.Locked;
                string salvageLabel = needsConfirm ? "DESMONTAR!" : "DESMONTAR";
                if (GUILayout.Button(salvageLabel, GUILayout.Width(100f)))
                {
                    long scrap = game.Inventory.Salvage(part.Uid);
                    game.Economy.AddScrap(0);   // republica o extrato
                    game.SaveNow();
                    _hoverTooltip = "+" + scrap + " de scrap";
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }

            if (parts.Count == 0) GUILayout.Label("Nenhuma peca deste tipo.", UiSkin.Mono);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// A comparacao SEMPRE VISIVEL da secao 18.5: delta em stats E em Drift Score
        /// estimado, contra a peca equipada.
        ///
        /// O score estimado sai do MESMO simulador que roda a corrida (GDD 16.3), entao
        /// o tooltip nunca promete algo que a pista nao entrega.
        /// </summary>
        private string BuildComparison(GameManager game, CarInstance car, PartInstance candidate)
        {
            var conditions = new RaceConditions
            {
                Weather = game.Forecast,
                TimeOfDay = game.Clock.Period,
                Traffic = TrafficDensity.Medium,
            };

            BuildDelta delta = game.Builds.Compare(car, candidate, game.ActiveTrack(),
                                                   conditions, game.Save.Progress.Stage);

            var sb = new System.Text.StringBuilder();
            sb.Append(candidate.Describe());
            sb.Append("\n\nCONTRA A EQUIPADA");

            for (int i = 0; i < StatOps.Count; i++)
            {
                string line = StatResolver.DeltaLine((StatId)i, delta.Stat((StatId)i));
                if (line.Length > 0) sb.Append('\n').Append("  ").Append(line);
            }

            double scoreDelta = delta.ScoreDelta;
            sb.Append("\n  Drift Score estimado ")
              .Append(scoreDelta >= 0 ? "+" : "")
              .Append(UiSkin.Number(scoreDelta));

            return sb.ToString();
        }

        private void DrawTooltip()
        {
            if (string.IsNullOrEmpty(_hoverTooltip)) return;

            Vector2 mouse = Event.current.mousePosition;
            var content = new GUIContent(_hoverTooltip);
            Vector2 size = UiSkin.Label.CalcSize(content);
            size.x = Mathf.Min(size.x + 24f, 420f);
            size.y = UiSkin.Label.CalcHeight(content, size.x) + 16f;

            var rect = new Rect(
                Mathf.Min(mouse.x + 16f, Screen.width - size.x - 8f),
                Mathf.Min(mouse.y + 16f, Screen.height - size.y - 8f),
                size.x, size.y);

            UiSkin.Fill(rect, UiSkin.Panel);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f),
                      _hoverTooltip, UiSkin.Label);
        }

        public static string SlotName(PartSlot slot)
        {
            switch (slot)
            {
                case PartSlot.Engine: return "Motor";
                case PartSlot.Turbo: return "Turbo";
                case PartSlot.Transmission: return "Transmissao";
                case PartSlot.Differential: return "Diferencial";
                case PartSlot.Suspension: return "Suspensao";
                case PartSlot.Tires: return "Pneus";
                case PartSlot.Brakes: return "Freios";
                default: return "Aero/Peso";
            }
        }
    }
}
