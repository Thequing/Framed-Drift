# Framed Drift

An idle drift RPG for PC, solo, in Unity 6. The player never drives. They **prepare** —
pick the car, build the tune, choose the style, the track, the hour, the weather and how
much risk to take — and then watch the car run, or don't watch at all.

Underneath is a race simulator that resolves the whole race at once, deterministically,
and **does not know Unity exists**.

> Detailed documentation below this section is in Portuguese. This part is the summary
> in English. Full spec: [`Docs/GDD.md`](Docs/GDD.md).

---

## Why the architecture is the point

The `Simulation` assembly sets `noEngineReferences: true` in its asmdef. That turns a
design rule — *"`UnityEngine.Random` is forbidden in the Simulation namespace"* — from
discipline into a **compile error**. What that buys:

- balance tests run under `dotnet`, **without opening the editor**, in under a second
- **10,000 races resolve in 0.06 s**; a single race in 0.006 ms
- no result depends on framerate, platform or `Update` order

A race runs in two phases, and the split is what makes offline progress honest:

```
1. RESOLVE    RaceSimulator decides the ENTIRE race at once.
              Returns a RaceResult with the full Timeline — and NO DriftScore.

2. REPLAY     The visualiser plays the timeline back. Perfect Entry edits only the
              QUALITY of corners that are already resolved. At the end, DriftScorer
              reduces the timeline to a number.

   Offline skips phase 2 and calls the SAME scorer on the untouched timeline.
```

`RaceResult` deliberately has **no** `DriftScore` field. The score comes out of
`DriftScorer.Score(timeline)` *after* replay, because until then the input could still
edit the timeline. Offline calls that identical function on an unedited timeline — so
"offline earns what online earns" is true by construction, not because a test says so.
Measured drift between the two: **0.06 %**.

## Measured, not claimed

Every row below is an exit criterion from the GDD written as an assert. **135 tests**
(127 EditMode + 8 PlayMode), all passing.

| Criterion | Target | Measured |
|---|---|---|
| Starting car win rate | 55–75 % | **68.3 %** |
| Win rate entering C / B / A / S | 55–75 % | **68.8 / 63.7 / 63.2 / 64.3 %** |
| Cash per race, D → S | must climb | **617 → 1,642 → 4,467 → 11,459 → 32,203** |
| Offline vs. online cash/hour | ±8 % | **0.06 %** |
| Presence uplift | 20–30 % | **24.5 %** |
| Failure rate at MEDIUM risk | 3–6 % | **4.1 %** |
| Drift build vs. speed build | 1.8×–2.6× | **2.48×** |
| One simulated race | < 0.3 ms | **0.006 ms** |
| 10,000 races | < 5 s | **0.06 s** |
| Generated tracks valid | 200 / 200 | **200 / 200** |

Beyond those, the suite covers byte-for-byte determinism per seed, isolation between RNG
streams, the guarantee that a promotion **never** touches physics or combo, that Bad never
silently becomes Good, an honest offline ceiling, a clock set backwards without punishing
the player, and save round-trips with backup recovery.

## The rivals are the same code as the player

A rival is a real car with a real build, resolved by the **same** `RaceSimulator`, on its
own RNG stream so that adding one doesn't shift any of the player's curves. It takes a
grid slot instead of adding one.

Defeat is measured in Drift Score, never in position — which is what makes AKIRA's lesson
teachable: he crosses the line first in **99.7 %** of races and still loses the scoreboard
in half of them. Each rival's build is in `rivals.json` part by part, because the design
requires it to be readable and counterable.

## Content is commented JSON

11 tracks · 57 parts · 12 affixes · 8 conditional passives · 5 rivals — all in
`Assets/_Project/Resources/Content/`, editable without recompiling. Enums are written by
**name**, never by index, so reordering can't silently turn one value into another.

## Running it

1. Open in Unity 6000.3.10f1
2. Open `Assets/_Project/Scenes/FramedDrift.unity`
3. Play

The scene holds **one GameObject** with a `Bootstrap` component that builds camera, track,
car, HUD and systems in code. Editor menu: balancing tool (`Ctrl+Shift+B`), content
validator, track-generator test, scene rebuild.

