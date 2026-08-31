# Framed Drift

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
| `tracks.json` | 3 pistas fixas |
| `regions.json` | Cidade: pool de modulos, clima, pool de pecas |
| `cars.json` | Kite 130, Kanto AE, Brute V8 |
| `parts.json` | 20 bases nos 8 slots |
| `affixes.json` | 12 afixos (9 beneficios + 3 trade-offs) |
| `passives.json` | 8 passivas condicionais (Rare+) |
| `sets.json` | Street King e Drift Demon |
| `rivals.json` | AKIRA |

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

**41 EditMode + 5 PlayMode, todos passando.** Eles nao sao testes de fumaca: sao os
criterios de saida da GDD 21.2 escritos como asserts.

### Harness offline

O assembly `Simulation` compila fora do Unity. Ha um harness em
`scratchpad/SimHarness` que roda 45 verificacoes (incluindo as de desempenho e o
relatorio de balanceamento completo) em segundos, sem abrir o editor.

### O que os testes garantem hoje

| Criterio | GDD | Medido |
|---|---|---|
| Score do carro inicial na pista inicial | 6.3: 6.000-12.000 | **~6.700** |
| Vitorias do carro inicial | 21.2: 55-75% | **~64%** |
| Uplift por presenca | 3.6 / 6.2: 20-30% | **~24,5%** |
| Falha em risco MEDIO | 21.2: 3-6% | **~4,1%** |
| Build de drift x build de velocidade | 21.2: 1,8x-2,6x | **2,48x** |
| Cash/hora offline x online | 21.2: +-8% | **0,06%** |
| Uma corrida simulada | 20.4: < 0,3 ms | **0,006 ms** |
| 10.000 corridas | Fase 3: < 5 s | **0,06 s** |
| Auto-equipar (200 corridas) | 16.3: < 60 ms | **1,2 ms** |
| Pistas geradas validas | Fase 12: 200/200 | **200/200** |

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
| 7-9 | Garagem, tuning, loot | **feito** |
| 10 | Progressao, reputacao, tiers, unlocks | **feito** |
| 11 | Conteudo: regioes, horarios, clima | **parcial** - 1 regiao, 2 periodos, 3 climas |
| 12 | Gerador procedural | **feito** - 200/200 validas |
| 13 | Automacao + frota | **feito** - frota sem execucao paralela ainda |
| 14 | Prestigio, temporadas, Endless | **parcial** - prestigio e arvore de Fama existem; Endless nao |
| 15 | Modo Taskbar | **esboco** (D-07: e Fase 15 de proposito) |
| 16 | Cosmeticos, radio, conquistas, Steam | conquistas existem; o resto nao |

### O que falta, em ordem de importancia

1. **Fase 1 de verdade** - abrir, olhar 60 segundos e calibrar `cameraHeightMultiplier`.
   A GDD e explicita: a altura certa se descobre olhando, nao calculando.
2. **Teste cego da fumaca** (19.3) - com o HUD desligado, um observador precisa
   distinguir Perfect/Good/Bad em 8 de 10 curvas.
3. **Modelos e prefabs de modulo** - trocar as primitivas.
4. **Execucao paralela da frota** - hoje uma vaga corre por vez.
5. **Endless** (14.6) e o resto do conteudo da Fase 11.
