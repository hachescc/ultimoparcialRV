# Rick & Morty y las mezclas de otra dimensión — VR (Meta Quest 2/3)

Primera versión jugable de la propuesta (docx), armada contra la rúbrica (pptx).
Todo lo visual son **placeholders** (primitivas con los colores del diseño) para reemplazarlos con modelos de Sketchfab/Mixamo.

## Cómo abrirlo

1. Abrir el proyecto con **Unity 6000.3.13f1**.
2. La escena ya está construida en `Assets/_RickMorty/Scenes/RickGarage.unity` (y en Build Settings).
3. Si algún día hay que regenerarla: menú **Rick y Morty → 1. Configurar proyecto** y luego **Rick y Morty → 2. Construir escena del garaje** (la regenera desde cero; los cambios a mano en la escena se pierden, así que los cambios grandes conviene hacerlos en `Assets/_RickMorty/Editor/RickMortySceneBuilder.cs`).

### Documento con capturas
`Documentacion/RickMorty_Recorrido.pdf`: recorrido completo del juego con capturas, recetas y galería de objetos. Se regenera con **Rick y Morty → 4. Recorrido con capturas** (ver CLAUDE.md).

### Prueba automática
**Rick y Morty → 3. Prueba automática (smoke test)**: entra a Play, salta el tutorial, hace una mezcla válida y una inválida, aparece los 37 objetos, dispara los eventos, llega al final y deja capturas + `reporte.txt` (con los errores encontrados) en `Temp/RickMortySmoke`. Última corrida: 0 errores.

### Probar sin visor (simulador)
Dale Play sin visor conectado: se activa el **XR Interaction Simulator** (mouse + teclado, la ayuda aparece en pantalla).
Atajos de prueba (solo en Editor):

| Tecla | Acción |
|---|---|
| F1 | Saltar intro/onboarding/tutorial |
| F2 | Aparecer kit final (Portal Gun de cartón + Aguardiente) |
| F3 | +60 s al temporizador |
| F4 | Forzar evento inesperado |
| F5 | Aparecer un objeto aleatorio |
| F6 | Terminar (victoria) |
| F7 | Forzar game over (FBI) |
| P | Menú de pausa |

### Probar en Quest
File → Build Profiles → Android → Switch Platform → Build And Run (con el Quest en modo desarrollador). OpenXR + Meta Quest + perfiles Oculus Touch ya quedan activados por el setup; revisar en Project Settings → XR Plug-in Management → Project Validation y aplicar los "Fix" que sugiera.

## Flujo del juego

1. **Intro**: pantalla negra, Rick revolviendo cosas, golpe a la mesa y despertar (diálogo del documento).
2. **Onboarding VR**: mirar el reloj de la muñeca → caminar al círculo verde (joystick izq.) → girar (joystick der.) → agarrar un frasco (grip).
3. **Tutorial de Rick (3 mezclas)**: Mega-Semilla + Meeseeks → *Caja de Meeseeks* (y onboarding del gatillo usándola) → Plumbus (3 ingredientes) → Plumbus + Kalaxianos → *Espada de Zord*. Fallar la 3ª = pantalla blanca y reaparición, como en el guion.
4. **Rick se va**: se abre la puerta del garaje, Rick se va por un portal. Arranca el **temporizador del FBI (15 min)**.
5. **Juego libre**: 36 recetas del documento, pistas de Rick si no hay progreso, notas en el tablero de corcho, eventos inesperados.
6. **Final**: Portal Gun de cartón en el *soporte de ensamblaje* (socket) + Cristales Kalaxianos + Aguardiente → transmutación → agarrar la pistola real → holograma de Rick → disparar a una pared → cruzar el portal → resultados y créditos en el espacio.
7. **Fallo**: si se acaba el tiempo, el FBI entra (pantalla roja → resultados).

## Cómo cubre la rúbrica

| Requisito | Dónde está |
|---|---|
| XR Interaction Toolkit (Grab, Socket, Botones, UI XR) | `XRGrabInteractable` en todos los objetos, `IngredientSocket` (soportes de frascos + ensamblaje final), `MixButton` (XRSimpleInteractable + Poke), menú de pausa, holograma y resultados con `TrackedDeviceGraphicRaycaster` |
| Onboarding VR | `GameManager.CoOnboarding` (reloj, caminar, girar, agarrar, gatillo) |
| Temporizador | `GameTimer` (FBI, avisos a 5/3/1 min) |
| Fallo | Explosión por mezcla inválida, francotiradores del FBI al salir del perímetro, abrazo del Tío Steve, game over por tiempo |
| Evento inesperado | `EventDirector`: apagón, invasión del Tío Steve, Meeseeks pirata que roba ingredientes, temblor interdimensional |
| Sistema reactivo a eventos | `GameEvents` (bus de eventos): puntaje, UI, Rick y criaturas reaccionan sin acoplarse |
| Puntuación y progreso verificable | `ScoreManager` + reloj + monitor + holograma de descubrimientos (X/37) |
| Cierre claro con retroalimentación | `EndScreen`: puntaje, rango, tiempo, descubrimientos, explosiones, kills, easter eggs, historial |
| Manejo de datos | Items/recetas/diálogos en JSON (`Assets/_RickMorty/Resources/Data`), guardado JSON en `persistentDataPath/rickmorty_save.json` (colección, mejor puntaje, historial, opciones) |
| Optimización VR | Sin post-proceso permanente, luces sin sombras salvo el sol, sin GI horneado, materiales compartidos/cacheados, pool de audio, agujero negro para limpiar objetos, etiquetas solo de cerca |
| Inmersión | Audio espacial en todo, háptica (tazón bueno/malo, golpes, reloj), UI diegética (reloj, monitor, corcho), comer acercando a la boca |
| Confort | Viñeta dinámica y giro snap/suave en el menú de pausa (se guardan) |