**All art is placeholder and that is deliberate.** The car is a cube with a yellow stripe;
the track is a run of flattened cubes. The stripe exists to keep one non-negotiable
requirement visible: yaw has to read unambiguously from the raised chase camera. Models
drop in later without touching the architecture.

---

# Documentação completa (português)

RPG incremental de drift para PC. O jogador nao pilota: ele **prepara** - escolhe o
carro, monta a build de tuning, define o estilo, a pista, o horario, o clima e quanto
risco assumir - e entao assiste (ou nao) o carro executar.

Especificacao completa em [`Docs/GDD.md`](Docs/GDD.md) (versao 0.2). Este README cobre o
que **existe em codigo** e como rodar.

- **Engine:** Unity 6000.3.10f1 - URP 17.3 - Input System 1.18
- **Plataforma:** PC (Windows), Steam

---

## Como rodar

1. Abra o projeto no Unity 6000.3.10f1.
2. Abra `Assets/_Project/Scenes/FramedDrift.unity`.
3. Play.

A cena tem **um GameObject** com o componente `Bootstrap`, que monta camera, pista,
carro, HUD e sistemas em codigo. Se ela sumir, recrie com **Framed Drift > Recriar cena
jogavel**.

### Menu do editor

| Item | O que faz |
|---|---|
| **Framed Drift > Ferramenta de Balanceamento** (`Ctrl+Shift+B`) | Roda N corridas e reporta as distribuicoes (GDD 21.1). Mostra os criterios da GDD verificados na hora. |
| **Framed Drift > Validar conteudo** | Carrega e valida os JSON de `Resources/Content`. |
| **Framed Drift > Testar gerador de pistas** | Gera 200 pistas e checa adjacencia (criterio da Fase 12). |
| **Framed Drift > Recriar cena jogavel** | Regenera a cena. |
| **Framed Drift > Save > Apagar save** | Apaga o save e os 3 backups. |

### Arte: tudo e placeholder

O carro e um **cubo** com uma faixa amarela atravessada; a pista e uma sequencia de
**cubos achatados**; paredes e coletaveis tambem sao cubos. Isso e deliberado - os
modelos entram depois sem mexer na arquitetura.

Duas restricoes ja valem para quem for modelar (GDD 19.1), e nao sao negociaveis:

1. **Legivel a 360x48 px** (requisito do modo Taskbar).
2. **Yaw legivel da camera traseira elevada.** Um carro a 40 graus precisa ser
   inequivocamente diferente de um carro reto, mesmo com a perspectiva encurtando a
   rotacao. Na pratica: teto e capo assimetricos, faixa de cor atravessando o eixo
   longitudinal, rodas visiveis. *Um carro cujo topo e uma mancha uniforme falha o teste.*

A faixa amarela no cubo existe so para nao deixar esse requisito ser esquecido.

---

## Arquitetura

Onze assemblies, em ordem de dependencia. A seta indica "depende de".

```
Simulation  (C# PURO, sem UnityEngine)
   |
   +-> Data ------> Core ------> Garage ---> Progression ---> Automation
                                                                  |
                                                                  v
                                                                 App
                                                                  |
                                                    +-------------+-------------+
                                                    v                           v
                                                 Racing  ------------------->  UI
                                                                                |
                                                                                v
                                                                           Bootstrap
```

### `Simulation` - o coracao, e ele nao ve o Unity

O asmdef tem `noEngineReferences: true`. Isso transforma a regra da GDD 20.3
("`UnityEngine.Random` e proibido no namespace Simulation") em **erro de compilacao** em
vez de disciplina. Consequencias que valem o incomodo:

- os testes de balanceamento rodam em `dotnet`, **sem abrir o editor**, em menos de um
  segundo;
- 10.000 corridas resolvem em **0,06 s**;
- nenhum resultado depende de framerate, plataforma ou ordem de `Update`.

### As duas fases de uma corrida

