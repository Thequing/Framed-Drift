# FramedDrift.Data

## Onde o conteudo mora, e por que nao em ScriptableObject

A GDD 20.2 exige duas coisas do conteudo:

1. **Nenhum numero de balanceamento em codigo.**
2. **`BalanceConfig` precisa ser editavel e recarregavel sem recompilar.**

A secao lista `ScriptableObject` **ou** CSV/JSON importado. O MVP usa **JSON**, em
`Assets/_Project/Resources/Content/`, carregado por `GameContent` e transformado em
`FramedDrift.Simulation.Content.ContentDatabase`.

Tres razoes:

- **O simulador nao pode ver `UnityEngine`.** O asmdef de `Simulation` tem
  `noEngineReferences: true` (GDD 20.3), entao um `ScriptableObject` nunca poderia ser o
  formato que o solucionador le. Com SO haveria sempre uma camada de conversao; o JSON
  e lido direto pelo assembly puro.
- **Os testes de balanceamento rodam fora do editor.** Os criterios de saida das fases
  3, 4.5 e 6 sao numeros medidos sobre 1.000-10.000 corridas. Com JSON, esse harness
  roda em `dotnet` em menos de um segundo, sem abrir o Unity.
- **Diff legivel.** `git diff` de um `.asset` YAML com GUIDs nao mostra que a chance de
  falha mudou. Num `.json` comentado, mostra.

## As classes ScriptableObject continuam aqui

`CarData`, `PartData`, `ModuleData`, `TrackData`, `RegionData`, `TierData` e
`BalanceConfig` seguem no assembly, **sem uso em tempo de execucao**. Elas sao o caminho
de autoria previsto pela GDD 20.2 e voltam a ser relevantes quando houver conteudo
demais para editar a mao — provavelmente junto com o gerador da Fase 12, com um
importador `.asset -> .json` no editor.

Ate la, editar o JSON e a forma suportada, e `ContentValidator` falha o carregamento se
o resultado for inconsistente.

## Formato

Um arquivo por tipo, todos com um array na raiz nomeado pelo tipo:

```
balance.json    as ~60 constantes das formulas (GDD apendice B)
tiers.json      D..S: dificuldade, recompensa, pesos de raridade, teto de iLvl
modules.json    modulos autorais + regras de adjacencia (D-05, GDD 11.2)
tracks.json     pistas fixas do MVP (o gerador da Fase 12 produz o mesmo formato)
regions.json    pool de modulos, clima com pesos, pool de pecas, multiplicadores
cars.json       roster artesanal (D-04)
parts.json      20 bases nos 8 slots
affixes.json    12 afixos, com sinal e marcacao de trade-off
passives.json   passivas condicionais (Rare+)
sets.json       conjuntos com bonus em 2/3/4 pecas
rivals.json     carros reais com build real (GDD 13.1)
```

O leitor aceita **comentarios de linha** (`//`). Os arquivos sao editados a mao e
explicar uma constante ao lado dela vale mais que a pureza do formato.

Enums sao escritos por **nome**, nunca por indice, para que o conteudo sobreviva a uma
reordenacao do enum sem virar outro valor silenciosamente.
