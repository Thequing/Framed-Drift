# Prompts de textura — Framed Drift (ChatGPT / DALL·E)

Como usar: mande **o BLOCO 0 (estilo) + UM bloco de textura** por geracao. Nunca peca as
quatro numa imagem so.

Nomes de arquivo sao obrigatorios — `Resources.Load` usa o caminho exato, incluindo o
typo `GroudPlane`:

| Destino | Caminho |
|---|---|
| Asfalto | `Assets/_Project/Resources/Ground/Asphalt.png` |
| Chao | `Assets/_Project/Resources/Ground/GroudPlane.png` |
| Placas/muro | `Assets/_Project/Resources/Environments/Sign.png` |
| Horizonte | `Assets/_Project/Resources/Environments/BackGround.png` |

---

## BLOCO 0 — Estilo compartilhado (cole no topo de TODA geracao)

```
You are generating a game texture for a stylized Japanese street-racing game set at
night. Read these art rules first; they apply to every image in this series.

ART DIRECTION
- Subject culture: Japanese night street racing — touge mountain passes, Wangan
  expressway, bosozoku and drift-team aesthetics, kanban neon, vending machines,
  guardrails, industrial bayside. Confident and glamorous, not gritty or realistic.
- Time: night, always. There is no sun. Every light in the image comes from neon,
  sodium vapor lamps, headlights, or signage.
- Rendering style: FLAT, CEL-SHADED, VECTOR-LIKE. Hard edges, crisp shapes, bold
  color blocking, limited banded gradients. NO photographic texture, NO film grain,
  NO noise, NO blur, NO depth of field, NO lens flare, NO bloom.
- Lighting is BAKED: the image itself is the final rendered color. The engine applies
  zero lighting on top of it. So paint the glow and shadow directly into the pixels,
  and never rely on a light that isn't drawn.
- Because the game samples this texture with POINT filtering, every edge must be
  intentional and readable. Prefer few large shapes over many small ones.

COLOR PALETTE (use these, vividly and saturated)
- Hot magenta        #FF2E88
- Electric cyan      #00E5FF
- Acid lime          #C8FF2E
- Sodium orange      #FF7A1A
- Deep violet        #2B0B4A
- Midnight navy      #0B0A1F  (darkest value — never use pure black)
Neon accents should sing against the dark. Keep the darks blue/violet, never gray.

TEXT
- Avoid legible words. Any Japanese signage must be abstract katakana/kanji-LIKE glyph
  blocks used as shape and color, not as readable text.

OUTPUT
- A single flat texture image. No border, no frame, no drop shadow, no mockup, no
  perspective preview, no watermark, no annotation, no color-swatch strip.
```

---

## BLOCO 1 — Asfalto (`Asphalt.png`)

Restricoes reais: U vai de 0 a 1 UMA vez na largura inteira da pista (9 m), V repete a
cada fatia de 4 m. Logo a imagem quadrada cobre 9 m x 4 m, e **o vertical fica esticado
2,25x**. Faixas continuas moram nas bordas esquerda/direita da imagem. So topo/base
precisam casar.

```
[COLE O BLOCO 0 AQUI]

TEXTURE 1 OF 4 — ASPHALT ROAD SURFACE
Output: 2048 x 2048 pixels, square.

WHAT IT REPRESENTS
A top-down orthographic view, camera pointing straight down, of a patch of road that is
9 meters wide (left edge to right edge of the image) and 4 meters long (bottom edge to
top edge of the image). The full width of the road fits in the image exactly once.

CRITICAL ASPECT DISTORTION
The image is square but the patch is 9 m by 4 m, so the vertical axis is stretched 2.25x
compared to the horizontal. Everything must be drawn PRE-STRETCHED VERTICALLY: a shape
that should look square on the road must be drawn 2.25 times taller than it is wide.

TILING
- The TOP and BOTTOM edges must match perfectly so the texture repeats seamlessly in the
  vertical direction. Any element crossing the top edge must continue at the bottom.
- The LEFT and RIGHT edges must NOT tile — they are the road's outer edges.

REQUIRED LAYOUT
- Two continuous solid edge lines running the full height of the image, one near the far
  left and one near the far right, inset about 70 px from each border. Paint them in a
  glowing electric cyan #00E5FF that reads like reflective road paint catching neon.
- ONE center-line dash, centered horizontally, roughly 35 px wide and 1500 px tall (this
  is a 3 m dash pre-stretched), in acid lime #C8FF2E with a faint bloomless glow. Leave
  the remaining vertical gap split evenly between the top and bottom edges so the dash
  rhythm stays continuous when tiled. Exactly one dash — do not add more.
- Base surface: dark blue-violet asphalt between #14121F and #1E1B2E, with flat patches
  of slightly different value suggesting old repairs and seams.
- Drift skid marks: long, wide, slightly curved black-violet rubber streaks running
  mostly along the vertical axis, overlapping and layered, as if hundreds of cars have
  slid through here. This is the personality of the texture.
- Wet-asphalt reflections: soft vertical streaks of desaturated magenta #FF2E88 and cyan
  #00E5FF, as if neon from off-screen signage is smeared along the road. Keep them low
  contrast so they do not strobe when the texture repeats.

DO NOT
- No arrows, no numbers, no crosswalks, no manhole covers, no curbs, no gravel shoulder.
- No horizontal lines or bands — they would look like a fence when the road repeats.
```