```
1. RESOLVER    RaceSimulator decide a corrida INTEIRA, de uma vez.
               Devolve RaceResult com a Timeline completa - e SEM DriftScore.

2. REPRODUZIR  O visualizador toca a timeline. A Entrada Perfeita edita apenas a
               QUALIDADE de curvas ja resolvidas. No fim, DriftScorer reduz a
               timeline a um numero.

   Offline pula a fase 2 e chama o MESMO scorer com a timeline intocada.
```

`RaceResult` **nao tem** campo `DriftScore`, e isso e proposital (GDD 20.6). O score sai
de `DriftScorer.Score(timeline)` depois da reproducao, porque a timeline ainda podia ser
editada pelo input. Chamar o mesmo scorer com a timeline intocada devolve o resultado
offline - **e a mesma funcao**. E isso, e nao um teste, que faz a promessa de D-02
("offline rende 100%") ser literalmente verdadeira.

### Quatro licencas em relacao a arvore da GDD 20.1

1. **`DeterministicRng` mora em `Simulation/Rng/`, nao em `Core/`.** A GDD lista `Rng`
   sob `Core`, mas `Core` referencia `UnityEngine` e o simulador nao pode depender dele.
2. **`GameManager` mora em `App/`, nao em `Core/`.** `Core` e a base (EventBus, SaveData,
   SaveManager, TimeManager) e Garage, Progression e Automation dependem dela. A
   composicao raiz precisa das tres - deixa-la em `Core` seria um ciclo.
3. **`TuningUI` e `InventoryUI` nao sao arquivos separados**: as duas telas vivem dentro
   de `GarageUI`, porque a GDD 18.4 exige que a garagem responda as tres perguntas da 3.4
   *sem navegacao extra*.
4. **`EconomyLedger` mora em `Core/`, nao em `Progression/`.** `Garage` e `Progression`
   sao irmaos: nenhum ve o outro. Com o ledger em `Progression`, quem mais gasta
   (`Crafting`, `InventoryManager`) nao conseguia alcanca-lo e debitava `SaveData` direto
   - o `CurrencyChanged` nunca saia. O ledger nao depende de nenhum dos dois, so de
   `SaveData` + `BalanceSettings` + `EventBus`, entao `Core` e onde ele sempre coube.

Cada uma esta documentada tambem no README da pasta correspondente.

---

## Conteudo

Todo o conteudo e **JSON comentado** em `Assets/_Project/Resources/Content/`, editavel
sem recompilar (GDD 20.2). Ver [`Assets/_Project/Scripts/Data/README.md`](Assets/_Project/Scripts/Data/README.md)
para o porque de JSON em vez de ScriptableObject.

| Arquivo | Conteudo |
|---|---|
| `balance.json` | as ~100 constantes das formulas |
| `tiers.json` | D..S: dificuldade, recompensa, pesos de raridade |
| `modules.json` | 6 modulos autorais + regras de adjacencia |
| `tracks.json` | 11 pistas: 3 D + 2 C + 2 B + 2 A + 2 S |
| `regions.json` | Cidade: pool de modulos, clima, pool de pecas |
| `cars.json` | Kite 130, Kanto AE, Brute V8 |
| `parts.json` | 57 bases: 52 de catalogo (20 D + 8 C + 8 B + 8 A + 8 S) + 5 assinaturas de rival |
| `affixes.json` | 12 afixos (9 beneficios + 3 trade-offs) |
| `passives.json` | 8 passivas condicionais (Rare+) |
| `sets.json` | Street King e Drift Demon |
| `rivals.json` | 5 rivais, um por tier: AKIRA (D), KEN (C), MIKA (B), RYU (A), ZERO (S) |

Enums sao escritos por **nome**, nunca por indice, para que o conteudo sobreviva a uma
reordenacao sem virar outro valor silenciosamente. O leitor aceita comentarios `//`.

---

## Testes

### Unity

```
Window > General > Test Runner        (ou em batch:)

Unity.exe -batchmode -runTests -projectPath . -testPlatform EditMode
Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode
```

**127 EditMode + 8 PlayMode, todos passando.** Eles nao sao testes de fumaca: sao os
criterios de saida da GDD 21.2 escritos como asserts.

### Harness offline

