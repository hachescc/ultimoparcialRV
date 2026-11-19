# Modelos a conseguir (Sketchfab / Mixamo)

> **Estado (2026-11-15): YA NO HACE FALTA BUSCAR NADA.** Todo está integrado: lo descargado se optimizó y lo que faltaba se generó por código en Blender (`_Tools/blender/generate_models.py`). Detalle de qué se usó de cada cosa en `CLAUDE.md` → "Pipeline de modelos". Esta lista queda como referencia si quieren reemplazar algún generado por uno mejor: se pone el GLB en `_ModelosOriginales/`, se agrega una línea en `_Tools/blender/process_models.py` y se vuelve a construir la escena.

## Reglas generales (valen para todos)
- En Sketchfab filtrar por **Downloadable**. Licencia **CC** o **CC0** y anotar el autor (va en los créditos).
- **Low poly / pocos triángulos**: el Quest es un celular. Como guía: objetos chicos < 10k triángulos, muebles < 20k, personajes < 50k, casas < 30k. Sketchfab muestra el conteo ("Triangles") en la página del modelo.
- Formato **FBX** (ideal) o **glTF/GLB**. Que traiga **texturas incluidas** (no solo el material vacío).
- **Un solo objeto por modelo**: nada de "escenas completas" o "packs de 50 cosas en un solo archivo", salvo que se diga lo contrario.
- **Estilo**: el juego es caricatura (Rick & Morty). Mejor **estilizado/cartoon o low poly limpio** que fotorrealista. Si hay que elegir entre uno fiel a la serie y uno genérico bonito, prefieran el fiel a la serie.
- Donde dice **"genérico ok"** no hace falta que sea de Rick & Morty: el color, la etiqueta o la textura los pongo yo.
- La columna **id** es lo que yo uso para conectarlo en el juego; ustedes no la necesitan, pero ayuda que nombren la carpeta igual.
- **Tamaño real** es el tamaño en el juego. No importa la escala del archivo (la ajusto yo); sirve para saber qué tipo de objeto es.

---

## 1. Prioridad ALTA (se ven todo el tiempo)

| Qué | Buscar | Aclaración |
|---|---|---|
| **Rick** | `rick sanchez`, `rick sanchez rigged` | Personaje **completo de cuerpo entero** (no solo la cabeza ni un busto), con bata blanca. Ideal **rigged** (con esqueleto). Si no viene rigged, igual sirve: lo subimos a Mixamo para que le ponga esqueleto. **No sirven**: Funko Pop, figuras cabezonas ni versiones chibi. Pose en T o en A, mejor. ~1,8 m. |
| **Mesa de trabajo** | `workbench`, `wooden workbench` | **Solo la mesa**, de **madera**, rústica y usada. Rectangular y larga (~2,4 m × 1 m, a la altura de la cintura), con o sin repisa abajo. **Sin** tornillo de banco, sin herramientas pegadas encima y sin tablero de herramientas atrás: la mesa va **vacía**, yo pongo todo encima. **No** una mesa de oficina ni de cocina. |
| **Estantería** | `metal shelf`, `industrial shelving`, `warehouse shelf` | Estantería **metálica de garaje/bodega**, **vacía** (los objetos los pongo yo). 4 o 5 niveles, unos 4 m de ancho × 2,5 m de alto × 0,5 m de fondo (si es más angosta, pongo 2 lado a lado). Gris, puede estar oxidada. **No** un librero de madera ni de supermercado. |
| **Brazo robótico** | `robotic arm`, `industrial robot arm`, `robot arm claw` | Brazo **industrial de fábrica** (como los brazos KUKA de las fábricas de carros, o el robot "Dum-E" del taller de Tony Stark), con **pinza o garra** en la punta. **No** un brazo humano robótico, prótesis o brazo de cyborg. Va **colgado del techo**: sirve uno de piso, yo lo pongo al revés. Ideal **gris verdoso** (yo lo puedo pintar). Si viene rigged o por piezas separadas, mejor. ~1,5 m. |
| **Tazón de vidrio** | `glass bowl`, `mixing bowl glass` | Tazón **transparente** grande, ancho y bajo, sin tapa, ~45 cm de diámetro. **No** un bowl de cerámica opaco ni una pecera. Si no hay de vidrio, sirve uno de metal o plástico y yo lo vuelvo transparente. |
| **Matraces** (4 líquidos) | `erlenmeyer flask`, `round bottom flask`, `chemistry flask` | **Un solo matraz** de laboratorio, de vidrio y **vacío** (el líquido de color lo pongo yo). De fondo redondo o Erlenmeyer (cónico), con o sin tapón de corcho. ~20 cm. **No** un set de química completo en un solo archivo. |
| **Frascos** (4 sólidos) | `glass jar`, `jar with lid`, `apothecary jar` | **Un solo frasco** de vidrio ancho con **tapa** (tipo frasco de conservas o de boticario), vacío. ~13 cm de alto. |
| **Pistola de Portales** | `portal gun`, `rick portal gun` | La **pistola de Rick**: blanca/gris con el tubo de líquido verde arriba. Si el modelo trae el tubo como pieza separada, mejor (le pongo brillo). **No** la pistola del videojuego *Portal* (la blanca de Aperture). ~30 cm. |
| **Lavadora** | `washing machine` | Lavadora **de carga frontal** (la puerta redonda adelante), **blanca o beige**, vieja. ~85 cm de alto. |
| **Secadora** | `dryer`, `clothes dryer` | Igual que la lavadora (blanca/beige, puerta redonda). Si consiguen un modelo de **lavadora + secadora juntas**, también sirve. |
| **Fregadero** | `utility sink`, `laundry sink` | Lavadero **de pie** tipo lavandería/garaje, de una tina, **sucio** si se puede. **No** un lavaplatos de cocina con mueble. |
| **Puerta de garaje** | `garage door`, `sectional garage door` | **Solo la puerta** metálica seccional (la de paneles horizontales que sube enrollándose), **sin** la casa ni el marco de concreto. ~3,5 m × 2,5 m. Blanca o gris. |
| **Lámpara fluorescente** | `fluorescent lamp`, `tube light fixture` | Lámpara de **tubo largo colgante**, tipo taller (~1,2 m), mejor si cuelga de cadenas. **No** un bombillo normal ni una lámpara de escritorio. |
| **Caja de herramientas** | `toolbox` | Caja **metálica roja** de mano, cerrada. ~40 cm. |
| **Escoba y recogedor** | `broom`, `dustpan` | Escoba de **cerdas de paja** (vieja, tipo garaje) y un recogedor plástico. Pueden ser dos modelos separados o uno con ambos. |