---

## BLOCO 2 — Chao lateral (`GroudPlane.png`)

Restricoes reais: ladrilho de 8 m x 8 m num plano de 4 km. Repete umas 500 vezes por
lado, entao qualquer elemento marcante vira grade visivel. Precisa costurar nos DOIS
eixos.

```
[COLE O BLOCO 0 AQUI]

TEXTURE 2 OF 4 — GROUND BESIDE THE ROAD
Output: 2048 x 2048 pixels, square.

WHAT IT REPRESENTS
A top-down orthographic view of an 8 meter by 8 meter patch of the terrain that surrounds
the racetrack — the shoulder and roadside of a Japanese mountain pass at night. Pixels
are square here: no aspect distortion, draw everything in true proportion.

TILING — THE HARDEST REQUIREMENT
This must be a PERFECTLY SEAMLESS TILE in BOTH directions: left edge matches right edge,
top edge matches bottom edge. It repeats roughly 500 times across the visible world, so:
- NO large or distinctive single element anywhere. No rocks, no signs, no puddles, no
  drain covers, no bright hotspots. Any one of these becomes a visible grid.
- Keep overall contrast LOW and value distribution EVEN across the whole square. If you
  squint at it, it should read as one uniform tone with no center of attention.

CONTENT
- OVERALL VALUE: this tile must be DARK. It sits beside a lit road and must never compete
  with it. Nothing in this image may be brighter than #22203A. Aim for an average
  brightness around 8-12% — deep night: readable, but clearly in shadow.
- Dark packed dirt and fine gravel in near-black violet-navy, values between #05040D and
  #14111F, evenly scattered.
- Sparse, small clumps of dry roadside grass and weeds in a very dark, heavily
  desaturated teal around #10201E — barely distinguishable from the dirt in value, tiny
  and evenly distributed, never clustered.
- A very faint, even wash of magenta #FF2E88 and cyan #00E5FF across the surface — the
  distant neon of the city bleeding onto the ground. Keep it at very low opacity: it
  should tint the hue without lifting the brightness.
- Fine cracks and pebble detail at small scale to keep it from reading as flat color,
  drawn only as slightly darker marks, never as lighter highlights.

DO NOT
- No road paint, no asphalt, no curb, no grass blades large enough to identify, no
  flowers, no leaves, no visible repetition pattern, no vignette, no darkened corners.
```

---

## BLOCO 3 — Placas / muro lateral (`Sign.png`)

Restricoes reais: cada repeticao ocupa ~1,33 m x 1,4 m de parede (3 por fatia de 4 m).
Repete so na horizontal. O engine ESPELHA a imagem em U na parede direita, e o codigo
conta com o desenho ser simetrico em cima/baixo — por isso a exigencia de simetria
horizontal-central abaixo.

```
[COLE O BLOCO 0 AQUI]

TEXTURE 3 OF 4 — ROADSIDE BARRIER / DIRECTION SIGN PANEL
Output: 2048 x 2048 pixels, square.

WHAT IT REPRESENTS
A flat, straight-on front view of one repeating panel of the barrier wall that runs along
both sides of the track. In the game one copy of this image covers a piece of wall about
1.33 meters wide and 1.4 meters tall, so treat the proportions as square. The BOTTOM edge
of the image sits at road level; the TOP edge is the top of the barrier.

TILING
- The LEFT and RIGHT edges must match perfectly so the panel repeats seamlessly in a long
  horizontal run. A horizontal band crossing the right edge must continue at the left.
- The TOP and BOTTOM edges do NOT tile — they are the top and base of the wall.

MANDATORY SYMMETRY (non-negotiable)
The image must be MIRROR-SYMMETRIC ABOUT ITS HORIZONTAL CENTERLINE — the top half is an
exact vertical mirror of the bottom half. The engine flips this image horizontally for the
opposite wall, and that only reads correctly if the design is symmetric top-to-bottom.
Design accordingly: build the panel out of horizontal bands mirrored around the middle.

CONTENT
- A dark steel guardrail panel in deep violet #2B0B4A over midnight navy, with a strong
  horizontal rib running along the centerline — the classic Japanese touge Armco profile.
- A bold directional CHEVRON in hot magenta #FF2E88, drawn as a mirrored pair (one arm
  pointing up-and-across, its mirror pointing down-and-across) so the whole mark is
  symmetric about the horizontal centerline and reads as a single arrow shape.
- Reflective delineator studs in acid lime #C8FF2E, one near the top edge and its mirror
  near the bottom edge, glowing as if catching headlights.
- Thin electric cyan #00E5FF edge glow lines along the very top and very bottom of the
  panel, mirrored.
- Light weathering: flat chipped paint, a few scrape marks from cars clipping the wall.
  Keep the wear symmetric or subtle enough not to break the mirror.

DO NOT
- No text, no numbers, no logos, no posts or vertical support columns (they would repeat
  into a picket fence), no perspective, no ground, no sky, no background behind the panel.
```

