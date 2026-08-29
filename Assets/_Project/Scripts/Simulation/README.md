# FramedDrift.Simulation

**C# puro. Nada de `UnityEngine` aqui dentro.**

O asmdef desta pasta tem `noEngineReferences: true`, o que faz a proibicao da GDD
(secao 20.3, "UnityEngine.Random e proibido no namespace Simulation") virar erro de
compilacao em vez de disciplina.

Consequencias praticas:

- Use `DeterministicRng`, nunca `UnityEngine.Random`.
- Use `System.Numerics` ou matematica propria, nunca `Vector3` / `Mathf`.
- Tudo aqui e testavel em EditMode sem abrir uma cena.

## Duas licencas tomadas em relacao a secao 20.1

1. **`DeterministicRng` mora aqui, nao em `Core/`.** A GDD lista `Rng` sob `Core/`,
   mas `Core` referencia `UnityEngine` e este assembly nao pode depender dele.
   O gerador precisa ser puro para a corrida ser reproduzivel.
2. **`BalanceSettings` (POCO) mora aqui; `BalanceConfig` (ScriptableObject) mora em
   `Data/`.** Mesma razao: a secao 20.2 quer as constantes num asset editavel sem
   recompilar, mas um `ScriptableObject` nao existe em C# puro. O asset produz o POCO.