---

## 2. Prioridad MEDIA (resultados de las mezclas)

| id | Qué | Buscar | Aclaración |
|---|---|---|---|
| `bola_meeseeks` | Caja de Meeseeks | `meeseeks box` | La **caja azul cilíndrica** con un **botón blanco grande** arriba (la de la serie). ~20 cm. La "de contrabando" la saco de esta misma cambiándole la textura. |
| `plumbus` | Plumbus | `plumbus` | El de la serie: rosado carne, con bulto y "tubos". Solo existe de Rick & Morty; si no hay, aviso y lo dejo como placeholder. ~20 cm. |
| `espada_zord` | Espada de Zord | `greatsword`, `power rangers sword`, `dark fantasy sword` | **Espada enorme** a dos manos, ancha, **negra** u obsidiana, idealmente con runas o grabados. **No** una katana ni una espada medieval delgada. ~1,1 m. |
| `katana` | Katana | `katana` | Katana tradicional **desenvainada** (sin funda, o con la funda como pieza aparte), empuñadura negra con detalles dorados. ~1 m. |
| `sable_luz` | Sable de luz | `lightsaber hilt` | **Solo el mango** plateado (la hoja de luz la hago yo). Si el modelo trae la hoja pegada, también sirve: la oculto. ~28 cm. |
| `cuchillo_csgo` | Cuchillo karambit | `karambit` | El cuchillo **curvo con anillo** en el mango (el de CS:GO). Ideal con camuflaje rojo ("crimson web"). ~20 cm. |
| `kunai` | Kunai | `kunai` | Cuchillo ninja **negro** con anillo atrás, ideal con cinta roja. ~25 cm. |
| `scar` | SCAR de Fortnite | `fortnite scar`, `scar assault rifle` | **Rifle** de asalto. Ideal la versión **dorada/cartoon de Fortnite**; si no hay, un SCAR normal y yo lo pinto dorado. **No** una pistola. ~70 cm. |
| `pistola_laser` | Pistola láser | `sci-fi pistol`, `raygun`, `blaster pistol` | **Pistola** de ciencia ficción de una mano, **gris metálico** con partes que brillen (turquesa). **No** un rifle. ~30 cm. |
| `guantelete` | Guantelete del Infinito | `infinity gauntlet` | El **guante dorado de Thanos** con las 6 gemas. **Solo el guante** (sin el brazo ni el personaje). Si es la versión mano abierta, mejor. |
| `token_fortnite` | Moneda V-Bucks | `v-bucks`, `gold coin` | Moneda **dorada gruesa**; ideal con la "V" grabada. Genérico ok. |
| `aguardiente` | Botella de aguardiente | `liquor bottle`, `vodka bottle` | **Botella de vidrio transparente con tapa azul**. Genérico ok (la etiqueta la pongo yo). ~25 cm. |
| `szechuan` | Salsa Szechuan | `sauce packet`, `dipping sauce cup` | **Sobrecito o vasito** de salsa de comida rápida (los de McDonald's). Genérico ok. ~8 cm. |
| `empanada` | Empanada | `empanada` | **Empanada frita** (dorada, en forma de media luna). Si solo hay de horno, sirve igual. ~15 cm. |
| `papa_rellena` | Papa rellena | `croquette`, `arancini`, `fried food ball` | **Bola frita dorada**. Genérico ok. ~12 cm. |
| `camiseta_america` | Camiseta del América de Cali | `soccer jersey`, `football shirt` | Camiseta de fútbol **suelta** (no puesta en un maniquí ni en una persona), de manga corta. Genérico ok: le pongo la textura roja del América. |
| `sr_pantalones` | Sr. Pantalones de Popó | `mr poopybutthole` | El personaje de la serie (amarillo, sombrero de copa). |
| `pepino_mutante` | Pepino mutante | `pickle`, `pickle rick` | Un **pepinillo**. Si es Pickle Rick, mejor todavía (le cambio la cara). ~25 cm. |
| `tio_steve` | Falso Tío Steve | `cute slug`, `blob creature cute` | Criatura **tierna y gelatinosa** (cruce de babosa sin caparazón con cachorro gordito), **rosada**, ojos negros grandes. Genérico ok: no existe oficial. ~40 cm. |
| `mini_cronenberg` | Mini-Cronenberg | `flesh monster`, `blob monster`, `creature eye tentacles` | **Bulto de carne** asqueroso con **un ojo grande** y tentáculos. Genérico ok. ~25 cm. |
| `meeseeks_kirkland` + NPC | Mr. Meeseeks | `mr meeseeks` | El **personaje azul** de cuerpo entero. Ideal **rigged** (para que camine con Mixamo). Lo uso para todos los Meeseeks: el azul, el rojo Kirkland y el pirata. |
| `clon_rick` | Bola de clon | `glass orb`, `crystal ball` | **Esfera de vidrio** (el feto de Rick adentro lo simulo yo). Genérico ok. ~10 cm. |
| `toallin` | Toallín | `towelie` | La toalla azul de *South Park* (con ojos). Si no hay, una toalla doblada. |
| `peluche_teto` | Peluche de Kasane Teto | `kasane teto`, `teto plush`, `fumo plush` | **Peluche cabezón** tipo "fumo", de pelo rojo con coletas. Cualquier fumo sirve si no hay de Teto. ~30 cm. |
| `dakimakura` | Dakimakura de Miku | `body pillow`, `dakimakura` | **Almohada larga** de cuerpo entero (rectangular, ~1 m). La imagen de Miku la pongo yo de textura. |
| `casco_snowball` | Casco de Snowball | `snowball helmet rick and morty`, `dog helmet sci-fi` | El casco del perro Snowball (gris, con antena). Si no hay, un **casco de sci-fi pequeño**. |
| `cereal_eyeholes` | Caja de cereal Eyeholes | `cereal box` | **Caja de cereal** rectangular cerrada. Genérico ok (la portada la pongo yo). |
| `vhs_balistica` | Cinta VHS | `vhs tape`, `vhs cassette` | Casete VHS **negro**, solo el casete (sin estuche, o con estuche aparte). |
| `bateria_microverso` | Batería de microverso | `car battery`, `sci-fi battery` | Batería grande tipo **batería de carro**. Genérico ok. |
| `semilla_sillas` | Semilla | `seed`, `acorn` | Una **semilla o bellota** grande. Genérico ok. |
| `bloque_minecraft` | Bloque de pasto | `minecraft grass block` | El **bloque de tierra con pasto** de Minecraft (cubo con textura pixelada). |
| `rick_caleno` | Rick caleño (muñeco de 15 cm) | — | **No buscar**: uso el mismo modelo de Rick, en chiquito y con accesorios (gafas, gorra). |
| `marihuana`, `semen` | Frascos | — | **No buscar**: reutilizo el frasco de los sólidos. |

---

## 3. Easter eggs

| Qué | Buscar | Aclaración |
|---|---|---|
| Casco de Iron Man | `iron man helmet` | **Solo el casco** (no el personaje entero). Va tirado bajo la mesa como chatarra: si está dañado o le falta un ojo, mejor. |
| Pokébola | `pokeball` | Pokébola **normal** (roja y blanca). Si está entreabierta o rota, mejor todavía (en la historia Rick la usó de pieza). ~8 cm. |
| Cubo Prison Realm | `prison realm`, `jujutsu kaisen cube` | El cubo de *Jujutsu Kaisen* lleno de **ojos**. Chico (~10 cm): es un pisapapeles. |
| Esfera del Dragón | `dragon ball 4 star`, `dragon ball` | **Una sola esfera** naranja, ideal la de **4 estrellas** (no las 7 juntas). |
| Figura de Miku | `hatsune miku`, `chibi miku` | **Figura chica** de Miku (cuerpo entero, puede ser chibi). ~20 cm. |
| Llama de Fortnite | `fortnite llama` | La **llama piñata** morada/azul de Fortnite. Basta la cabeza (solo se ve asomándose de una caja), pero entera también sirve. |
| **Capibara** | `capybara` | Capibara **real** (no cartoon), idealmente **acostada o echada** (está durmiendo). Las gafas VR se las pongo yo. ~1 m de largo. |
| Utilería de la estantería | `skull`, `spiked mace`, `crown`, `monster head`, `cardboard box` | Cosas sueltas y chicas: calavera, mazo con púas, corona (que parezca alienígena), **cabeza** de monstruo verde con dientes (solo la cabeza) y una caja de cartón cerrada. Genérico ok. |
| Bandera de Colombia | — | **No buscar**: es una calcomanía (textura). |

---

## 4. Exterior

| Qué | Buscar | Aclaración |
|---|---|---|
| Casa Smith (2 pisos) | `suburban house`, `american house low poly` | Casa **americana de suburbio** de **2 pisos**, paredes blancas, techo de tejas marrón. **Solo el exterior** (no hace falta interior). Si viene con **garaje integrado** a un costado, perfecto; si no, igual sirve. Low poly. |
| Casa del vecino (1 piso) | `bungalow`, `small house low poly`, `ranch house` | Casa **de un piso**, exterior solamente, ojalá con **ventanas grandes al frente** (son diana para lanzar cosas). El color rosado se lo pongo yo. |
| Valla blanca | `white picket fence` | Valla **de estacas blancas** (la clásica de suburbio), en un **tramo modular** que yo repito. Ideal que incluya también la puertita. |
| Camioneta del FBI | `black van`, `swat van`, `cargo van` | **Van negra** cerrada (tipo furgoneta), sin calcomanías raras. Genérico ok: el "FBI" lo pongo yo. |
| Barricada | `road barricade`, `traffic barrier` | Barricada de calle **blanca y naranja**, a rayas. |
| Cono | `traffic cone` | Cono de tránsito naranja normal. |
| Aro de baloncesto | `basketball hoop` | Aro con **tablero blanco** para colgar **en la pared** (no el de poste en el piso). |
| Casas de fondo (opcional) | `low poly house pack`, `low poly suburban houses` | Casas **muy simples** para el horizonte. **Aquí sí sirve un pack** con varias casas. |

---

## Mixamo (animaciones)
1. Subir a Mixamo el modelo de **Rick** (y el del Meeseeks si lo consiguen).
2. Bajar: Rick → **Idle**, **Talking** y **Walking**. Meeseeks → **Walking** y **Excited**.
3. Formato: **FBX for Unity**. La primera animación **with skin**, las demás **without skin**.

## Cómo pasármelos
Dejen cada modelo en su carpeta, por ejemplo `Assets/_RickMorty/Models/rick/` o `Assets/_RickMorty/Models/katana/`, y me avisan. Yo los conecto (escala, colisiones, registro y reemplazo en la escena). Si en algún caso dudan entre dos modelos, bajen ambos y elijo yo.
