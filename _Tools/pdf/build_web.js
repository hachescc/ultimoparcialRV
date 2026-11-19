// Genera la version web interactiva del recorrido:
//  - Documentacion/RickMorty_Recorrido_web.html  (copia local completa, abrir con doble clic junto a la carpeta capturas/)
//  - <out>/index.html                            (cuerpo para publicar como Artifact, sin <html>/<head>)
// Uso: node _Tools/pdf/build_web.js [carpeta_salida_artifact]
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..', '..');
const doc = path.join(root, 'Documentacion');
const capDir = path.join(doc, 'capturas');
const dataDir = path.join(root, 'Assets', '_RickMorty', 'Resources', 'Data');
const outArtifact = process.argv[2];

const manifest = JSON.parse(fs.readFileSync(path.join(capDir, 'manifest.json'), 'utf8'));
const items = JSON.parse(fs.readFileSync(path.join(dataDir, 'items.json'), 'utf8')).items;
const recipes = JSON.parse(fs.readFileSync(path.join(dataDir, 'recipes.json'), 'utf8'));

const SKIP = new Set(['Meeseeks de contrabando', 'Rick Caleno + camiseta del America', 'Rick Caleño + camiseta del América', 'Cubo Prison Realm']);
const shots = manifest.shots.filter(s => !s.file.startsWith('item_') && !SKIP.has(s.title));
const sections = [];
for (const s of shots) {
  const name = s.section.replace(/^\d+\.\s*/, '');
  let sec = sections.find(x => x.name === name);
  if (!sec) { sec = { name, shots: [] }; sections.push(sec); }
  sec.shots.push({ f: s.file, t: s.title, c: s.caption });
}
const has = id => fs.existsSync(path.join(capDir, `item_${id}.jpg`));
const DATA = {
  sections,
  items: items.map(i => ({ id: i.id, n: i.name, cat: i.category, d: i.description, img: has(i.id) ? `capturas/item_${i.id}.jpg` : null })),
  recipes: recipes.recipes.map(r => ({ r: r.result, i: r.ingredients })),
  final: { r: recipes.finalRecipe.result, i: recipes.finalRecipe.ingredients },
};
const usedFiles = [...shots.map(s => s.file), ...DATA.items.filter(i => i.img).map(i => path.basename(i.img))];

