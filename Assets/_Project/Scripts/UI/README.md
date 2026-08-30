# FramedDrift.UI

## Por que IMGUI

Toda a UI do MVP e `OnGUI`. E provisorio e deliberado: IMGUI nao precisa de nenhum asset
autorado, entao o jogo **roda antes de existir arte** - condicao para as fases 1 a 6
poderem ser jogadas e medidas em vez de imaginadas.

D-07 e o argumento a favor. O modo Taskbar e a assinatura do produto, mas e uma feature
de *apresentacao*, e entra na Fase 15 depois de o jogo ser divertido numa janela normal.
Construir a UI definitiva agora travaria decisoes antes de saber quais informacoes
importam. Esta camada existe justamente para descobrir isso.

Os quatro principios da secao 18.5 ja valem, porque sao de design e nao de tecnologia:

- comparacao sempre visivel (delta de stats **e** de Drift Score estimado);
- nunca mostrar uma stat sem a frase que a explica;
- sem confirmacao para acoes reversiveis (equipar nao pede; desmontar Legendary pede);
- **cor codifica raridade e nada mais** - qualidade de rolagem e icone.

## Arquivos

```
UiSkin                paleta, barras, cores de raridade e de risco
GameHud               moldura do Modo Completo: barra superior, abas, toasts
RaceUI                HUD da corrida + pop-ups da secao 6.4
GarageUI              a tela central: 4 barras-resumo, 8 slots, ajuste fino, inventario
MapUI                 pistas com risco, recompensa e previsao do tempo
AutomationUI          painel de regras, escada de desbloqueio e frota
ReturnUI              tela de retorno da ausencia (17.4), modal
TaskbarUI             modo Compacto + esboco do Taskbar (Fase 15)
WindowModeController  troca de modo e orcamento de CPU por estado
WindowMode            o enum dos tres modos
```

`TuningUI` e `InventoryUI` da secao 20.1 **nao sao arquivos separados**: as duas telas
vivem dentro de `GarageUI`, porque a secao 18.4 exige que a garagem responda as tres
perguntas da 3.4 *sem navegacao extra*. Separa-las em telas proprias criaria exatamente a
navegacao que aquela secao proibe. Quando o ajuste fino crescer para alem de tres eixos,
vale reabrir a decisao.
