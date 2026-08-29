# FRAMED DRIFT — Game Design Document

> **Versão:** 0.2
> **Data:** 29/08/2026
> **Mudanças desde 0.1:** direção visual fechada em 2.5D (§19) · interação presente adicionada (§3.6) · D-09 registrada · simulador passa a emitir resultado por curva (§5.5, §20.6)
> **Engine:** Unity 6000.3.10f1 · URP 17.3.0 · Input System 1.18
> **Plataforma alvo:** PC (Windows) — Steam
> **Equipe:** solo dev
> **Status:** pré-produção — nenhum código de gameplay escrito ainda

---

## Sumário

1. [Visão](#1-visão)
2. [Contexto de uso e sessões](#2-contexto-de-uso-e-sessões)
3. [Core loop](#3-core-loop)
    - 3.6 [Interação presente](#36-interação-presente)
4. [Decisões de design registradas](#4-decisões-de-design-registradas)
5. [Modelo de simulação de corrida](#5-modelo-de-simulação-de-corrida)
6. [Drift Score](#6-drift-score)
7. [Estatísticas](#7-estatísticas)
8. [Carros](#8-carros)
9. [Tuning](#9-tuning)
10. [Peças e loot](#10-peças-e-loot)
11. [Pistas, regiões e gerador](#11-pistas-regiões-e-gerador)
12. [Condições, risco e falha](#12-condições-risco-e-falha)
13. [Rivais e eventos](#13-rivais-e-eventos)
14. [Progressão](#14-progressão)
15. [Economia](#15-economia)
16. [Automação](#16-automação)
17. [Progresso offline](#17-progresso-offline)
18. [Interface e modos de janela](#18-interface-e-modos-de-janela)
19. [Direção de arte e áudio](#19-direção-de-arte-e-áudio)
20. [Arquitetura técnica](#20-arquitetura-técnica)
21. [Balanceamento e telemetria](#21-balanceamento-e-telemetria)
22. [Escopo: MVP e roadmap](#22-escopo-mvp-e-roadmap)
23. [Riscos](#23-riscos)
24. [Apêndices](#24-apêndices)

---

## 1. Visão

### 1.1 High concept

**Framed Drift é um RPG incremental onde os personagens são carros e as batalhas são corridas de drift.**

O jogador não pilota. Ele **prepara**: escolhe o carro, monta a build de tuning, define o estilo de pilotagem, escolhe a pista, o horário, o clima e quanto risco quer assumir — e então assiste (ou não) o carro executar. A corrida é o combate automático; o tuning é o equipamento; a pista é a dungeon; o rival é o boss.

O jogo cabe numa janela pequena e continua rodando enquanto o jogador faz outra coisa.

### 1.2 Nome

*Framed* em dois sentidos simultâneos: o jogo vive **enquadrado** numa janela mínima no canto da tela, e o universo é o de corrida de rua, onde todo mundo já foi **incriminado** por alguma coisa. O logotipo deve carregar a moldura como elemento gráfico.

### 1.3 Pilares

**P1 — A build é o gameplay.**
Toda decisão significativa acontece antes da largada. Se uma decisão não muda o resultado da corrida de forma legível, ela não deve existir.

**P2 — Leitura em três segundos.**
O jogador precisa entender o estado do jogo num olhar de relance, com a janela ocupando 300×60 pixels. Nada essencial pode depender de atenção sustentada.

**P3 — Continuidade honesta.**
O que o carro faz ausente é o que ele faria presente. Sem penalidade oculta, sem simulador paralelo, sem "recompensa offline reduzida".

**P4 — Especialização acima de escalada.**
Ficar mais forte é o eixo secundário. O eixo primário é ficar **mais específico**: um carro de chuva, um carro de montanha, um carro de farm, um carro suicida de score.

**P5 — Espetáculo automático.**
A corrida precisa ser bonita de assistir sem input. Fumaça, ângulo, combo subindo, números explodindo na tela.

### 1.4 Anti-pilares

- **Não é um simulador.** Nenhum realismo é buscado por si só; o vocabulário automotivo existe porque é legível e evocativo, não porque queremos precisão.
- **Não é um clicker.** Nenhuma mecânica premia clicar mais rápido ou mais vezes.
- **Não tem parede de números vazios.** Se o jogador não consegue verbalizar o que uma stat faz, a stat está mal desenhada.
- **Não tem 20 moedas.** Cinco recursos, no máximo, no jogo inteiro.
- **Não tem monetização predatória.** Preço fixo. Sem energia, sem timers pagos, sem gacha.

### 1.5 Pitch de uma frase

> Um arcade idle de drift para PC: você constrói carros, monta builds de tuning e os manda correr sozinhos por regiões, horários e climas diferentes — em tela cheia, numa janela de barra de tarefas, ou com o jogo fechado.

---

## 2. Contexto de uso e sessões

O jogo precisa funcionar bem em **três durações de sessão radicalmente diferentes**. Cada tela deve ser projetada respondendo a qual dessas sessões ela serve.

| Sessão | Duração | O que o jogador faz | Onde acontece |
|---|---|---|---|
| **Relance** | 3–20 s | Olha o score subindo, vê se caiu peça, aceita ou ignora um evento | Modo Taskbar / Compacto |
| **Manutenção** | 2–10 min | Equipa peças, faz um upgrade, troca de pista, ajusta regras de automação | Modo Completo |
| **Sessão** | 30 min–3 h | Reconstrói builds, caça uma peça específica, enfrenta rival, avança região, prestígio | Modo Completo |

**Regra de ouro:** nenhuma progressão relevante pode exigir a sessão longa. A sessão longa deve ser *desejável*, nunca *obrigatória*.

---

## 3. Core loop

### 3.1 Loop imediato (30–90 s)

```
            ┌──────────────────┐
            │     CORRIDA      │  ← executa sozinha
            └────────┬─────────┘
                     ↓
            ┌──────────────────┐
            │  DRIFT + POSIÇÃO │  ← duas moedas de sucesso
            └────────┬─────────┘
                     ↓
            ┌──────────────────┐
            │ CASH · PEÇA · XP │
            └────────┬─────────┘
                     ↓
            ┌──────────────────┐
            │  PRÓXIMA CORRIDA │  ← automática
            └──────────────────┘
```

### 3.2 Loop de decisão (2–10 min)

```
GARAGEM → equipar peça → alterar tuning → escolher pista
   ↑                                            ↓
   └──── loot ← corrida ← risco ← clima ← horário
```

### 3.3 Loop macro (horas / dias)

```
CARRO → BUILD → REGIÃO → TIER → NOVO CARRO → REPUTAÇÃO → PRESTÍGIO → TEMPORADA → ∞
```

### 3.4 As três perguntas

Todo conteúdo adicionado ao jogo precisa tornar pelo menos uma destas perguntas mais interessante:

1. **Qual carro eu uso?**
2. **Como eu configuro esse carro?**
3. **Onde eu mando esse carro correr?**

Conteúdo que não afeta nenhuma das três é cosmético — e cosmético é bem-vindo, mas entra no orçamento de arte, não no de design.

### 3.5 A tensão central: posição × score

Esta é a mecânica que sustenta o buildcraft inteiro.

- **Vencer a corrida** paga *cash base* e *reputação*.
- **Fazer drift** paga *cash por score*, *multiplicadores de drop* e *fama*.

E as duas coisas **competem**: derrapar numa curva custa velocidade de saída (ver §5.4). Um carro em drift atravessa a curva ~15–25% mais devagar que um carro em grip.

Consequência: existem builds legítimas que **terminam em quarto lugar e ganham mais** do que a build que venceu. Isso é intencional e deve ser comunicado explicitamente na UI de resultado, mostrando as duas fontes de recompensa lado a lado.

### 3.6 Interação presente

D-06 garante que **nada exige** o jogador. Isso é um piso, não um teto. Sem nada para fazer quando a janela está aberta, não existe motivo para abri-la — e todo o orçamento de arte da §19 é gasto num modo que ninguém olha.

A referência é a *golden cookie* do Cookie Clicker e a engrenagem dourada do Rusty's Retirement: **efêmero, opcional, alto valor, baixo esforço, e ausente da matemática offline.**

**Regra que governa esta seção:**

> Presença vale um **bônus**, nunca um **requisito**, e nunca uma **taxa**.

- *Requisito* mataria D-06 e o pilar P3.
- *Taxa* (clicar mais rápido rende mais) transformaria o jogo num clicker e faria do modo Taskbar uma punição.

**Teto explícito:** o ganho total por presença é limitado a **20–30%** do Drift Score da corrida. Abaixo de ~10% ninguém se importa; acima de ~40% o jogo deixa de ser idle. Este teto é constante do `BalanceConfig` e é verificado por teste (§21).

#### 3.6.1 Entrada Perfeita (mecânica principal)

Na entrada de cada curva com drift, abre-se uma janela de input curta ancorada no carro. Acertar promove **aquela curva em um nível de qualidade**: `Good 1.00 → Perfect 1.60` (§5.5).

```
janela      = 0.45 s          // constante do BalanceConfig
promoção    = Good → Perfect  // nunca Bad → Good: erro do simulador continua erro
ausente     = a rolagem do simulador vale, sem alteração
```

Uma corrida tem 18–30 segmentos, dos quais **6–10 são curvas com drift**. O input é portanto *temporização*, limitada e legível — nunca cadência de cliques.

**Restrição arquitetural — a que torna isto seguro:** a Entrada Perfeita altera **apenas o score, jamais a trajetória**. Se o input mudasse `v_drift`, todos os segmentos seguintes precisariam ser re-simulados e a linha do tempo pré-computada de D-01 desmoronaria. Ver §5.5 e §20.6.

#### 3.6.2 Coletáveis raros

Durante a corrida visível, um drone de patrocinador ou um brilho de *hot lap* aparece sobre um segmento por poucos segundos. Clicar concede um burst:

| Coletável | Efeito | Frequência alvo |
|---|---|---|
| Drone de patrocinador | cash imediato | ~1 a cada 4 corridas |
| Brilho de hot lap | +25% de chance de drop nesta corrida | ~1 a cada 6 corridas |
| Selo de reputação | reputação plana | ~1 a cada 10 corridas |

**Coletáveis não existem na agregação offline (§17).** Não são "perdidos" na ausência — eles simplesmente não são gerados. É isso que mantém a promessa de D-02 ("offline rende 100%") literalmente verdadeira, e não apenas tecnicamente verdadeira.

#### 3.6.3 Cortado do MVP

**Ajuste de linha ao vivo** (mexer no slider SAFE ↔ RECKLESS durante a corrida) fica fora. Sobrepõe-se ao slider de risco do painel de automação (§16.2) e não adiciona nada arquiteturalmente que não possa entrar depois.

---

## 4. Decisões de design registradas

Decisões estruturais já tomadas, com a alternativa rejeitada. Alterar qualquer uma delas exige revisar este documento.

### D-01 — Simulador único e autoritativo

**Decisão:** existe **um** solucionador de corrida, determinístico e baseado em segmentos (§5). A corrida em 3D é uma **visualização** desse resultado: o carro é animado ao longo do traçado seguindo o timeline de eventos que o simulador já produziu.

**Rejeitado:** física real (WheelCollider / rigidbody) quando visível, simulação estatística quando minimizado.

**Por quê:** dois solucionadores produzem distribuições diferentes. O jogador percebe em poucas horas que o offline "rende diferente", e o pilar P3 morre. Além disso, física real com carro autônomo exige uma IA de pilotagem competente — que é um problema de dificuldade equivalente ao jogo inteiro, e cujo desempenho variaria com o framerate.

**Consequência para a Fase 1:** o protótipo de direção manual **continua sendo o primeiro passo**, mas seu propósito muda: ele existe para *derivar e calibrar as constantes* do simulador (velocidade de curva, ganho de score, sensação de ângulo) e para provar que o resultado é gostoso de assistir. Ele não vira o motor de corrida do jogo.

**Consequência técnica:** todo o `Racing.Simulation` é código puro em C#, sem dependência de `UnityEngine` além de tipos matemáticos, e é testável por unidade.

### D-02 — Recompensa offline sem penalidade, com teto de horas

**Decisão:** o progresso offline rende **100%** do que renderia online, limitado a um teto de tempo acumulável (§17).

**Rejeitado:** render 50–70% offline.

**Por quê:** penalizar percentualmente pune quem trabalha e ensina o jogador a desconfiar do sistema. O teto de horas é um limite legível ("sua garagem comporta 8 horas de operação sem supervisão") e vira um eixo de progressão vendável.

### D-03 — Score é a moeda principal, posição é a secundária

**Decisão:** ver §3.5. Drift Score converte em cash e influencia drops; posição paga um valor base e reputação.

**Rejeitado:** posição como única fonte de recompensa.

**Por quê:** se vencer é tudo, a build ótima é sempre "mais grip, mais potência", e o tuning colapsa numa única dimensão.

### D-04 — Peças com afixos procedurais, carros artesanais

**Decisão:** carros são poucos e desenhados à mão, com identidade mecânica forte. Peças são geradas proceduralmente sobre bases artesanais.

**Rejeitado:** 50 carros com diferenças numéricas.

**Por quê:** um carro com 8 slots e 40 peças possíveis gera mais decisões do que 50 carros com "+5 Power".

### D-05 — Pistas modulares, nunca ruído procedural

**Decisão:** o gerador combina **módulos autorais** (§11.2). Nenhuma geometria é gerada por ruído.

**Rejeitado:** geração totalmente procedural de traçado.

### D-06 — Nenhuma mecânica exige o jogador presente

**Decisão:** todo evento que aparece durante o farm tem uma **regra de automação padrão** que o resolve na ausência do jogador (§16).

**Por quê:** se o evento raro de 0,2% só conta quando o jogador está olhando, o jogo pune o modo de uso principal.

### D-07 — Modo Taskbar é feature de release, não de MVP

**Decisão:** modo Taskbar entra na **Fase 15**, depois do jogo ser divertido em janela normal.

**Por quê:** é a assinatura do produto, mas é uma feature de *apresentação*. Construí-la cedo trava decisões de UI antes de saber quais informações importam.

### D-08 — Preço fixo, sem monetização recorrente

Steam, preço único (faixa alvo US$ 7,99–11,99). DLC de conteúdo é possível no futuro. Nenhuma moeda premium jamais.

### D-09 — Drift é estrutural, não estético; a apresentação é 2.5D

**Decisão:** o jogo continua sendo sobre **drift**, renderizado em **2.5D** — modelos low-poly com sombreamento chapado e câmera traseira **elevada** (§19). A presença do jogador paga um bônus limitado (§3.6), nunca uma taxa.

**Rejeitado — corrida de rua em 2D side-scroller (estilo Task Bar Hero):**

Uma visão lateral pura não consegue mostrar *yaw*. Um carro derrapando visto de lado apenas encurta: a rotação em torno do eixo vertical é justamente o que a perspectiva lateral elimina. Adotar side-scroller obrigaria a trocar drift por corrida de rua.

**Por que isso é fatal:** drift não é o estilo visual deste jogo, é a **parede mestra**. Sem ele existe uma única fonte de sucesso — chegar em primeiro — e a build ótima volta a ser "mais grip, mais potência". Isso é exatamente o colapso que D-03 foi escrita para impedir, e leva junto a §3.5, o segundo eixo de moeda e toda a camada de loot da §10. Adotar side-scroller não seria uma decisão de arte: exigiria inventar uma segunda moeda do zero e reescrever §3.5, §6, §10 e D-03.

**Segundo motivo:** numa visão lateral o traçado é uma linha reta e o canal visual fica vazio — seria preciso fabricar obstáculos (tráfego, rampas, polícia) para preencher a tela. De cima ou de trás, **o próprio traçado é o conteúdo**, e ele já existe de graça pelos módulos autorais de D-05.

**Rejeitado — 2D top-down em sprite:** unificaria os três modos de janela num único renderizador com três níveis de zoom (elegante), e o drift ficaria perfeitamente legível. Perde a sensação de velocidade, que é o que o gênero arcade vende, e a página da Steam fica com cara de protótipo.

**Rejeitado — sprite-scaling 2D autêntico (Mode 7):** numa câmera em perspectiva, sprites são **mais caros** que 3D, não mais baratos. Seria necessário um atlas de yaw × curvatura da pista × body kits da §19.6. Em 3D, ângulo arbitrário de drift é uma rotação de transform — sai de graça.

---

## 5. Modelo de simulação de corrida

### 5.1 Estrutura

Uma corrida é uma **sequência ordenada de segmentos**. O simulador resolve segmento a segmento, carregando estado contínuo (velocidade, temperatura, desgaste, combo, dano).

```
RaceInstance
├── Track      : Segment[]        // gerado a partir dos módulos
├── Car        : ResolvedStats    // base + peças + tuning
├── Driver     : DriverStats      // pós-MVP; MVP usa piloto neutro
├── Conditions : Weather, TimeOfDay, Traffic
├── Difficulty : int (tier)
├── Style      : DriftStyle       // Safe / Balanced / Aggressive / Reckless
└── Seed       : ulong            // reproduz a corrida inteira
```

```
Segment
├── Type       : Straight | Gentle | Sharp | Hairpin | SCurve | Tunnel | Bridge | Junction
├── LengthM    : float
├── Radius     : float            // ∞ para retas
├── Direction  : Left | Right | None
├── Surface    : Asphalt | WornAsphalt | Concrete | Gravel | Snow
├── Walls      : None | OneSide | BothSides    // habilita proximity bonus e risco
└── Difficulty : float (0–100)
```

**Escala:** uma corrida curta tem 18–30 segmentos, ~1,2–2,0 km, resolvida em menos de 0,3 ms.

### 5.2 Grip efetivo

```
μ_base   = 0.60 + (Grip / 100) * 0.60                    // 0.60 .. 1.20
μ_eff    = μ_base · k_tire · k_weather · k_surface · (1 − Wear · 0.35)
```

| Modificador | Valores |
|---|---|
| `k_tire` | Street 0.92 · Sport 1.00 · Semi-Slick 1.08 · Drift 0.95 · Racing 1.15 · Wet 0.88 (seco) / 1.30 (molhado) |
| `k_weather` | Clear 1.00 · Cloudy 1.00 · Rain 0.78 · Heavy Rain 0.66 · Fog 0.95 · Snow 0.55 |
| `k_surface` | Asphalt 1.00 · Worn 0.94 · Concrete 0.97 · Gravel 0.72 · Snow 0.58 |
| `Wear` | 0.0 → 1.0 ao longo da corrida; taxa depende de Power/Weight e do estilo |

Pneu Wet em pista seca superaquece: `k_tire` cai adicionalmente 0.15 após 40% da corrida.

### 5.3 Velocidade de curva

```
v_grip = 3.6 · √(μ_eff · 9.81 · Radius) · k_arcade        // km/h, k_arcade = 1.25
v_grip = min(v_grip, TopSpeed)
```

Exemplo: raio 40 m, Grip 68, pneu Sport, seco → μ_eff ≈ 1.01 → **v ≈ 89 km/h**.

Em reta, o carro acelera em direção a `TopSpeed` com taxa proporcional ao índice de aceleração.

### 5.4 Modo grip × modo drift

Para cada segmento com curvatura, o simulador decide se o carro **derrapa**:

```
initiationScore = Initiation + (Power / Weight_t) · 0.05 + estiloBias
threshold       = 55 − (curvatura · 20)                    // curvas fechadas facilitam
drift           = initiationScore ≥ threshold
```

`estiloBias`: Safe −25 · Balanced 0 · Aggressive +18 · Reckless +35.

Se derrapar:

```
v_drift = v_grip · (0.78 + Angle / 600)                   // 0.78 .. 0.96
```

Ou seja: **drift custa tempo**. Um carro com Angle 100 perde ~4% de velocidade de curva; um com Angle 30 perde 17%. O upgrade de Angle é, ao mesmo tempo, upgrade de score e de tempo — por isso é caro.

`Transition` reduz a perda de velocidade em segmentos S-Curve e em curvas consecutivas de sentido oposto:

```
v_drift += v_grip · (Transition / 100) · 0.10             // apenas em transições
```

### 5.5 Qualidade de execução

Cada segmento em drift resolve uma qualidade: **Bad · Good · Perfect**.

```
skill      = DriftControl + Steering · 0.4 + Stability · 0.3 + DriverSkill
challenge  = Segment.Difficulty · condMod · (1 + estiloRisco)
x          = (skill − challenge) / 18
s          = 1 / (1 + e^(−x))                              // sigmoide

pPerfect   = clamp(s · 0.68,          0.02, 0.75)
pBad       = clamp(0.38 − s · 0.34,   0.03, 0.48)
pGood      = 1 − pPerfect − pBad
```

`condMod`: chuva +12% · noite +8% · neblina +15% · tráfego pesado +10% (cumulativos).

Efeitos:
- **Perfect** → `qualityMult = 1.60`, combo +1, sem perda de velocidade
- **Good** → `qualityMult = 1.00`, combo +1
- **Bad** → `qualityMult = 0.35`, **combo zera**, velocidade de saída −12%

**O simulador não devolve um score final.** Ele devolve uma **lista de resultados por curva**, e um passo de pontuação separado a reduz a um número (§6.2). Essa separação é o que permite a Entrada Perfeita (§3.6.1): o input do jogador edita o `qualityMult` de uma curva já resolvida, **sem tocar em `v_drift` nem em nenhum estado contínuo**.

```
SegmentOutcome
├── SegmentIndex, TimeOffset          // quando a janela de input abre
├── Drift : bool, Angle, VExit        // física — imutável, decidida pelo simulador
├── Quality      : Bad | Good | Perfect   // rolada pelo simulador
├── QualityFinal : Bad | Good | Perfect   // = Quality, ou promovida por input
└── Promotable   : bool               // true apenas se Quality == Good
```

Consequência: online e offline produzem `Quality` idêntico para a mesma `Seed`. A única diferença é que offline nunca promove — o que é exatamente a garantia de D-02.

### 5.6 Tempo e posição

```
t_segment = Length / (v_médio / 3.6)
t_total   = Σ t_segment + penalidades
```

Os adversários **não são simulados individualmente** no MVP. A pista define uma distribuição de tempos de referência por tier:

```
t_ref(tier)  = pista.baseTime · (1 − tier · 0.018)
σ_ref        = t_ref · 0.06
posição      = 1 + #{adversários com tempo < t_total},  amostrados de N(t_ref, σ_ref)
```

Isso dá um grid de 6 carros por ~0 custo de CPU e é indistinguível de simular cada um. Rivais nomeados (§13) **são** simulados de verdade, com carro e build próprios.

### 5.7 Estado contínuo

Acumulado ao longo da corrida e persistido entre corridas quando aplicável:

| Estado | Efeito | Recupera |
|---|---|---|
| `TireWear` 0–1 | reduz μ_eff | troca de pneu (custo) ou fim da corrida no MVP |
| `Temperature` 0–1 | acima de 0.8, Power −10%, risco de falha ×2 | resfria entre corridas |
| `Damage` 0–100 | reduz todas as stats proporcionalmente | reparo (custo em cash) |
| `Combo` int | multiplicador de score | zera em Bad, colisão ou reta longa |
| `Fuel` | *fora do escopo* | — |

**Combustível foi cortado deliberadamente:** adiciona uma restrição de tempo sem adicionar decisão interessante, e conflita com o pilar P3.

---

## 6. Drift Score

### 6.1 Fórmula por segmento

```
base       = Length_m · 10
angleMult  = 0.50 + (Angle_efetivo / 90)                   // ~0.5 .. 1.7
speedMult  = clamp(v_drift / 120, 0.40, 2.50)
comboMult  = 1 + 0.08 · min(combo, 25)                     // 1.00 .. 3.00
proxMult   = 1 + proximidade · 0.30                        // só em segmentos com Walls
transMult  = 1.25 se transição válida, senão 1.00

segScore   = base · angleMult · speedMult · qualityMult · comboMult · proxMult · transMult
```

`Angle_efetivo` = `Angle` modulado pela qualidade: Perfect usa 100% do Angle, Good 85%, Bad 45%.

`proximidade` é rolada em segmentos com parede: `p = clamp((DriftControl − Difficulty) / 100, 0, 1)`, com risco de colisão associado (§12.3).

### 6.2 Total da corrida

```
DriftScore = ⌊ Σ segScore · trackMult · weatherMult · timeMult · eventMult · prestigeMult ⌋
```

`segScore` usa `QualityFinal` (§5.5), não `Quality`. Toda a contribuição da presença do jogador entra por aí — não existe multiplicador separado de "jogador presente", e por isso o teto de 20–30% da §3.6 é uma propriedade emergente que precisa ser **medida**, não assumida:

```
uplift = (DriftScore com todas as promoções − DriftScore sem nenhuma) / DriftScore sem nenhuma
```

Teste de balanceamento obrigatório: `uplift` medido sobre 1.000 corridas precisa cair em `[0.20, 0.30]`. Fora disso, ajustar a janela de input ou a fração de curvas promovíveis no `BalanceConfig`.

### 6.3 Números de referência

Alvos de balanceamento para a primeira hora:

| Situação | Drift Score esperado |
|---|---|
| Carro inicial, pista inicial, dia, seco | 6.000 – 12.000 |
| Mesmo carro após 30 min de upgrades | 20.000 – 35.000 |
| Build de drift dedicada, tier D, noite | 80.000 – 150.000 |
| Endgame tier S, chuva + noite + montanha | 4.000.000 – 12.000.000 |

### 6.4 Feedback visual

Durante a corrida visível, cada evento aparece como um pop-up empilhado:

```
CURVA          +1.240
TRANSIÇÃO      +3.100
PERFECT        +8.200
PROXIMIDADE    +4.500
                        COMBO ×12
```

O contador principal deve subir com *tweening*, nunca saltar. É o principal elemento de espetáculo do jogo.

---

## 7. Estatísticas

### 7.1 Stats primárias (modificáveis por peças e tuning)

| Stat | Unidade | O que faz, em uma frase |
|---|---|---|
| **Power** | hp | Aceleração e velocidade máxima. |
| **Weight** | kg | Divide a potência; peso baixo melhora tudo, mas reduz Stability. |
| **Grip** | 0–100 | Quanta velocidade o carro sustenta em curva **sem** derrapar. |
| **Braking** | 0–100 | Quão tarde o carro entra na curva; encurta o tempo de segmento. |
| **Steering** | 0–100 | Rapidez de resposta; alimenta qualidade de execução e transição. |
| **Stability** | 0–100 | Resistência a rodar; reduz chance de falha. |
| **Initiation** | 0–100 | Facilidade de **entrar** em drift. |
| **Angle** | 0–100 | Quanto ângulo o carro sustenta — o multiplicador de score. |
| **DriftControl** | 0–100 | Chance de Perfect e de proximidade sem bater. |
| **Transition** | 0–100 | Velocidade de troca de lado; bônus em S e curvas encadeadas. |
| **Reliability** | 0–100 | Reduz falha mecânica, dano recebido e custo de reparo. |
| **Cooling** | 0–100 | Segura a temperatura; relevante em Endurance e Endless. |

### 7.2 Stats derivadas (exibidas, nunca editadas diretamente)

```
PowerRatio    = Power / (Weight / 1000)                      // hp por tonelada
Acceleration  = clamp(40 + PowerRatio · 0.09, 0, 100)
TopSpeed      = 130 + Power · 0.20 + Aero · 0.15 − Weight · 0.010     // km/h
RiskIndex     = f(estilo, Stability, Reliability, condições)  // 0–100, mostrado na UI
```

### 7.3 Regra de legibilidade

**Toda stat precisa caber numa frase que um jogador leigo entenda**, e essa frase aparece no tooltip. A tabela §7.1 *é* o texto dos tooltips — se uma stat nova não couber nesse formato, ela não entra.

Na UI da garagem, as 12 stats aparecem agrupadas em 4 barras-resumo (**Potência · Aderência · Drift · Confiabilidade**), com o detalhamento em hover. O jogador casual lê 4 números; o jogador de build lê 12.

---

## 8. Carros

### 8.1 Princípio

Nenhum carro é estritamente melhor que outro. Cada carro tem **um eixo de excelência, um eixo de fraqueza e uma peculiaridade mecânica** que muda como ele é jogado.

### 8.2 Anatomia de um carro

```
CarData (ScriptableObject)
├── Id, DisplayName, Silhueta
├── Layout        : FR | FF | MR | RR | AWD
├── BaseStats     : as 12 stats primárias
├── SlotProfile   : quais slots aceita e quantos (§9.1)
├── Trait         : peculiaridade única (abaixo)
├── TuningRange   : limites por eixo de ajuste fino
├── Rarity / Tier : D .. S
└── UnlockRule    : reputação, blueprint, rival derrotado, região
```

### 8.3 Traits — o que dá personalidade

Cada carro tem **exatamente uma** trait passiva. Exemplos:

| Trait | Efeito |
|---|---|
| **Chassi leve** | −8% Weight efetivo, −10 Stability |
| **Torque bruto** | Initiation +20, Grip −8 |
| **Tração integral** | Ignora 50% da penalidade de clima, Angle máximo −15 |
| **Motor central** | Transition +25, chance de Bad +30% |
| **Frio de fábrica** | Cooling +30; Endurance e Endless ganham +15% de recompensa |
| **Carro de rua** | +20% cash em pistas urbanas, −20% em montanha |
| **Herança de corrida** | Perfect concede combo +2 em vez de +1 |

Traits são o principal vetor de "quero aquele carro" — mais até que as stats base.

### 8.4 Arquétipos (referência de design)

| Arquétipo | Excelência | Fraqueza | Sensação pretendida |
|---|---|---|---|
| Hatch antigo | leveza, custo | potência | primeiro carro, sempre útil |
| Coupé japonês FR | drift equilibrado | nada excepcional | o "carro principal" clássico |
| Muscle | potência absurda | curva, peso | difícil, recompensador |
| Sedan | Stability, Reliability | Angle | carro de farm confiável |
| Compacto | DriftControl | TopSpeed | rei das pistas técnicas |
| AWD | clima, consistência | Angle | especialista em chuva/neve |
| MR | Transition | punitivo | carro de expert |
| Supercar | TopSpeed | Initiation | rodovia e endurance |

### 8.5 Roster do MVP

Três carros, cobrindo três posturas distintas:

| Nome | Layout | Trait | Papel |
|---|---|---|---|
| **Kite 130** | FF | Chassi leve | inicial; barato, previsível, teto baixo |
| **Kanto AE** | FR | Torque bruto | o carro de drift; teto alto, exige tuning |
| **Brute V8** | FR | Herança de corrida | desbloqueio da primeira hora; risco/recompensa |

Nomes são fictícios e devem permanecer assim — **nenhuma marca ou modelo real**, por licenciamento.

---

## 9. Tuning

### 9.1 Slots

O carro tem **8 slots equipáveis**. Cada slot aceita peças de um tipo.

| Slot | Stats que domina |
|---|---|
| Motor | Power, Reliability |
| Turbo / Aspiração | Power, Cooling, Initiation |
| Transmissão | Aceleração, Transition |
| **Diferencial** | Initiation, Angle, Stability |
| Suspensão | Grip, Steering, DriftControl |
| Pneus | Grip (consumível de perfil, §9.3) |
| Freios | Braking, DriftControl |
| Aero / Peso | Weight, TopSpeed, Stability |

Componentes menores citados no brainstorm (ECU, admissão, escape, comando, pistões, molas, amortecedores, cárter, pastilhas, discos, pinças, bancos, bateria, painéis, santantônio, splitter, difusor, saias) **não viram slots**. Eles viram **nomes e artes de peças** dentro dos 8 slots. Oito slots × N peças já produz explosão combinatória suficiente; 25 slots produz planilha.

### 9.2 O diferencial é o slot de assinatura

O diferencial é o slot que mais muda o comportamento do carro, e é onde mora o ajuste fino mais interessante.

| Tipo | Initiation | Angle | Stability | Nota |
|---|---|---|---|---|
| Aberto | −20 | −25 | +15 | fácil, quase não derrapa |
| LSD de rua | 0 | 0 | 0 | referência |
| LSD 1.5-way | +12 | +10 | −5 | equilibrado |
| LSD 2-way | +25 | +22 | −15 | o padrão de drift |
| Competição | +35 | +32 | −28 | requer DriftControl alto |

Além do tipo, o jogador ajusta três eixos contínuos:

```
Trava          ████████░░  80%    → Angle + / Grip −
Aceleração     ███████░░░  70%    → Initiation + / Stability −
Desaceleração  ████████░░  80%    → Transition + / risco de Bad +
```

### 9.3 Pneus

Pneus são o único slot com **perfil de condição** — a peça certa depende do clima e da superfície, não do "nível". Ver `k_tire` em §5.2. Isso força o jogador a manter um estoque e é a porta de entrada natural para o sistema de presets (§9.5).

### 9.4 Progressão de tuning — não é "Level Up"

Cada slot tem duas dimensões independentes:

1. **Tier da peça** — progressão discreta e artesanal (Stock → Rua → Esportivo → Pro → Competição).
2. **Ajuste fino** — sliders contínuos com trade-off explícito, sempre soma-zero-ish. Nenhum slider é "mais é melhor".

**Não existe** botão de "upgrade +1" que só aumenta números. Melhorar significa **trocar** a peça ou **reequilibrar** o ajuste.

### 9.5 Builds e presets

O jogador salva builds nomeadas contendo: peças equipadas + valores de ajuste fino + estilo de pilotagem.

```
BUILD 01 — DRIFT       Power 420 · Grip 62 · Drift 94 · Control 71
BUILD 02 — SPEED       Power 510 · Grip 88 · Drift 40 · Control 82
BUILD 03 — RAIN        Power 380 · Grip 95 · Drift 76 · Control 91
BUILD 04 — FARM        otimizada para cash/hora, não para score
```

A automação pode selecionar a build automaticamente por pista (§16).

---

## 10. Peças e loot

### 10.1 Anatomia de uma peça

```
Part (instância, serializada no save)
├── BaseId       : referência à PartData artesanal
├── Rarity       : Common | Uncommon | Rare | Epic | Legendary
├── ItemLevel    : deriva do tier da pista onde caiu
├── Affixes      : 0–4 modificadores rolados
├── Passive      : 0–1 efeito condicional (Rare+)
└── Seed         : reproduz a rolagem
```

### 10.2 Raridades

Cinco raridades no lançamento. **Prototype e Mythic ficam de fora** — sete raridades diluem o significado de cada uma e o jogador para de ler as cores.

| Raridade | Afixos | Passiva | Peso de drop (tier D) |
|---|---|---|---|
| Common | 1 | não | 62% |
| Uncommon | 2 | não | 26% |
| Rare | 3 | 40% | 9.5% |
| Epic | 4 | sim | 2.2% |
| Legendary | 4 + afixo único | sim, exclusiva | 0.3% |

Pesos deslizam com o tier da pista e com o modificador de risco.

### 10.3 Afixos

Rolados de um pool específico do slot, com valor dentro de faixa.

```
TURBO — Rare — iLvl 14
  +14 Power
  +6  Aceleração
  −3  DriftControl
  Passiva: +8% Drift Score acima de 120 km/h
```

```
TURBO — Rare — iLvl 14
  +8  Power
  +12 Aceleração
  +4  Cooling
  Passiva: +15% de duração de combo
```

Duas peças do mesmo tipo e raridade servindo a builds completamente diferentes — este é o motor do *loot chase*.

**Afixos negativos** existem e são desejáveis: peças com trade-off têm valores absolutos maiores, o que cria peças de alto risco.

### 10.4 Passivas condicionais

As passivas são onde o buildcraft fica interessante, porque interagem com pista, clima e horário:

- `+22% Drift Score em pistas noturnas`
- `Perfect concede +2 de combo em vez de +1`
- `Chuva não reduz o grip nos primeiros 30% da corrida`
- `Cada colisão evitada por proximidade concede +5% de cash`
- `Abaixo de 30% de Reliability, +40% Power`

### 10.5 Sets

Conjuntos de 4 peças com bônus escalonados em 2/3/4 itens. Dois sets no MVP, expandindo para ~8 no lançamento.

```
STREET KING          DRIFT DEMON
 2 pçs  +5% TopSpeed  2 pçs  +10 Angle
 3 pçs  +10% Acel.    3 pçs  +20% duração de combo
 4 pçs  +15% Score    4 pçs  Perfect fica 12% mais provável
```

### 10.6 Salvage e crafting

Toda peça pode ser desmontada em **Scrap**, escalando por raridade e iLvl.

```
Desmontar 3 peças do mesmo slot  →  Scrap
Scrap + Blueprint                →  peça específica com rolagem direcionada
```

O crafting existe para **cortar a cauda do RNG**: o jogador que caçou uma peça por horas precisa de um caminho determinístico. Blueprints caem em pedaços (1/5, 2/5…) e completar um é um objetivo de longo prazo.

### 10.7 Inventário

Cap inicial de 60 slots, expansível. **Auto-sell por raridade é desbloqueado junto com o inventário** — não depois. Um idle game que enche o inventário e para de progredir enquanto o jogador dorme quebra o pilar P3.

```
AUTO-DESMONTAR
  Common      ☑
  Uncommon    ☑
  Rare        ☐
  Epic        ☐
  Legendary   ☐

  Manter peças com afixo de Drift  ☑
  Manter peças de set incompleto   ☑
```

---

## 11. Pistas, regiões e gerador

### 11.1 Regiões

Mapa em grafo, não em lista linear:

```
                 MONTANHA
                     │
CIDADE ────── INDUSTRIAL ────── RODOVIA
   │              │                 │
 COSTA ─────── DESERTO ─────── CIDADE NOTURNA
```

Cada região define: pool de módulos, superfície predominante, clima possível, pool de peças, rivais, trilha sonora e multiplicadores base.

| Região | Favorece | Superfície | Clima típico |
|---|---|---|---|
| Cidade | Transition, Braking | Asfalto | Clear, Rain |
| Industrial | Angle, proximidade | Concreto | Clear, Fog |
| Montanha | DriftControl, Braking | Asfalto gasto | Rain, Fog |
| Rodovia | TopSpeed, Power | Asfalto | Clear, Storm |
| Costa | Grip, Stability | Asfalto | Clear, Rain |
| Deserto | Cooling, Reliability | Gravel | Clear, Storm |
| Cidade Noturna | tudo, com risco | Asfalto | Rain, Heavy Rain |

### 11.2 Módulos

Biblioteca autoral de trechos. Cada módulo é um prefab com traçado, colisão, props e metadados de simulação.

| Categoria | Módulos |
|---|---|
| Retas | Curta · Média · Longa |
| Curvas | Suave E/D · Fechada E/D |
| Técnicos | Curva S · Grampo · Grampo duplo |
| Especiais | Ponte · Túnel · Rampa · Cruzamento · Rotatória |

**MVP: 6 módulos.** Reta média, curva suave E, curva suave D, curva fechada E, curva fechada D, curva S. Isso já gera variedade suficiente para validar o loop.

### 11.3 Gerador de pista

```
TrackGenerator(seed, region, tier, length)
  1. Escolhe um esqueleto rítmico da região (ex.: "técnica", "fluida", "mista")
  2. Preenche com módulos respeitando regras de adjacência
  3. Valida: fechamento do circuito, sem sobreposição, dificuldade dentro da faixa
  4. Gera nome a partir de região + horário + clima + assinatura do traçado
```

**Regras de adjacência** impedem sequências ruins (dois grampos seguidos sem reta, S-curve saindo de rampa). São autorais e ficam num asset de configuração por região.

Exemplo de saída:

```
MONTANHA + NOITE + CHUVA + GRAMPOS + TRÁFEGO PESADO
  → "Serra — Tempestade da Meia-Noite"
  Dificuldade ★★★★☆ · Risco ALTO · Recompensa ×2.8 · Drop raro +40%
```

### 11.4 Horário

Quatro períodos, com efeito mecânico real — não estética:

| Período | Tráfego | Grip | Risco | Recompensa |
|---|---|---|---|---|
| Dia (10–16h) | Alto | 1.00 | Baixo | ×1.0 |
| Pôr do sol (17–19h) | Muito alto | 1.00 | Médio | ×1.3 |
| Noite (20–01h) | Médio | 0.97 | Alto | ×1.6 |
| Madrugada (02–05h) | Baixo | 0.95 | Muito alto | ×1.9 |

O horário do jogo **avança com o tempo real acelerado** (1 dia de jogo = 2 horas reais), e o jogador pode "esperar" ou pagar para forçar um horário. Isso dá ritmo ao dia e cria janelas de oportunidade sem exigir presença.

### 11.5 Clima

`Clear · Cloudy · Rain · Heavy Rain · Fog · Storm · Snow`

Clima é sorteado por região com pesos, previsto com 2 corridas de antecedência (o jogador vê "chuva chegando") e as regras de automação podem reagir a ele — trocar para build de chuva, ou parar de correr.

### 11.6 Composição de multiplicadores

```
rewardMult = timeMult · weatherMult · trafficMult · tierMult · riskMult
```

Exemplos:

```
Cidade · 14h · seco          →  Tráfego ALTO   · Grip normal · Risco BAIXO   · ×1.0
Cidade · 02h · seco          →  Tráfego BAIXO  · Grip normal · Risco ALTO    · ×1.8
Cidade · 02h · chuva forte   →  Tráfego BAIXO  · Grip BAIXO  · Risco EXTREMO · ×3.2
Montanha · 02h · tempestade  →  o teto prático: ×4.5
```

---

## 12. Condições, risco e falha

### 12.1 Índice de risco

Exibido sempre, em quatro faixas: `BAIXO · MÉDIO · ALTO · EXTREMO`.

```
RiskIndex = clamp(
    base(pista, tier)
  + condRisk(clima, horário, tráfego)
  + styleRisk(Safe −20 · Balanced 0 · Aggressive +15 · Reckless +35)
  − (Stability + Reliability) / 4
  , 0, 100)
```

Risco alto aumenta **recompensa, chance de drop raro e chance de falha**, nesta ordem de destaque na UI.

### 12.2 Eventos durante a corrida

Rolados por segmento, com pesos dependentes de região, clima e horário.

**Positivos:** reta livre · zona de drift perfeita · público (bônus de score) · atalho · vácuo · transição perfeita
**Negativos:** tráfego · óleo na pista · poça · polícia · bloqueio · colisão · falha mecânica

Eventos negativos custam tempo, combo e dano — **nunca terminam a corrida** no modo normal. Só o Endless (§14.6) tem terminação por falha.

### 12.3 Falha e dano

```
pFail_seg = 0.004 · riskMult · (1 + overdrive) · (1 − Reliability / 200)
```

Onde `overdrive` cresce com temperatura acima de 0.8 e com desgaste de pneu acima de 0.85.

Uma corrida de 25 segmentos em risco médio tem ~4% de chance de pelo menos uma falha. Em risco extremo com build frágil, ~28%.

**Consequência de falha:** perda de tempo (posição cai), combo zera, `Damage += 8..25`. Dano acumulado entre corridas reduz stats proporcionalmente e é curado por reparo pago (automatizável).

### 12.4 Glass cannon é uma build válida

```
BUILD A   Power 95 · Drift 98 · Control 42 · Reliability 30
BUILD B   Power 72 · Drift 80 · Control 92 · Reliability 95
```

A primeira ganha mais **quando dá certo**; a segunda ganha menos e **nunca falha**. Para o jogador presente, A é melhor. Para farm de 8 horas sem supervisão, B costuma render mais. Essa assimetria entre "build de sessão" e "build de farm" é intencional e deve ser ensinada explicitamente pelo tutorial da automação.

---

## 13. Rivais e eventos raros

### 13.1 Rivais

Rivais são **carros reais, com build real**, simulados pelo mesmo solucionador. Isso significa que a build de um rival é legível, contra-atacável e, quando derrotado, **dropa a peça que ele usava**.

```
RIVAL DETECTADO
  "AKIRA"
  Carro:  ?????
  Score:  ?????
  [ DESAFIAR ]   [ IGNORAR ]
```

Se o jogador estiver ausente, a regra de automação decide (§16).

| Rival | Build | Lição que ensina |
|---|---|---|
| **Ken** | Top speed puro | vencer não é tudo — ele ganha a corrida e perde no score |
| **Mika** | Drift técnico | Transition e DriftControl importam |
| **Ryu** | Angle extremo | ângulo alto custa velocidade |
| **Zero** | Risco máximo | glass cannon; às vezes se destrói sozinho |

Rivais reaparecem em outras regiões com builds evoluídas. Derrotar o mesmo rival três vezes desbloqueia seu carro como blueprint.

### 13.2 Eventos raros

```
EVENTO RARO — CORRIDA DA MEIA-NOITE
Chance: 0,2% por corrida noturna
Um piloto desconhecido te desafiou.
Recompensa: ★★★★★   Peça especial: ????
```

Regras: sempre opcionais, sempre resolvíveis pela automação, e **o log de resultados offline lista todos os que aconteceram** para que o jogador ausente não sinta que perdeu algo.

---

## 14. Progressão

### 14.1 Eixos

```
XP do carro  →  slots de tuning, ajuste fino desbloqueado
Reputação    →  tier de corrida, regiões, carros
Blueprints   →  peças e carros específicos
Prestígio    →  multiplicadores permanentes
Temporada    →  conteúdo novo sem resetar o sistema
```

### 14.2 Tiers, não "dificuldade"

```
D → C → B → A → S
```

Cada tier tem 20 stages internos. Subir de stage aumenta ~4% na dificuldade e ~6% na recompensa. Subir de **tier** muda a natureza: novos afixos, novos módulos, novos rivais, novo teto de raridade.

### 14.3 Soft caps

Para evitar números absurdos, os ganhos por stage entram em rendimento decrescente após o stage 12 de cada tier:

```
efetivo = ganho · (1 − 0.5 · max(0, stage − 12) / 20)
```

O sinal ao jogador é claro: **suba de tier em vez de moer o mesmo tier**.

### 14.4 Progressão horizontal

Tão importante quanto a vertical. Cada bloco de ~45 min deve desbloquear ao menos um item desta lista: carro · pneu · clima · região · horário · rival · slot de garagem · regra de automação · set · blueprint · cosmético.

### 14.5 Prestígio — "Nova Temporada"

Ao atingir **Reputação 100**:

```
REINICIAR TEMPORADA
Perde:  cash, peças equipadas, nível de carros, progresso de região
Mantém: carros desbloqueados, blueprints, cosméticos, conquistas
Ganha:  FAMA → multiplicadores permanentes, carros iniciais melhores,
        regras de automação novas, regiões de partida alternativas
```

A **Fama** compra melhorias em uma árvore, não um multiplicador linear. O jogador escolhe se quer começar a próxima temporada mais rico, mais rápido, com melhor loot, ou com automação mais avançada.

### 14.6 Endgame

Quatro modos:

| Modo | Descrição |
|---|---|
| **Carreira** | progressão por regiões e tiers |
| **Farm** | corrida escolhida em repetição infinita |
| **Desafio** | regras especiais rotativas (sem freio, só grampo, pneu único) |
| **Endless** | pista procedural sem fim; dificuldade +1%/segmento, recompensa +2%/segmento, termina em falha |

O **Endless é o modo idle definitivo**: o jogador o inicia antes de dormir e recebe pela manhã:

```
CORRIDA INFINITA CONCLUÍDA
Distância        182,4 km
Segmentos        426
Drift Score      12.842.991
Peças raras      4
Causa da falha   Superaquecimento
```

### 14.7 Missões

- **Diárias:** 3 objetivos curtos (20 corridas · 100 km de drift · 5 vitórias noturnas)
- **Semanais:** 3 objetivos longos (100 corridas · 3 peças raras · derrotar rival X)
- **Vitalícias:** marcos de coleção (1.000.000 de score · 100 carros · 1.000 corridas)
- **Conquistas secretas:** *Sem Luzes* · *Perto Demais* · *Insano* · *Rei da Garagem*

Toda missão precisa ser cumprível **passivamente** pela automação. Missões que exigem presença são banidas pelo pilar P2.

---

## 15. Economia

### 15.1 Recursos

Cinco, no jogo inteiro:

| Recurso | Fonte | Dreno |
|---|---|---|
| **Cash** | corridas | peças, upgrades, reparo, slots |
| **Scrap** | desmontar peças | crafting, reroll de afixo |
| **Blueprints** | drops raros, rivais, eventos | peças e carros específicos |
| **Reputação** | vitórias, rivais, missões | desbloqueios (não gasta, é limiar) |
| **Fama** | prestígio | árvore de melhorias permanentes |

### 15.2 Fórmula de recompensa

```
cash = (trackBaseCash + DriftScore · 0.02) · positionMult · rewardMult · repBonus
xp   = (trackBaseXP + DriftScore · 0.001) · rewardMult
```

`positionMult`: 1º 1.00 · 2º 0.82 · 3º 0.68 · 4º 0.55 · 5º 0.45 · 6º 0.38

Note que `DriftScore · 0.02` **domina** a recompensa em builds de drift: com 12.000 de score, o drift paga 240 contra ~300 de base. Em 150.000 de score, paga 3.000 contra 300. É por isso que score é a moeda principal (D-03).

### 15.3 Custos

```
custoUpgrade(slot, tier, nível) = 120 · tier^1.6 · 1.16^nível
custoReparo(dano)               = dano · 6 · (1 + tier · 0.3) · (1 − Reliability/300)
custoSlotGaragem(n)             = 5.000 · 2.4^(n−2)
```

### 15.4 Alvos de ritmo

| Momento | Cash/corrida | Corridas por upgrade | Tempo por upgrade |
|---|---|---|---|
| 0–15 min | 300–600 | 3–5 | ~4 min |
| 15–60 min | 800–2.000 | 5–8 | ~7 min |
| 1–5 h | 3.000–12.000 | 8–14 | ~12 min |
| Endgame | escala por tier | 15–25 | ~25 min, absorvido por multiplicadores |

**Regra:** o intervalo entre duas recompensas *significativas* nunca deve passar de ~12 minutos de jogo ativo. Se a curva empurrar além disso, a curva está errada — não o jogador.

### 15.5 Sinks de longo prazo

Sem drenos, a economia colapsa no endgame. Os drenos são: reroll de afixo (Scrap, custo crescente), slots de garagem, expansão do teto offline, reparo em tier alto e crafting direcionado.

---

## 16. Automação

A automação **é** a progressão de meta-jogo. O jogador começa apertando "correr" e termina operando uma organização.

### 16.1 Escada de desbloqueio

```
Auto-corrida  →  Auto-reparo  →  Auto-desmontar  →  Auto-equipar
   →  Auto-build por pista  →  Auto-rota  →  Auto-evento  →  Operação completa
```

Cada degrau é desbloqueado por reputação e tem um custo, e cada um deve ser conquistado *depois* que o jogador sentiu o atrito de fazer aquilo à mão. Automatizar algo que nunca incomodou não é recompensa.

### 16.2 Painel de regras

```
AUTOMAÇÃO

☑ Iniciar corrida automaticamente
☑ Reparar quando dano > 40
☑ Equipar peça melhor (critério: Drift Score estimado ▾)
☑ Desmontar Common e Uncommon
☑ Aceitar eventos com risco ≤ MÉDIO
☐ Aceitar desafios de rival
☑ Trocar para build de chuva quando chover

Nível de risco   BAIXO ──────●── EXTREMO
Recompensa mín.  ×1.5
Parar se         dano > 80  ·  falhas consecutivas > 3
```

### 16.3 Critério de "peça melhor"

O auto-equipar precisa de uma função objetivo explícita e **escolhida pelo jogador**, porque "melhor" depende da build:

```
Maximizar:  Drift Score estimado | Cash/hora | Tempo de volta | Confiabilidade | Build ativa
```

Internamente, o sistema roda o simulador em modo rápido com a peça candidata, N=200 corridas, e compara a média. Isso é barato (§20.4) e **honesto**: usa o mesmo solucionador do jogo, então nunca sugere algo que na prática piora.

### 16.4 Frota

Slots de garagem permitem operações paralelas. Começa em 2, escala até ~12.

```
CARRO 01  Kanto AE   →  Cidade Noturna     →  farm de Drift Score
CARRO 02  Brute V8   →  Montanha           →  caça de peças
CARRO 03  Kite 130   →  Rodovia            →  farm de cash
CARRO 04  —          →  Endless            →  recorde
```

Cada carro tem sua própria build, pista, regras e log. O painel principal do modo Completo é essa lista.

---

## 17. Progresso offline

### 17.1 Os três estados de execução

| Estado | Renderização | Simulação | Custo alvo |
|---|---|---|---|
| **Ativo** | 3D completa, 60 fps | tempo real, 1 corrida por vez | GPU normal |
| **Compacto / Taskbar** | HUD 2D, 15–30 fps | tempo real | < 3% CPU, sem GPU relevante |
| **Segundo plano** | nenhuma | tempo real, tick agregado 1 Hz | < 1% CPU |
| **Offline** | — | reconstruída na volta | instantânea |

Em todos os casos, **o solucionador é o mesmo** (D-01). O que muda é só a taxa de amostragem e o que é desenhado.

### 17.2 Reconstrução offline

Simular 30 dias corrida a corrida é inviável. O sistema usa **agregação estatística do próprio simulador**:

```
1. elapsed = min(now − lastSave, offlineCap)
2. n       = elapsed / (avgRaceTime + interRaceDelay)
3. Roda o simulador K=300 vezes com a config atual → μ e σ de cada métrica
4. Amostra os totais:
     cash, score, xp  ~  Normal(n·μ, √n·σ)
     drops por raridade ~ Binomial(n, p), aproximada por Poisson quando n·p < 30
     eventos raros      ~ Poisson(n·p), com cap por sessão
     falhas             ~ Binomial(n, pFail)
5. Aplica regras de automação sequencialmente sobre o resultado
   (reparos consumiram cash, auto-desmonte gerou scrap, dano parou a operação em X)
6. Gera o log de eventos notáveis (peças Epic+, rivais, eventos raros)
```

O passo 5 é o que faz o offline **parecer vivo** em vez de uma multiplicação: se as regras diziam "parar após 3 falhas consecutivas", o relatório mostra que a operação parou às 4h12 e por quê.

### 17.3 Teto acumulável

```
Teto base:              8 horas
Upgrade "Turno Extra":  +2 h por nível, até 24 h  (custo em Fama)
```

Ao atingir o teto, o relatório diz explicitamente: *"Sua garagem parou às 08:00. Amplie o turno para operar mais tempo."* Nunca esconder o teto.

### 17.4 Tela de retorno

```
BEM-VINDO DE VOLTA

Ausente por 10h 10m  (operação: 8h 00m — teto atingido)

Corridas       127        Vitórias   103    Derrotas  24
Drift Score    +1.245.000
Cash           +78.420
Peças          +34        (2 Epic, 1 Legendary)
Eventos        8

DESTAQUES
  ◆ Turbo Legendary — "Sopro de Meia-Noite"
  ◆ Rival AKIRA derrotado
  ◆ Novo recorde: 2.842.110 de Drift Score

[ COLETAR ]        [ VER LOG COMPLETO ]
```

O log completo é uma linha do tempo navegável. Vale muito mais que o número agregado — é ele que cria a relação com o carro.

### 17.5 Voz

O relatório fala **do carro**, não do jogador:

> *"Seu S13 completou 412 corridas. Encontrou um turbo Legendary. Derrotou AKIRA."*

Essa escolha de voz é barata e é o principal vetor de apego. Deve ser aplicada consistentemente em todo texto de resultado.

---

## 18. Interface e modos de janela

### 18.1 Modo Completo (padrão, 1280×720+)

```
┌──────────────────────────────────────────────────────┐
│ FRAMED DRIFT                       REP 42   ⛁ 82.421 │
├──────────────────────────────────────────────────────┤
│                                                      │
│                   [ CORRIDA 2.5D ]                   │
│                                                      │
│              DRIFT  124.820      COMBO ×12           │
│              ÂNGULO 42°          PERFECT 6/9         │
│              P2/6   ▰▰▰▰▰▰▰▱▱▱  68%                  │
├──────────────────────────────────────────────────────┤
│  GARAGEM  │  TUNING  │  MAPA  │  PEÇAS  │  FROTA     │
└──────────────────────────────────────────────────────┘
```

### 18.2 Modo Compacto (~480×270)

Sem 3D. HUD 2D com traçado esquemático, posição do carro, score, combo, barra de progresso e próximo evento.

A Entrada Perfeita (§3.6.1) **continua disponível** aqui, como um pulso no marcador da curva no traçado esquemático. Coletáveis (§3.6.2) aparecem como um ícone sobre o traçado. Nenhum dos dois existe no modo Taskbar — a 360×48 não há espaço para um alvo clicável honesto, e forçá-lo criaria exatamente a pressão que a regra da §3.6 proíbe.

### 18.3 Modo Taskbar (~360×48) — Fase 15

```
┌────────────────────────────────────────────┐
│ 🚗 ════════╱╲══════ 🏁   124.820  ×12  ⛁82k│
└────────────────────────────────────────────┘
```

Requisitos: sempre-no-topo opcional, arrastável, sem borda, clique abre o modo Completo, tooltip com detalhe. Notificação discreta quando cai peça Epic+ ou aparece rival.

### 18.4 Garagem

Tela central do jogo. Deve responder às três perguntas (§3.4) sem navegação extra.

```
GARAGEM — KANTO AE                          [ TROCAR CARRO ]

  Potência       ████████████████░░░░  412
  Aderência      ██████████░░░░░░░░░░  68
  Drift          ███████████████████░  94
  Confiabilidade ██████████████░░░░░░  72
  Peso                                 1.120 kg

  Motor        Turbo Pro          Rare      ◆◆◆
  Turbo        Sopro de Meia-Noite Legendary ◆◆◆◆◆
  Transmissão  Câmbio Curto       Uncommon  ◆◆
  Diferencial  LSD 2-Way          Epic      ◆◆◆◆
  Suspensão    Coilover Drift     Rare      ◆◆◆
  Pneus        Drift Soft         Common    ◆
  Freios       Pastilha Esportiva Uncommon  ◆◆
  Aero/Peso    Painéis de Fibra   Rare      ◆◆◆

  Estilo   SAFE ──────●─── RECKLESS
  Build    [ 01 DRIFT ▾ ]              [ SALVAR BUILD ]

                                      [ INICIAR CORRIDA ]
```

### 18.5 Princípios de UI

- **Comparação sempre visível.** Ao passar o mouse numa peça, mostrar delta contra a equipada, em stats *e* em Drift Score estimado.
- **Números com significado.** Nunca mostrar uma stat sem a frase que a explica.
- **Sem confirmação para ações reversíveis.** Equipar peça não pede confirmação; desmontar Legendary pede.
- **Cor codifica raridade e nada mais.** Cor não codifica qualidade de rolagem — isso é ícone.

---

## 19. Direção de arte e áudio

### 19.1 Visual

**2.5D: geometria low-poly com sombreamento chapado, paleta neon, iluminação forte, sem realismo.**

Referência de sensação: *Horizon Chase* + *Top Gear (SNES)* + estética PS1/PS2 tratada com pós-processamento moderno — **com a câmera mais alta que a dessas referências** (§19.2).

"2.5D" aqui significa: modelos e cenário são 3D de verdade (é o que torna o ângulo de drift gratuito — ver D-09), mas a linguagem visual é chapada e a leitura é quase de plano único. Nada de PBR, nada de reflexos, nada de sombras suaves.

- Pista ocupando a maior parte da tela, horizonte sempre visível
- Carros pequenos na tela — o jogo é uma miniatura de um universo, não uma vitrine
- Fumaça de drift é o efeito hero, e é um **sistema de legibilidade**, não decoração (§19.3)

**Duas restrições de silhueta, ambas obrigatórias desde a modelagem:**

1. **Legível a 360×48 px** — requisito do modo Taskbar.
2. **Yaw legível a partir da câmera traseira elevada.** Esta é nova e é a mais exigente. Um carro a 40° de ângulo precisa ser *inequivocamente* diferente de um carro reto, mesmo com a perspectiva encurtando a rotação. Na prática: teto e capô com formas assimétricas e de alto contraste, faixa de cor que atravesse o eixo longitudinal, e rodas visíveis o suficiente para que o esterçamento apareça. Um carro cujo topo é uma mancha uniforme falha neste teste.

### 19.2 Câmera

```
posição alvo = carro.pos
             + offset base            (elevada — ver abaixo)
             + offset de velocidade   (recua com v)
             + offset de drift        (desloca lateralmente com o ângulo)
FOV          = base + f(v) + f(ângulo)
```

Nada de corte brusco. Toda transição é amortecida. Durante drift: FOV abre, leve shake, partículas, e a câmera **atrasa** propositalmente em relação ao carro para vender o ângulo.

**Altura da câmera — a decisão que faz o drift funcionar.**

A câmera canônica de Top Gear / Horizon Chase é muito baixa e muito colada. Esse enquadramento é sintonizado para **sensação de velocidade**, não para leitura de curva: naquela altura o yaw fica tão encurtado que um carro derrapando aparece apenas como um carro mais estreito.

Alvo: **30–40% mais alta** que a linha de base do Horizon Chase, com o ângulo de arfagem correspondente apontando um pouco mais para baixo. Isso preserva a estrada correndo em direção ao jogador e recupera a leitura do yaw. Constante do `BalanceConfig`, calibrada na Fase 1.

**O que se aceita perder.** O idioma Top Gear não tem *linha de corrida* significativa: a estrada é uma fita sobre a qual se desliza lateralmente, sem ápice nem geometria de entrada/saída visível. Parte da perícia que a §5 pontua (ângulo de entrada, velocidade de saída) fica invisível. Isso é aceitável porque D-01 diz que o jogador não está pilotando — ele não precisa *ler a linha*, precisa *sentir o drift* e *ler o score*. As duas mitigações são a fumaça (§19.3) e o HUD (§18.1), e ambas são obrigatórias, não opcionais.

### 19.3 Fumaça como sistema de legibilidade

A fumaça não é um efeito de partículas bonito. Com a câmera traseira encurtando o yaw (§19.2), **a fumaça é o canal primário que comunica que existe um drift acontecendo e de que tamanho ele é.** Ela é orçada como sistema de gameplay e recebe teste de legibilidade.

Requisitos:

| Propriedade | Requisito |
|---|---|
| Forma | Fita **curva** contínua atrás do carro, não um jato radial — a curvatura da fita desenha o arco do drift |
| Comprimento | Proporcional ao ângulo e ao combo — leitura à distância de "isto está indo bem" |
| Cor | Iluminada pela cena (neon); cor customizável é cosmético (§19.6) e **nunca** pode reduzir a legibilidade |
| Densidade | Sobe com `qualityMult`; um **Perfect** tem um pulso visual distinto do Good |
| Bad | Colapso visível e imediato — o jogador precisa perceber a perda de combo sem olhar o HUD |

**Teste de saída (Fase 2):** com o HUD inteiramente desligado, um observador que nunca viu o jogo precisa distinguir corretamente Perfect / Good / Bad em 8 de 10 curvas.

### 19.4 Cenários

Ordem de produção: `Downtown → Industrial → Montanha → Rodovia → Cidade Noturna → Costa → Deserto → Porto → Floresta → Neve`

Cada cenário precisa alterar geometria, iluminação, clima possível, tráfego, obstáculos, eventos, drops e trilha — **não apenas textura**.

### 19.5 Áudio

- Trilha por região × período (Dia / Noite / Especial)
- **Rádio** com estações desbloqueáveis: Underground · Synthwave · Rock · Electronic · Lo-Fi · Racing
- Mix adaptativo: no modo Taskbar, música baixa e apenas *stingers* (peça rara, rival, recorde)
- O som precisa funcionar como notificação periférica — o jogador não está olhando

### 19.6 Cosméticos

Body kits, rodas, aerofólios, para-choques, faróis, neon, escapamento, adesivos, insulfilme, pintura, cor de fumaça. Raridades próprias, alguns exclusivos de evento. **Cosmético nunca altera stats.**

---

## 20. Arquitetura técnica

### 20.1 Estrutura de pastas

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/           GameManager · SaveManager · TimeManager · EventBus · Rng
│   │   ├── Data/           ScriptableObjects: CarData, PartData, TrackData, ModuleData...
│   │   ├── Simulation/     ← C# puro, sem UnityEngine, 100% testável
│   │   │   ├── RaceSimulator, SegmentSolver, DriftScorer, LootRoller
│   │   │   └── OfflineAggregator
│   │   ├── Garage/         CarInstance · BuildManager · InventoryManager · Crafting
│   │   ├── Racing/         RaceVisualizer · CameraRig · TrackAssembler · VFX
│   │   ├── Progression/    Reputation · Prestige · Missions · Achievements
│   │   ├── Automation/     RuleEngine · FleetManager
│   │   └── UI/             GarageUI · RaceUI · TuningUI · InventoryUI · MapUI · TaskbarUI
│   ├── Art/  Audio/  Prefabs/  Modules/  Settings/
└── Tests/
    ├── EditMode/           testes do Simulation (determinismo, balanceamento, curvas)
    └── PlayMode/           integração save/load, transições de janela
```

### 20.2 Orientação a dados

**Nenhum número de balanceamento em código.** Tudo em `ScriptableObject` ou em CSV/JSON importado.

```
CarData        Id · Layout · BaseStats · SlotProfile · Trait · TuningRange · UnlockRule
PartData       Slot · Tier · AffixPool · PassivePool · BaseStats · SellValue · DropSources
ModuleData     Type · Length · Radius · Surface · Walls · Difficulty · Prefab · Adjacência
RegionData     Módulos · Clima · Superfície · PartPool · Rivais · Multiplicadores · Trilha
TierData       Dificuldade · Recompensa · Pesos de raridade · Teto de iLvl
BalanceConfig  Todas as constantes das fórmulas deste documento
```

`BalanceConfig` é um único asset com as ~60 constantes citadas aqui. Ele precisa ser editável e recarregável **sem recompilar**.

### 20.3 Determinismo

- RNG próprio (`xorshift128+`) instanciado por corrida a partir de `Seed`. **`UnityEngine.Random` é proibido no namespace Simulation** — teste de edit mode falha se aparecer.
- `RaceInstance` + `Seed` reproduzem a corrida byte a byte. Isso permite: replay, debug de bug reportado, e o "modo rápido" do auto-equipar.
- Fluxos de RNG separados por domínio (execução, loot, eventos) para que mudar a tabela de loot não altere o resultado da corrida.

### 20.4 Orçamento de desempenho

| Operação | Alvo |
|---|---|
| Uma corrida simulada (25 segmentos) | < 0,3 ms |
| Avaliação de auto-equipar (200 corridas) | < 60 ms, fora do thread principal |
| Agregação offline (K=300) | < 100 ms |
| Modo Ativo | 60 fps, < 400 MB RAM |
| Modo Taskbar | < 3% CPU num i5 de 2019 |
| Segundo plano | < 1% CPU, render desligado (`Application.targetFrameRate` + `OnDemandRendering`) |

### 20.5 Save

```
SaveData (JSON versionado)
├── Version, LastTimestampUtc, PlaySeconds
├── Currencies      cash, scrap, fama, reputação
├── Cars[]          instância, XP, dano, build ativa, peças equipadas
├── Inventory[]     peças com seed e afixos rolados
├── Builds[]        presets nomeados
├── Fleet[]         atribuição carro → pista → regras
├── Automation      regras globais e por carro
├── Progress        região, tier, stage, blueprints, missões, conquistas
├── Prestige        temporada, fama gasta, árvore
└── Settings        janela, áudio, atalhos
```

Regras: escrita atômica (temp + `File.Replace`), 3 backups rotativos, autosave a cada 60 s e em todo evento significativo, migração explícita por versão. Timestamp em UTC; detectar relógio andando para trás e **não** punir o jogador (clamp em zero, sem acusação).

### 20.6 Camada de visualização

O `RaceVisualizer` consome um `RaceResult` que já contém a linha do tempo completa:

```
RaceResult
├── TotalTime, Position, MaxCombo, Distance, Damage
├── Rewards       cash, xp, loot[], blueprints[]
├── Timeline      SegmentOutcome[]   // §5.5 — física imutável + qualidade promovível
└── InputWindows  InputWindow[]      // t_abre, t_fecha, segmentIndex  (§3.6.1)
```

**`DriftScore` não faz parte do `RaceResult`.** Ele é calculado por `DriftScorer.Score(timeline)` *depois* que a corrida termina de tocar, porque a `Timeline` ainda pode ser editada pelo input do jogador durante a reprodução (§3.6.1). Chamar o scorer com a timeline intocada devolve o resultado offline — é literalmente a mesma função, o que torna a paridade de D-02 estrutural em vez de uma coisa a testar.

O visualizador interpola o carro ao longo do traçado respeitando os tempos de segmento e dispara VFX nos eventos. Como a linha do tempo inteira é conhecida antes da reprodução começar, as janelas de input podem ser telegrafadas com antecedência — a UI sabe que uma curva vem aí antes de o carro chegar nela. Se o jogador minimizar no meio, nada se perde — a física já existe, e apenas as promoções não acontecem.

**Consequência positiva:** o "replay" sai de graça, e o modo Compacto e o Taskbar consomem exatamente a mesma `Timeline` com outro renderizador.

---

## 21. Balanceamento e telemetria

### 21.1 Ferramenta de balanceamento

Uma janela de editor, construída na Fase 3, que roda **10.000 corridas** com uma configuração e reporta: distribuição de tempo, score, posição, cash/hora, taxa de falha e drops por hora.

Sem essa ferramenta, balancear este jogo à mão é inviável. Ela é a peça de tooling de maior retorno do projeto inteiro e deve vir antes de qualquer conteúdo.

### 21.2 Testes de balanceamento como testes automatizados

```
[Test] Carro inicial na pista inicial: 55%–75% de vitórias
[Test] Build de drift dedicada rende 1.8×–2.6× mais cash que build de velocidade
[Test] Nenhuma peça isolada é ótima em >70% das builds testadas
[Test] Taxa de falha em risco MÉDIO fica entre 3% e 6%
[Test] Cash/hora offline fica dentro de ±8% do cash/hora online medido
```

O último é o teste que **protege o pilar P3** e nunca pode ser desativado.

### 21.3 Telemetria (opt-in, pós-lançamento)

Tempo até o primeiro auto-race, distribuição de builds usadas, pistas farmadas, ponto de abandono, razão entre sessões curtas e longas.

---

## 22. Escopo: MVP e roadmap

### 22.1 Definição do MVP

| Eixo | Quantidade |
|---|---|
| Carros | 3 |
| Pistas | 3 fixas (gerador vem depois) |
| Módulos | 6 |
| Regiões | 1 (Cidade) |
| Horários | Dia / Noite |
| Clima | Seco / Chuva |
| Peças | 20 bases, 8 slots |
| Raridades | 3 (Common, Uncommon, Rare) |
| Afixos | 12 |
| Moedas | Cash + Scrap |
| Rivais | 1 |
| Pilotos | nenhum (piloto neutro embutido) |
| Automação | auto-corrida, auto-reparo, auto-desmontar |

Sistemas obrigatórios no MVP: **corrida simulada · drift score · tuning · loot · inventário · idle · offline**.

### 22.2 Roadmap por fases, com critério de saída

| Fase | Entrega | Critério de saída |
|---|---|---|
| **0** | GDD, planilha de economia, `BalanceConfig` inicial | este documento aprovado e as ~60 constantes preenchidas |
| **1** | Protótipo de direção manual + câmera + drift visual | *é gostoso de assistir 60 segundos sem tocar em nada* |
| **2** | Score de drift, combo, qualidade, HUD, **fumaça** | os números batem com o que o olho vê; teste de legibilidade da §19.3 passa |
| **3** | `RaceSimulator` + janela de balanceamento | clicar START e receber resultado sem renderizar; 10k corridas em < 5 s |
| **4** | Visualizador consumindo `RaceResult` | a corrida visível é indistinguível do protótipo da Fase 1 |
| **4.5** | **Interação presente**: Entrada Perfeita + coletáveis (§3.6) | `uplift` medido em 1.000 corridas cai em [0,20 – 0,30]; timeline intocada rende exatamente o resultado offline |
| **5** | Auto-race em loop infinito | roda 2 horas sem vazamento de memória nem drift de estado |
| **6** | Save + progresso offline | fechar, esperar 1 h, abrir e receber o esperado (±8%) |
| **7** | Garagem, seleção de carro, builds | trocar de carro muda visivelmente o resultado |
| **8** | Tuning: 8 slots + ajuste fino do diferencial | duas builds do mesmo carro rendem >40% diferente |
| **9** | Loot, raridades, afixos, inventário, salvage | jogador consegue verbalizar por que quer uma peça específica |
| **10** | Progressão: XP, reputação, tiers, unlocks | primeira hora planejada (§22.3) funciona ponta a ponta |
| **11** | Conteúdo: regiões, horários, clima, tráfego | pista muda a build ótima de forma mensurável |
| **12** | Gerador procedural de pistas | 200 pistas geradas, nenhuma inválida ou injogável |
| **13** | Automação completa + frota | operação de 8 h sem intervenção rende o esperado |
| **14** | Prestígio, temporadas, Endless | ciclo de reinício é desejável, não punitivo |
| **15** | **Modo Taskbar** e modo Compacto | < 3% CPU, legível a 360×48 |
| **16** | Cosméticos, rádio, conquistas, polimento, Steam | build de demo pública |

**Nota sobre a Fase 1:** ela permanece primeiro apesar de D-01. É lá que se descobre quanto vale um ângulo, quão rápido é rápido, como a fumaça deve se comportar e **qual a altura de câmera em que o yaw fica legível** (§19.2) — informação que vira constante no `BalanceConfig`. Fazer o simulador antes de ter sentido a corrida produz números arbitrários.

**Nota sobre a Fase 4.5:** ela vem depois do visualizador e antes do auto-race, de propósito. Antes do visualizador não há o que clicar; depois do auto-race, o risco é desenhar a interação para agradar quem está olhando e só então descobrir que ela quebra o modo ausente.

### 22.3 A primeira hora, planejada minuto a minuto

| Tempo | Acontece |
|---|---|
| 0–2 min | Uma corrida, um carro, sem menu. O jogador só assiste e vê o score subir. |
| 2–5 min | Primeira peça cai. Equipar. Correr de novo. A diferença é visível. |
| 5–15 min | Garagem abre. Upgrades. Segunda pista. Primeira decisão de build. |
| 15–30 min | Noite desbloqueia (risco × recompensa). Chuva aparece. Pneu de chuva cai. |
| 30–45 min | **Auto-corrida desbloqueia.** Primeiro rival. Segundo carro. |
| 45–60 min | Segunda região. Presets de build. Auto-desmontar. Primeira ausência sugerida. |

Ao fim de uma hora o jogador precisa ter: 2 carros, 5 pistas, 20+ peças, 3 builds, 1 rival, clima, dia/noite, auto-race e garagem. **A partir daí o jogo se sustenta sozinho.**

### 22.4 Cortado explicitamente do escopo

Registrado para evitar que volte por inércia:

- Combustível
- Pilotos e árvore de habilidade de piloto (**pós-lançamento**, é uma camada inteira)
- Raridades Prototype e Mythic
- Multiplayer e leaderboards online (a lista de recordes locais fica; o backend não)
- 25 slots de tuning componentizados
- Dano visual persistente no modelo do carro
- Modo primeira pessoa

---

## 23. Riscos

| # | Risco | Impacto | Mitigação |
|---|---|---|---|
| R1 | "Se eu não piloto, por que me importar?" | fatal | A resposta é o pilar P1 + §3.5 + a interação presente da §3.6. Validar na Fase 5: um testador externo precisa querer voltar depois de 2 h. |
| R2 | O simulador produz corridas indistinguíveis entre si | alto | Ferramenta da §21.1 desde a Fase 3; teste automatizado de variância mínima por segmento. |
| R3 | Explosão de escopo (o brainstorm tem ~15 sistemas) | alto | O MVP da §22.1 é contrato. Nada entra antes da Fase 10. |
| R4 | Offline diverge do online e quebra a confiança | alto | D-01 + teste de ±8% (§21.2), não desativável. |
| R5 | Solo dev + 10 cenários artísticos | alto | Low-poly deliberado; 1 cenário no MVP; módulos reaproveitados entre regiões com paleta e props diferentes. |
| R6 | Números crescem sem controle no endgame | médio | Tiers nomeados + soft caps (§14.3); nenhum número exibido passa de 10 dígitos. |
| R7 | Inventário lota durante a ausência e a operação para | médio | Auto-desmontar sai junto com o inventário, não depois (§10.7). |
| R8 | Modo Taskbar consome CPU demais e o jogador fecha | médio | Orçamento da §20.4 medido a partir da Fase 5, não da 15. |
| R9 | Tuning vira planilha ilegível | médio | Regra de legibilidade §7.3; 4 barras-resumo; delta de score estimado em todo tooltip. |
| R10 | A interação da §3.6 vira obrigação e o jogo deixa de ser idle | alto | Teto de `uplift` em 20–30% testado na Fase 4.5; promoção limitada a Good→Perfect; coletáveis fora da agregação offline; nenhuma interação no modo Taskbar. |
| R11 | Yaw ilegível na câmera traseira e o drift "não aparece" | alto | Restrição de silhueta da §19.1; altura de câmera calibrada na Fase 1; teste cego de fumaça na Fase 2 (§19.3). Se falhar nas duas, a alternativa registrada em D-09 (top-down) volta à mesa — mas antes da Fase 4, não depois. |

---

## 24. Apêndices

### A. Dicionário RPG → Framed Drift

| RPG idle | Framed Drift |
|---|---|
| Herói | Carro |
| Classe | Layout (FR/FF/MR/AWD) + Trait |
| Equipamento | Peça |
| Build | Tuning + ajuste fino + estilo |
| Skill | Passiva de peça / Trait |
| XP | XP do carro |
| Gold | Cash |
| Loot | Autopeça |
| Dungeon | Pista |
| Modificador de dungeon | Horário + clima + tráfego |
| Boss | Rival |
| Região | Cidade / bioma |
| Dificuldade | Tier D–S |
| Auto-batalha | Auto-corrida |
| Party | Frota da garagem |
| Prestígio | Nova temporada (Fama) |
| Endgame | Endless Drift |
| Progresso offline | Corridas durante a ausência |

### B. Constantes de balanceamento (valores iniciais)

Todas vivem em `BalanceConfig`. Valores abaixo são **pontos de partida**, para serem ajustados com a ferramenta da §21.1.

```
k_arcade                1.25
μ_base_min / max        0.60 / 1.20
drift_speed_min / max   0.78 / 0.96
quality_mult            Bad 0.35 · Good 1.00 · Perfect 1.60
combo_step / cap        0.08 / 25
segment_base_score      length_m · 10
cash_per_score          0.02
xp_per_score            0.001
fail_base_per_segment   0.004
upgrade_cost            120 · tier^1.6 · 1.16^nível
offline_cap_hours       8  (→ 24)
offline_sample_K        300
sim_race_budget_ms      0.3
```

### C. Formato de nome de pista gerada

```
{Região} — {Assinatura do traçado} {do/da} {Período}[, {Clima}]

  "Serra — Grampos da Meia-Noite, Tempestade"
  "Centro — Corrida do Pôr do Sol"
  "Zona Industrial — Circuito Fechado da Madrugada"
```

### D. Referências de design

| Referência | O que extrair |
|---|---|
| *TBH: Task Bar Hero* (2026) | filosofia de progressão sem input, janela mínima, combate automático como espetáculo |
| *Horizon Chase* | câmera, legibilidade de pista, leveza arcade |
| *Top Gear* (SNES) | ritmo de corrida, perspectiva, identidade retrô |
| *Path of Exile* / *Diablo* | afixos, sets, loot chase, crafting como corte de RNG |
| *Melvor Idle* | regras de automação como progressão de meta-jogo |
| *Initial D* / *Wangan Midnight* | tom, rivais nomeados, cultura de montanha e madrugada |

---

*Documento vivo. Toda alteração em §4 (decisões registradas) ou §22.1 (escopo do MVP) exige nova versão numerada.*