---

## BLOCO 4 — Horizonte / cidade (`BackGround.png`)

Restricoes reais: panorama 360 graus num cilindro de 500 m, texturizado por dentro. A
linha do horizonte tem de cair em **34,6% da altura, medindo da base** (`_horizonV`), e
o topo da imagem cobre so 38 graus acima do horizonte.

```
[COLE O BLOCO 0 AQUI]

TEXTURE 4 OF 4 — 360 DEGREE NIGHT SKYLINE PANORAMA
Output: the widest landscape image you can produce, as close to a 2:1 width-to-height
ratio as possible. Aim for 2048 x 1024 if available.

WHAT IT REPRESENTS
A single continuous 360-degree panoramic view of the horizon, seen from the middle of the
racetrack. It is wrapped around the inside of a cylinder, so it must read as one
uninterrupted circular vista, not as a framed picture.

HORIZON PLACEMENT (exact, this drives the geometry)
- The horizon line must sit at 34.6% of the image height, measured UP FROM THE BOTTOM
  edge. In a 1024-tall image that is 354 px above the bottom.
- Everything BELOW that line is ground that will be almost entirely hidden. Make it a
  flat, featureless dark violet-navy gradient. Put no detail there.
- Everything ABOVE that line covers only 38 degrees of sky — a narrow band just above the
  horizon, NOT a full sky dome. So keep the city low: buildings and mountains should
  occupy roughly the lower half of the sky band, leaving open sky above them.

SEAMLESS WRAP
- The far LEFT and far RIGHT edges must connect perfectly into a loop. Make this easy:
  compose so that both edges land on empty haze and open sky with no building or mountain
  crossing them.
- Do not repeat a distinctive landmark twice — the player sees the whole loop when
  cornering.

CONTENT, left to right around the loop
- A dense Japanese night skyline: mid-rise blocks and towers in flat silhouette, studded
  with tiny warm window lights and vertical neon kanban signboards glowing magenta,
  cyan, and sodium orange.
- An elevated expressway on stilts crossing part of the view, with a streak of red
  taillights and white headlights along it — the Wangan.
- Bayside industrial section: port gantry cranes, a couple of chimney stacks with
  aviation lights, all in flat dark silhouette.
- Layered mountain ridges behind the city in progressively lighter violet, the touge.
- Sky: a vertical banded gradient from deep violet #2B0B4A at the top down to a hot
  magenta and electric cyan glow concentrated right at the horizon, where the city's
  light pollution sits. A few flat clouds lit from below in magenta.
- Aerial perspective: the whole panorama sits 500 meters away, so lower the contrast and
  push distant elements toward the haze color. Nothing here should be as bright or as
  dark as the foreground.

DO NOT
- No foreground objects, no road, no cars in the near field, no ground detail, no stars
  large enough to be individual dots, no moon large enough to be a landmark, no frame,
  no vignette.
```

---

## Depois de gerar

1. Salve com os nomes exatos da tabela do topo (atencao ao `GroudPlane`).
2. `maxTextureSize` esta em 2048 nos `.meta` — imagens maiores sao reduzidas na
   importacao, o que e aceitavel.
3. O ChatGPT nao garante tiling perfeito. Confira as emendas; asfalto (topo/base), chao
   (quatro lados) e placa (esquerda/direita) sao os que quebram.
4. O backdrop provavelmente sai em 3:2 e nao 2:1. Se sair assim, me diga a resolucao
   final que eu reajusto `_horizonV` e `_topAngleDegrees` no `Backdrop.cs` em vez de
   voce distorcer a imagem.
5. Parametros que da para afinar sem gerar de novo: `_asphaltTilesPerSlice`,
   `_signsPerSlice`, `_groundMetersPerTile`, `_horizonV`.