const body = `<title>Rick y Morty VR: el recorrido</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Chakra+Petch:wght@500;700&family=Instrument+Sans:wght@400;500;600&family=JetBrains+Mono:wght@400;600&display=swap">
<style>
/* Layout: banda de titulo sobre papel de plano (blueprint de Rick) → visor del recorrido → mesa de mezclas → recetas → objetos */
:root {
  --paper: #eef3f6; --sheet: #ffffff; --ink: #14304f; --muted: #5a6f86; --line: #c9d6e2;
  --portal: #2f9e3a; --portal-soft: #def3dc; --tape: #f0b400; --boom: #d2452d; --made: #fff1d0; --made-line: #e7c36a;
  --grid: rgba(20, 48, 79, .06);
  --display: "Chakra Petch", "Segoe UI", system-ui, sans-serif;
  --body: "Instrument Sans", "Segoe UI", system-ui, sans-serif;
  --mono: "JetBrains Mono", Consolas, monospace;
}
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) {
  --paper: #0d1f36; --sheet: #13294a; --ink: #e3edf7; --muted: #9db2c9; --line: #26446b;
  --portal: #6fdc6a; --portal-soft: #183f2a; --tape: #ffc933; --boom: #ff7a5c; --made: #3a3018; --made-line: #8a7230;
  --grid: rgba(160, 200, 240, .07); color-scheme: dark; } }
:root[data-theme="dark"] {
  --paper: #0d1f36; --sheet: #13294a; --ink: #e3edf7; --muted: #9db2c9; --line: #26446b;
  --portal: #6fdc6a; --portal-soft: #183f2a; --tape: #ffc933; --boom: #ff7a5c; --made: #3a3018; --made-line: #8a7230;
  --grid: rgba(160, 200, 240, .07); color-scheme: dark; }
* { box-sizing: border-box; }
body { background: var(--paper); color: var(--ink); font-family: var(--body); font-size: 15px; line-height: 1.5;
  background-image: linear-gradient(var(--grid) 1px, transparent 1px), linear-gradient(90deg, var(--grid) 1px, transparent 1px);
  background-size: 28px 28px; }
.wrap { max-width: 1180px; margin: 0 auto; padding-block: 28px 64px;
  padding-left: max(16px, env(safe-area-inset-left, 0px)); padding-right: max(16px, env(safe-area-inset-right, 0px)); }
h1, h2, h3 { font-family: var(--display); margin: 0; text-wrap: balance; letter-spacing: .01em; }
h2 { font-size: 26px; display: flex; align-items: baseline; gap: 12px; }
h2 small { font-family: var(--mono); font-size: 12px; color: var(--muted); font-weight: 400; letter-spacing: .04em; }
section { margin-top: 44px; display: grid; gap: 16px; }
p { margin: 0; }
.lede { color: var(--muted); max-width: 70ch; }
button { font: inherit; color: inherit; cursor: pointer; }
:focus-visible { outline: 2px solid var(--portal); outline-offset: 2px; }

/* Encabezado */
header.top { display: grid; gap: 14px; }
.tape { display: inline-flex; width: fit-content; background: var(--tape); color: #1d1a0c; font-family: var(--mono); font-size: 11.5px;
  font-weight: 600; letter-spacing: .12em; text-transform: uppercase; padding: 4px 10px; transform: rotate(-1.2deg); }
h1 { font-size: clamp(34px, 6vw, 58px); line-height: .98; }
h1 em { font-style: normal; color: var(--portal); }
.facts { display: flex; flex-wrap: wrap; gap: 8px 18px; font-family: var(--mono); font-size: 13px; color: var(--muted); }
.facts b { color: var(--ink); font-weight: 600; }

/* Pasos de la partida */
.steps { display: grid; grid-template-columns: repeat(auto-fit, minmax(128px, 1fr)); gap: 8px; }
.step { text-align: left; background: var(--sheet); border: 1px solid var(--line); border-radius: 8px; padding: 10px 12px; display: grid; gap: 2px; }
.step span { font-family: var(--mono); font-size: 11px; color: var(--muted); }
.step b { font-family: var(--display); font-size: 15px; }
.step:hover { border-color: var(--portal); }

/* Visor del recorrido */
.viewer { background: var(--sheet); border: 1px solid var(--line); border-radius: 12px; overflow: hidden; display: grid; }
.tabs { display: flex; gap: 6px; overflow-x: auto; padding: 10px; border-bottom: 1px solid var(--line); scrollbar-width: thin; }
.tab { flex: none; border: 1px solid var(--line); background: transparent; border-radius: 999px; padding: 5px 12px; font-size: 13px; white-space: nowrap; }
.tab[aria-selected="true"] { background: var(--portal); border-color: var(--portal); color: #fff; }
:root[data-theme="dark"] .tab[aria-selected="true"] { color: #062010; }
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) .tab[aria-selected="true"] { color: #062010; } }
.stage { position: relative; background: #0a1220; aspect-ratio: 16 / 9; max-width: 100%; }
.stage img { width: 100%; height: 100%; object-fit: contain; display: block; cursor: zoom-in; }
.nav { position: absolute; top: 50%; transform: translateY(-50%); width: 44px; height: 44px; border-radius: 50%; border: 0;
  background: rgba(10, 18, 32, .65); color: #fff; font-size: 22px; display: grid; place-items: center; }
.nav.prev { left: 10px; } .nav.next { right: 10px; }
.counter { position: absolute; top: 10px; right: 10px; font-family: var(--mono); font-size: 12px; background: rgba(10, 18, 32, .65);
  color: #fff; padding: 3px 8px; border-radius: 6px; font-variant-numeric: tabular-nums; }
.caption { padding: 14px 16px 6px; display: grid; gap: 4px; min-height: 92px; }
.caption h3 { font-size: 19px; }
.caption p { color: var(--muted); max-width: 90ch; }
.strip { display: flex; gap: 6px; overflow-x: auto; padding: 8px 10px 12px; scrollbar-width: thin; }
.strip button { flex: none; border: 2px solid transparent; border-radius: 6px; padding: 0; width: 104px; aspect-ratio: 16 / 9; overflow: hidden; background: none; }
.strip button[aria-current="true"] { border-color: var(--portal); }
.strip img { width: 100%; height: 100%; object-fit: cover; display: block; }

/* Mesa de mezclas */
.lab { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(0, 1fr); gap: 16px; }
@media (max-width: 820px) { .lab { grid-template-columns: 1fr; } }
.panel { background: var(--sheet); border: 1px solid var(--line); border-radius: 12px; padding: 14px; display: grid; gap: 12px; align-content: start; min-width: 0; }
.panel h3 { font-size: 15px; font-family: var(--mono); font-weight: 600; text-transform: uppercase; letter-spacing: .08em; color: var(--muted); }
.pick { display: flex; flex-wrap: wrap; gap: 6px; }
.chip { border: 1px solid var(--line); background: var(--paper); border-radius: 999px; padding: 4px 10px 4px 4px; font-size: 13px;
  display: inline-flex; align-items: center; gap: 6px; }
.chip img { width: 22px; height: 22px; border-radius: 50%; object-fit: cover; }
.chip.base { border-color: color-mix(in srgb, var(--portal) 45%, var(--line)); }
.chip.made { background: var(--made); border-color: var(--made-line); }
.chip[aria-pressed="true"] { outline: 2px solid var(--portal); background: var(--portal-soft); }
.bowl { display: grid; gap: 12px; justify-items: center; text-align: center; }
.slots { display: flex; gap: 8px; flex-wrap: wrap; justify-content: center; min-height: 64px; align-items: center; }
.slot { width: 60px; height: 60px; border-radius: 50%; border: 2px dashed var(--line); display: grid; place-items: center; overflow: hidden; color: var(--muted); font-size: 12px; }
.slot img { width: 100%; height: 100%; object-fit: cover; }
.mixbtn { background: var(--boom); color: #fff; border: 0; border-radius: 999px; padding: 10px 26px; font-family: var(--display); font-size: 17px; letter-spacing: .08em;
  box-shadow: 0 3px 0 color-mix(in srgb, var(--boom) 60%, #000); }
.mixbtn:active { transform: translateY(2px); box-shadow: 0 1px 0 color-mix(in srgb, var(--boom) 60%, #000); }
.mixbtn:disabled { opacity: .45; cursor: default; }
.linkbtn { border: 0; background: none; color: var(--muted); text-decoration: underline; font-size: 13px; }
.result { width: 100%; border-radius: 10px; padding: 12px; display: grid; grid-template-columns: 84px minmax(0, 1fr); gap: 12px; align-items: center; text-align: left; }
.result > img { width: 84px; height: 84px; border-radius: 8px; object-fit: cover; }
.result.ok { background: var(--portal-soft); }
.result.boom { background: color-mix(in srgb, var(--boom) 16%, var(--sheet)); grid-template-columns: 1fr; }
.result.info { background: var(--paper); grid-template-columns: 1fr; }
.result b { font-family: var(--display); font-size: 18px; display: block; }
.result span { color: var(--muted); font-size: 13.5px; }
@keyframes pop { from { transform: scale(.94); opacity: .4; } to { transform: none; opacity: 1; } }
.result { animation: pop .25s ease-out; }
@media (prefers-reduced-motion: reduce) { .result { animation: none; } }

/* Recetas */
.toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.search { flex: 1 1 240px; border: 1px solid var(--line); background: var(--sheet); color: var(--ink); border-radius: 8px; padding: 9px 12px; font: inherit; }
.hint { font-size: 13px; color: var(--muted); }
.recipes { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 8px; }
.rcp { background: var(--sheet); border: 1px solid var(--line); border-radius: 10px; padding: 10px; display: grid; grid-template-columns: 56px minmax(0, 1fr); gap: 10px; align-items: center; }
.rcp > img { width: 56px; height: 56px; border-radius: 8px; object-fit: cover; }
.rcp b { font-family: var(--display); font-size: 15.5px; display: block; }
.rcp .ing { display: flex; flex-wrap: wrap; gap: 4px; align-items: center; margin-top: 4px; font-size: 12px; color: var(--muted); }
.rcp .ing .chip { padding: 2px 8px; font-size: 12px; }
.rcp.final { grid-column: 1 / -1; background: var(--portal-soft); border-color: var(--portal); }
.empty { color: var(--muted); padding: 10px 2px; }

/* Objetos */
.filters { display: flex; flex-wrap: wrap; gap: 6px; }
.gallery { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 10px; }
.obj { background: var(--sheet); border: 1px solid var(--line); border-radius: 10px; padding: 0; overflow: hidden; text-align: left; display: grid; }
.obj img { width: 100%; aspect-ratio: 1; object-fit: cover; display: block; }
.obj div { padding: 8px 10px 10px; display: grid; gap: 2px; }
.obj b { font-size: 13.5px; line-height: 1.25; }
.obj span { font-family: var(--mono); font-size: 11px; color: var(--muted); text-transform: uppercase; letter-spacing: .06em; }
.obj:hover { border-color: var(--portal); }

/* Controles */
.cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 12px; }
.card { background: var(--sheet); border: 1px solid var(--line); border-radius: 12px; padding: 14px 16px; display: grid; gap: 8px; align-content: start; }
.card h3 { font-size: 17px; }
.card ul { margin: 0; padding-left: 18px; display: grid; gap: 4px; }
kbd { font-family: var(--mono); font-size: 12px; background: var(--paper); border: 1px solid var(--line); border-radius: 4px; padding: 0 5px; }

/* Lightbox */
.lightbox { position: fixed; inset: 0; background: rgba(4, 10, 20, .9); display: grid; place-items: center; z-index: 50;
  padding: max(16px, env(safe-area-inset-top, 0px)) max(16px, env(safe-area-inset-right, 0px)) max(16px, env(safe-area-inset-bottom, 0px)) max(16px, env(safe-area-inset-left, 0px)); }
.lightbox figure { margin: 0; max-width: 1100px; width: 100%; display: grid; gap: 10px; color: #e8f0f8; }
.lightbox img { width: 100%; max-height: 78vh; object-fit: contain; border-radius: 8px; background: #0a1220; }
.lightbox figcaption b { font-family: var(--display); font-size: 20px; display: block; }
.lightbox .close { position: absolute; top: max(14px, env(safe-area-inset-top, 0px)); right: max(14px, env(safe-area-inset-right, 0px));
  border: 0; background: rgba(255, 255, 255, .12); color: #fff; width: 40px; height: 40px; border-radius: 50%; font-size: 20px; }
footer { margin-top: 48px; color: var(--muted); font-size: 13px; }
</style>

<div class="wrap">
  <header class="top">
    <span class="tape">Proyecto final · Realidad Virtual 2026 · Meta Quest</span>
    <h1>Rick &amp; Morty<br>y las mezclas de <em>otra dimension</em></h1>
    <p class="lede">Morty despierta en el garaje, Rick le ensena a usar la mesa de mezclas y se va. Hay que fabricar una Pistola de Portales real
      antes de que el FBI entre a la casa. Todo lo que ves aca son capturas del juego corriendo.</p>
    <div class="facts">
      <span><b>${shots.length}</b> capturas</span><span><b>${DATA.recipes.length + 1}</b> recetas</span>
      <span><b>${DATA.items.length}</b> objetos</span><span><b>15 min</b> antes de que entre el FBI</span><span><b>8</b> easter eggs</span>
    </div>
  </header>

  <section aria-labelledby="h-steps">
    <h2 id="h-steps">La partida <small>toca un paso para verlo</small></h2>
    <div class="steps" id="steps"></div>
  </section>

  <section aria-labelledby="h-tour">
    <h2 id="h-tour">El recorrido <small>flechas ← → del teclado</small></h2>
    <div class="viewer">
      <div class="tabs" role="tablist" id="tabs"></div>
      <div class="stage">
        <img id="shot" alt="">
        <button class="nav prev" id="prev" aria-label="Captura anterior">‹</button>
        <button class="nav next" id="next" aria-label="Captura siguiente">›</button>
        <span class="counter" id="counter"></span>
      </div>
      <div class="caption"><h3 id="shot-title"></h3><p id="shot-caption"></p></div>
      <div class="strip" id="strip"></div>
    </div>
  </section>

  <section aria-labelledby="h-lab">
    <h2 id="h-lab">Mesa de mezclas <small>proba una combinacion</small></h2>
    <p class="lede">Elegi 2 o 3 cosas y presiona MEZCLAR, igual que en el juego. Si la combinacion no existe, explota.</p>
    <div class="lab">
      <div class="panel">
        <h3>Ingredientes base</h3><div class="pick" id="pick-base"></div>
        <h3>Cosas que ya fabricaste</h3><div class="pick" id="pick-made"></div>
      </div>
      <div class="panel bowl">
        <h3>Tazon</h3>
        <div class="slots" id="slots"></div>
        <button class="mixbtn" id="mix" disabled>MEZCLAR</button>
        <button class="linkbtn" id="clear">Vaciar el tazon</button>
        <div id="result" class="result info"><span>El tazon esta vacio. Una que funciona: Acido Galaxiano + Polvo de Plumbus.</span></div>
      </div>
    </div>
  </section>

  <section aria-labelledby="h-recipes">
    <h2 id="h-recipes">Recetas <small id="rcount"></small></h2>
    <div class="toolbar">
      <input class="search" id="q" type="search" placeholder="Buscar objeto o ingrediente (ej. plumbus, katana, acido)" aria-label="Buscar recetas">
      <span class="hint">Toca un ingrediente para ver todo lo que se hace con el.</span>
    </div>
    <div class="recipes" id="recipes"></div>
  </section>

  <section aria-labelledby="h-items">
    <h2 id="h-items">Objetos</h2>
    <div class="filters" id="filters"></div>
    <div class="gallery" id="gallery"></div>
  </section>

  <section aria-labelledby="h-ctrl">
    <h2 id="h-ctrl">Como se juega</h2>
    <div class="cards">
      <div class="card"><h3>Controles del Quest</h3><ul>
        <li><b>Joystick izquierdo:</b> caminar</li><li><b>Joystick derecho:</b> girar (por pasos o suave)</li>
        <li><b>Grip:</b> agarrar, soltar y lanzar</li><li><b>Gatillo:</b> usar el objeto que tenes en la mano</li>
        <li><b>Menu (izquierdo):</b> pausa y opciones de confort</li><li>Para <b>comer</b> o <b>ponerte</b> algo, acercalo a la boca o a la cabeza</li></ul></div>
      <div class="card"><h3>Puntaje</h3><ul>
        <li>+100 por cada objeto nuevo, +150 por cada mision de Rick</li><li>+1000 por la Pistola de Portales y bono por tiempo sobrante</li>
        <li>+50 por easter egg</li><li>-50 por explosion, -100 por salir de la cinta del FBI, -75 por el abrazo del Tio Steve</li>
        <li>Rango final S, A, B, C o D, guardado en el historial</li></ul></div>
      <div class="card"><h3>Sin inventario</h3><p>Solo cargas lo que te cabe en las dos manos. Lo que vas descubriendo queda en el holograma del reloj
        (mirando la muneca izquierda), que tambien muestra la receta de cada objeto ya descubierto.</p>
        <p>Si se te cae un frasco base no pasa nada: vuelve solo a la mesa por un micro-portal.</p></div>
    </div>
  </section>

  <footer>Modelos provisionales: algunos de Sketchfab y otros generados por codigo. Voces y sonidos todavia de prueba. Todavia no se probo en el visor.</footer>
</div>

<div class="lightbox" id="lightbox" hidden>
  <button class="close" id="lb-close" aria-label="Cerrar">✕</button>
  <figure><img id="lb-img" alt=""><figcaption><b id="lb-title"></b><span id="lb-text"></span></figcaption></figure>
</div>

<script>
const DATA = ${JSON.stringify(DATA)};
const $ = id => document.getElementById(id);
const byId = Object.fromEntries(DATA.items.map(i => [i.id, i]));
const isBase = id => (byId[id]?.cat || '').startsWith('base');
const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const chip = (id, attrs = '') => '<button class="chip ' + (isBase(id) ? 'base' : 'made') + '" data-id="' + id + '" ' + attrs + '>' +
  (byId[id]?.img ? '<img src="' + byId[id].img + '" alt="">' : '') + esc(byId[id]?.n || id) + '</button>';

// ---------- Visor ----------
let sec = 0, idx = 0;
const STEPS = [
  ['Intro', 'Rick despierta a Morty', 'Inicio'], ['Onboarding', 'Reloj, caminar, girar, agarrar', 'Onboarding VR'],
  ['Tutorial', 'Las 3 mezclas de Rick', 'Tutorial de Rick (3 mezclas)'], ['Rick se va', 'Se abre el garaje', 'Rick se va'],
  ['Juego libre', '15 min, recetas y calle', 'Juego libre: UI'], ['Caos', 'Criaturas y eventos', 'Criaturas'],
  ['Final', 'Pistola de Portales', 'Final: la Pistola de Portales'], ['Otro final', 'Si se acaba el tiempo', 'Otro final: se acaba el tiempo']];
$('steps').innerHTML = STEPS.map((s, i) => '<button class="step" data-sec="' + esc(s[2]) + '"><span>Paso ' + (i + 1) + '</span><b>' + esc(s[0]) + '</b><span>' + esc(s[1]) + '</span></button>').join('');
$('steps').onclick = e => {
  const b = e.target.closest('.step'); if (!b) return;
  const i = DATA.sections.findIndex(s => s.name === b.dataset.sec);
  if (i >= 0) { show(i, 0); $('h-tour').scrollIntoView({ behavior: 'smooth', block: 'start' }); }
};
$('tabs').innerHTML = DATA.sections.map((s, i) => '<button class="tab" role="tab" data-i="' + i + '">' + esc(s.name) + '</button>').join('');
$('tabs').onclick = e => { const b = e.target.closest('.tab'); if (b) show(+b.dataset.i, 0); };
function show(s, i) {
  const list = DATA.sections[s].shots;
  if (i < 0) { if (s > 0) return show(s - 1, DATA.sections[s - 1].shots.length - 1); i = 0; }
  if (i >= list.length) { if (s < DATA.sections.length - 1) return show(s + 1, 0); i = list.length - 1; }
  const changed = s !== sec || !$('strip').children.length;
  sec = s; idx = i;
  const sh = list[i];
  $('shot').src = 'capturas/' + sh.f; $('shot').alt = sh.t;
  $('shot-title').textContent = sh.t; $('shot-caption').textContent = sh.c;
  let before = 0; for (let k = 0; k < s; k++) before += DATA.sections[k].shots.length;
  $('counter').textContent = (before + i + 1) + ' / ' + ${shots.length};
  [...$('tabs').children].forEach((t, k) => t.setAttribute('aria-selected', k === s));
  if (changed) {
    $('strip').innerHTML = list.map((x, k) => '<button data-k="' + k + '" aria-label="' + esc(x.t) + '"><img loading="lazy" src="capturas/' + x.f + '" alt=""></button>').join('');
    $('tabs').children[s].scrollIntoView({ block: 'nearest', inline: 'nearest' });
  }
  [...$('strip').children].forEach((b, k) => b.setAttribute('aria-current', k === i));
  $('strip').children[i]?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
  const next = list[i + 1] || DATA.sections[s + 1]?.shots[0];
  if (next) new Image().src = 'capturas/' + next.f;
}
$('strip').onclick = e => { const b = e.target.closest('button'); if (b) show(sec, +b.dataset.k); };
$('prev').onclick = () => show(sec, idx - 1);
$('next').onclick = () => show(sec, idx + 1);
document.addEventListener('keydown', e => {
  if (e.target.matches('input')) return;
  if (!$('lightbox').hidden && e.key === 'Escape') return closeLb();
  if (e.key === 'ArrowRight') show(sec, idx + 1);
  if (e.key === 'ArrowLeft') show(sec, idx - 1);
});
$('shot').onclick = () => { const sh = DATA.sections[sec].shots[idx]; openLb('capturas/' + sh.f, sh.t, sh.c); };
show(0, 1);

// ---------- Lightbox ----------
function openLb(src, t, c) { $('lb-img').src = src; $('lb-title').textContent = t; $('lb-text').textContent = c || ''; $('lightbox').hidden = false; }
function closeLb() { $('lightbox').hidden = true; }
$('lb-close').onclick = closeLb;
$('lightbox').onclick = e => { if (e.target.id === 'lightbox') closeLb(); };

// ---------- Mesa de mezclas ----------
const key = ids => ids.slice().sort().join('+');
const RECIPE = Object.fromEntries(DATA.recipes.map(r => [key(r.i), r.r]));
const madeIds = DATA.items.filter(i => !i.cat.startsWith('base') && i.cat !== 'final').map(i => i.id);
$('pick-base').innerHTML = DATA.items.filter(i => i.cat.startsWith('base')).map(i => chip(i.id, 'aria-pressed="false"')).join('');
$('pick-made').innerHTML = madeIds.map(id => chip(id, 'aria-pressed="false"')).join('');
let bowl = [];
function renderBowl() {
  $('slots').innerHTML = [0, 1, 2].map(k => { const id = bowl[k]; return '<div class="slot">' + (id ? (byId[id].img ? '<img src="' + byId[id].img + '" alt="' + esc(byId[id].n) + '">' : esc(byId[id].n)) : (k < 2 ? '+' : 'opcional')) + '</div>'; }).join('');
  $('mix').disabled = bowl.length < 2;
  document.querySelectorAll('#pick-base .chip, #pick-made .chip').forEach(c => c.setAttribute('aria-pressed', bowl.includes(c.dataset.id)));
}
function pickHandler(e) {
  const c = e.target.closest('.chip'); if (!c) return;
  const id = c.dataset.id, at = bowl.indexOf(id);
  if (at >= 0) bowl.splice(at, 1); else if (bowl.length < 3) bowl.push(id);
  renderBowl();
}
$('pick-base').onclick = pickHandler; $('pick-made').onclick = pickHandler;
$('clear').onclick = () => { bowl = []; renderBowl(); };
$('mix').onclick = () => {
  const r = $('result');
  if (key(bowl) === key(DATA.final.i)) {
    r.className = 'result info'; r.innerHTML = '<div><b>Eso no va en el tazon</b><span>Es la receta final: la Portal Gun de carton se pone en el Soporte de Ensamblaje y se le acercan los Cristales Kalaxianos y el Aguardiente. Sale la Pistola de Portales real.</span></div>';
  } else if (RECIPE[key(bowl)]) {
    const it = byId[RECIPE[key(bowl)]];
    r.className = 'result ok'; r.innerHTML = (it.img ? '<img src="' + it.img + '" alt="">' : '') + '<div><b>' + esc(it.n) + '</b><span>' + esc(it.d) + '</span></div>';
  } else {
    r.className = 'result boom'; r.innerHTML = '<div><b>¡BOOM!</b><span>Esa combinacion no existe: destello blanco, humo negro, zumbido en los oidos y -50 puntos. Los frascos base vuelven solos a la mesa.</span></div>';
  }
  bowl = []; renderBowl();
};
renderBowl();

// ---------- Recetas ----------
let ingFilter = null;
function renderRecipes() {
  const q = $('q').value.trim().toLowerCase();
  const norm = s => s.toLowerCase().normalize('NFD').replace(/[\\u0300-\\u036f]/g, '');
  const match = r => {
    if (ingFilter && !r.i.includes(ingFilter)) return false;
    if (!q) return true;
    return [r.r, ...r.i].some(id => norm(byId[id]?.n || id).includes(norm(q)));
  };
  const list = DATA.recipes.filter(match);
  const finalShown = match(DATA.final);
  const card = (r, extra = '') => '<div class="rcp ' + extra + '">' + (byId[r.r]?.img ? '<img loading="lazy" src="' + byId[r.r].img + '" alt="">' : '<span></span>') +
    '<div><b>' + esc(byId[r.r]?.n) + '</b><div class="ing">' + r.i.map(id => chip(id)).join('<span>+</span>') + '</div></div></div>';
  $('recipes').innerHTML = (finalShown ? card(DATA.final, 'final') : '') + list.map(r => card(r)).join('') ||
    '<p class="empty">Ninguna receta coincide. Proba con otro nombre.</p>';
  $('rcount').textContent = ingFilter ? 'que usan ' + byId[ingFilter].n + ' · toca de nuevo para quitar el filtro' : (list.length + (finalShown ? 1 : 0)) + ' de ' + (DATA.recipes.length + 1);
}
$('q').oninput = renderRecipes;
$('recipes').onclick = e => { const c = e.target.closest('.chip'); if (!c) return; ingFilter = ingFilter === c.dataset.id ? null : c.dataset.id; renderRecipes(); };
renderRecipes();

// ---------- Objetos ----------
const CATS = [['todo', 'Todo'], ['base', 'Ingredientes'], ['arma', 'Armas'], ['criatura', 'Criaturas'], ['comida', 'Comida'], ['absurdo', 'Absurdos'], ['final', 'Final']];
let cat = 'todo';
$('filters').innerHTML = CATS.map(c => '<button class="tab" data-c="' + c[0] + '">' + c[1] + '</button>').join('');
function renderGallery() {
  [...$('filters').children].forEach(b => b.setAttribute('aria-selected', b.dataset.c === cat));
  const list = DATA.items.filter(i => i.img && (cat === 'todo' || (cat === 'base' ? i.cat.startsWith('base') : i.cat === cat)));
  $('gallery').innerHTML = list.map(i => '<button class="obj" data-id="' + i.id + '"><img loading="lazy" src="' + i.img + '" alt=""><div><b>' + esc(i.n) + '</b><span>' +
    esc(i.cat.startsWith('base') ? 'ingrediente' : i.cat) + '</span></div></button>').join('');
}
$('filters').onclick = e => { const b = e.target.closest('.tab'); if (b) { cat = b.dataset.c; renderGallery(); } };
$('gallery').onclick = e => {
  const b = e.target.closest('.obj'); if (!b) return;
  const i = byId[b.dataset.id];
  const rec = DATA.recipes.find(r => r.r === i.id) || (DATA.final.r === i.id ? DATA.final : null);
  openLb(i.img, i.n, i.d + (rec ? '  ·  Receta: ' + rec.i.map(x => byId[x].n).join(' + ') : '  ·  Ingrediente base: siempre esta en la mesa.'));
};
renderGallery();
</script>
`;

const full = `<!doctype html>\n<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<style>:root{color-scheme:light;padding-top:env(safe-area-inset-top,0px);padding-bottom:env(safe-area-inset-bottom,0px)}body{margin:0;font:14px system-ui}img{max-width:100%}[hidden]{display:none!important}</style>
</head><body>\n${body}\n</body></html>\n`;
fs.writeFileSync(path.join(doc, 'RickMorty_Recorrido_web.html'), full);
if (outArtifact) {
  fs.mkdirSync(outArtifact, { recursive: true });
  fs.writeFileSync(path.join(outArtifact, 'index.html'), body);
  fs.writeFileSync(path.join(outArtifact, 'files.json'), JSON.stringify(usedFiles));
}
console.log('OK', shots.length, 'capturas,', sections.length, 'secciones,', usedFiles.length, 'imagenes usadas');