O assembly `Simulation` compila fora do Unity. Ha um harness em
`scratchpad/SimHarness` que roda 45 verificacoes (incluindo as de desempenho e o
relatorio de balanceamento completo) em segundos, sem abrir o editor.

### O que os testes garantem hoje

| Criterio | GDD | Medido |
|---|---|---|
| Score do carro inicial na pista inicial | 6.3: 6.000-12.000 | **~6.700** |
| Vitorias do carro inicial | 21.2: 55-75% | **68,3%** |
| Vitorias na entrada de C / B / A / S | 21.2 estendido: 55-75% | **68,8 / 63,7 / 63,2 / 64,3%** |
| Cash/corrida D -> C -> B -> A -> S | 15.4: tem de subir | **617 -> 1.642 -> 4.467 -> 11.459 -> 32.203** |
| Primeira planta completa | 10.6: objetivo de longo prazo | **121 corridas (~2,5 h ativas)** |
| Pedacos de planta offline x online | D-02: +-8% | **dentro da faixa** |
| Uplift por presenca | 3.6 / 6.2: 20-30% | **~24,5%** |
| Falha em risco MEDIO | 21.2: 3-6% | **~4,1%** |
| Build de drift x build de velocidade | 21.2: 1,8x-2,6x | **2,48x** |
| Cash/hora offline x online | 21.2: +-8% | **0,06%** |
| Uma corrida simulada | 20.4: < 0,3 ms | **0,006 ms** |
| 10.000 corridas | Fase 3: < 5 s | **0,06 s** |
| Auto-equipar (200 corridas) | 16.3: < 60 ms | **1,2 ms** |
| Pistas geradas validas | Fase 12: 200/200 | **200/200** |
| AKIRA chega na frente | 3.5: a licao dele | **99,7% das corridas** |
| ...e mesmo assim perde no score | 13.1: derrota e por score | **50,2%** |
| RYU perde a corrida e ganha no placar | 13.1: o espelho do AKIRA | **12,0% na frente / 60,7%** |
| ZERO se destroi sozinho | 13.1: glass cannon | **52,3% das corridas dele** |
| Derrota de rival offline x online | D-02 / 21.2: +-8% | **dentro da faixa** |

Alem desses, os testes cobrem: determinismo byte a byte por seed, isolamento dos fluxos
de RNG, a garantia de que uma promocao **nunca** toca em fisica ou combo, que Bad nunca
vira Good, teto offline honesto, relogio para tras sem punicao, e round-trip de save com
recuperacao de backup.

---

## Estado por fase (GDD 22.2)

| Fase | Entrega | Estado |
|---|---|---|
| 0 | GDD, constantes de balanceamento | **feito** - ~100 constantes preenchidas e calibradas |
| 1 | Prototipo de direcao + camera + drift visual | **parcial** - camera elevada e fumaca existem; falta calibrar olhando |
| 2 | Score, combo, qualidade, HUD, fumaca | **feito**; falta o teste cego de legibilidade da 19.3 |
| 3 | `RaceSimulator` + janela de balanceamento | **feito** |
| 4 | Visualizador consumindo `RaceResult` | **feito** (geometria placeholder) |
| 4.5 | Entrada Perfeita + coletaveis | **feito** - uplift medido em 24,5% |
| 5 | Auto-race em loop | **feito**; falta a prova de 2 h sem vazamento |
| 6 | Save + progresso offline | **feito** - paridade em 0,06% |
| 7-9 | Garagem, tuning, loot, loja | **feito** - vitrine de catalogo fixo por tier |
| 10 | Progressao, reputacao, tiers, unlocks | **feito** |
| 11 | Conteudo: regioes, horarios, clima | **parcial** - 1 regiao, 2 periodos, 3 climas; os 5 tiers de peca e de pista |
| 12 | Gerador procedural | **feito** - 200/200 validas |
| 13 | Automacao + frota | **feito** - frota sem execucao paralela ainda |
| 13.1 | Rivais: encontro, duelo, derrota, planta de carro | **feito** - 5 rivais, um por tier, calibrados por medicao |
| 14 | Prestigio, temporadas, Endless | **parcial** - prestigio e arvore de Fama existem; Endless nao |
| 15 | Modo Taskbar | **esboco** (D-07: e Fase 15 de proposito) |
| 16 | Cosmeticos, radio, conquistas, Steam | conquistas existem; o resto nao |