## Organización

```
Assets/_RickMorty/
  Editor/        RickMortySetup.cs (paso 1), RickMortySceneBuilder.cs (paso 2)
  Resources/Data items.json, recipes.json, dialogues.json   ← editar aquí recetas y textos
  Resources/     MaterialLibrary.asset, ItemVisualRegistry.asset
  Scripts/Core   GameManager, GameEvents, GameDatabase, SaveSystem, ScoreManager, GameTimer, AudioFX, FX, MatLib
  Scripts/Player PlayerRig, PlayerHead, ScreenFX, ComfortVignette, PlayAreaBoundary
  Scripts/Items  MixableItem, BaseIngredient, ItemFactory, Behaviours/ (láser, SCAR, katana, sable, cuchillos, comida...)
  Scripts/Mixing MixingBowl, MixButton, IngredientSocket, FinalAssemblyStation
  Scripts/NPC    Creature, FakeUncleSteve, MeeseeksNPC, RickCaleno, Capybara, RickNPC, RickHologram...
  Scripts/UI     WristWatch, DiscoveryHologram, RickMonitor, CorkBoard, PauseMenu, EndScreen, UIKit
  Scripts/Environment  EventDirector, GarageDoor, FlickerLight, BlackHoleTrash, EasterEgg...
```

## Modelos 3D
Ya están todos integrados (`Assets/_RickMorty/Models`, optimizados para Quest). Los originales descargados quedaron en `_ModelosOriginales/` (fuera de Assets, Unity no los carga). Herramientas de Blender en `_Tools/blender/`.

## Cómo cambiar un modelo (sin tocar código)

**Objetos del juego**: abrir `Assets/_RickMorty/Resources/ItemVisualRegistry.asset`. Ya tiene una fila por cada id; arrastrar el prefab del modelo al campo `prefab` (ajustar `scale`/offsets). La fábrica lo usa automáticamente en vez del placeholder (la física, el agarre y los comportamientos se mantienen).

**Escenario** (garaje, Rick, casas…): reemplazar los objetos de la escena a mano después de construirla.

**Voces de Rick**: poner audios en `Assets/_RickMorty/Resources/Audio/Rick/<id>.wav` usando el `id` de `dialogues.json` (ej. `intro_wake.wav`). Los subtítulos se sincronizan solos con la duración del audio.

**Efectos de sonido**: `Assets/_RickMorty/Resources/Audio/SFX/<nombre>.wav` reemplaza el sonido sintetizado (nombres: `explosion, splat, portal, laser, shot, bubbles, hum, arcade_win, watch_ping, alarm, siren, chord, bark, burp, chomp, fuuump, impact_metal, impact_glass, impact_flesh, impact_wood, tinnitus, scream, zap, snap...`).

**Música**: `Assets/_RickMorty/Resources/Audio/Music/bgm` (se detiene en el clímax como pide el documento).

## Modelos que hay que conseguir (Sketchfab / Mixamo)

Prioridad alta (se ven todo el tiempo):
- **Rick** (personaje rigged) + animaciones de Mixamo: *Idle*, *Talking*, *Walking*.
- **Frascos/matraces de laboratorio** (8 ingredientes base), **tazón de vidrio**, **mesa de trabajo**, **estantería metálica**, **brazo robótico**.
- **Pistola de portales** (real) y una versión de cartón (o la misma con textura de cartón).
- **Garaje**: lavadora, secadora, fregadero, puerta de garaje, lámpara fluorescente, caja de herramientas, escoba.

Prioridad media (resultados de mezclas): Caja de Meeseeks, Plumbus, katana, sable de luz, karambit, kunai, SCAR dorada, pistola láser sci-fi, guantelete del infinito, moneda V-Bucks, botella de aguardiente, salsa Szechuan, empanada, papa rellena, camiseta del América de Cali, Sr. Pantalones de Popó, pepino mutante, Tío Steve, Meeseeks (personaje), toallín, peluche Teto, dakimakura Miku, casco de Snowball, caja de cereal Eyeholes, VHS, batería, semilla.

Easter eggs: casco Iron Man, Pokébola, cubo Prison Realm, esfera del dragón, figura de Miku, llama de Fortnite, **capibara**, calcomanía de Colombia (textura).

Exterior: casa suburbana de 2 pisos, casa de un piso, valla blanca, camioneta negra (FBI), barricadas y conos, aro de baloncesto.

## Pendiente / ideas siguientes
- Cambiar placeholders por modelos.
- Grabar o generar las voces.
- Socket en el hombro para el Sr. Pantalones y "estirar" de verdad los ingredientes al verterlos (hoy se mezclan metiéndolos al tazón).
- Probar en el Quest y ajustar alturas/tamaños.