### Rivais (GDD 13.1)

Um rival e um carro REAL com build real, resolvido pelo **mesmo** `RaceSimulator` - com
fluxo de RNG proprio, para que incluir um rival nao desloque nenhuma curva do jogador.
Ele **ocupa** uma vaga do grid da 5.6 em vez de acrescentar uma.

**Derrota se mede em Drift Score, nunca em posicao** (D-03). E o que torna a licao do
AKIRA ensinavel: ele cruza a linha na frente em 99,7% das corridas e ainda assim perde o
placar em metade delas.

A build de cada um esta em `rivals.json`, peca por peca, porque a 13.1 exige que ela seja
legivel e contra-atacavel - o jogador precisa poder olhar o que ele monta e responder.
`statBonus` e so o piloto. Quando a build era de serie, um rival tier S corria com motor
de fabrica e era derrotado em 100% das corridas.

A assinatura de cada rival saiu do pool de loot da regiao: ela dropa quando o dono perde,
e o validador recusa o conteudo se alguem devolve-la ao pool. Tres derrotas do mesmo rival
entregam o carro dele como planta, que **dispensa a reputacao** que o carro exigia.

Tudo isso acontece igual na ausencia (D-06), pela mesma amostragem da 17.2 e pelo mesmo
`RivalSystem` - a taxa de derrota offline bate com a online dentro dos +-8%.

### O que falta, em ordem de importancia

1. **Fase 1 de verdade** - abrir, olhar 60 segundos e calibrar `cameraHeightMultiplier`.
   A GDD e explicita: a altura certa se descobre olhando, nao calculando.
2. **Teste cego da fumaca** (19.3) - com o HUD desligado, um observador precisa
   distinguir Perfect/Good/Bad em 8 de 10 curvas.
3. **Modelos e prefabs de modulo** - trocar as primitivas.
4. **Execucao paralela da frota** - hoje uma vaga corre por vez.
5. **Endless** (14.6) e o resto do conteudo da Fase 11.
6. **Segunda regiao.** A escada de tier esta fechada e medida de D a S, mas toda ela
   acontece em `city`. O mapa e um grafo desde o inicio (`neighbors` ja lista
   `industrial` e `coast`) e nada preenche esses nos.
7. **Eventos de corrida** (12.2) e **eventos raros** (13.2). O painel de automacao tem a
   regra "aceitar eventos com risco <= MEDIO" desde a Fase 13 e nada gera evento nenhum -
   nem os positivos (reta livre, publico, vacuo), nem os negativos (oleo, poca, policia).
   A regra de rival, que estava no mesmo estado, passou a ser lida agora.
8. **Audio.** Nao existe um `AudioSource` no projeto. A 19.5 pede trilha por regiao x
   periodo, radio com estacoes e - o que importa para o jogador ausente - *stingers* de
   peca rara, rival e recorde como notificacao periferica.
9. **Prestigio e conquistas nao tem tela.** Os dois sistemas existem e sao instanciados
   no `GameManager`; nada na UI chama nenhum dos dois, entao Reputacao 100 nao leva a
   lugar nenhum.

### Um achado de balanceamento, registrado aqui para nao se perder

Calibrar os rivais expos uma consequencia do teto de 0-100 das stats (`StatOps`): no tier
A e acima, jogador e rival chegam **os dois** em 100 de Angle, Initiation, DriftControl e
Transition, e so Power, Weight e Aero continuam vivos. O efeito colateral e que
`tir_drift_comp` vira estritamente pior que `tir_racing` la em cima - o bonus de angulo
nao rende nada porque ja saturou, e o agravo de agarre roda inteiro. Isso contradiz a
9.3 ("perfil e escolha, nao nivel") e nao e uma questao de rival: vale para a build do
jogador tambem. Ficou fora do escopo desta entrega.
