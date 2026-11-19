// Arma Documentacion/RickMorty_Recorrido.html a partir de las capturas del recorrido
// (Documentacion/capturas/manifest.json) y los datos del juego (items/recetas JSON).
// Luego se imprime a PDF con Edge headless (ver build_pdf.ps1).
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..', '..');
const doc = path.join(root, 'Documentacion');
const capDir = path.join(doc, 'capturas');
const data = path.join(root, 'Assets', '_RickMorty', 'Resources', 'Data');

const manifest = JSON.parse(fs.readFileSync(path.join(capDir, 'manifest.json'), 'utf8'));
const items = JSON.parse(fs.readFileSync(path.join(data, 'items.json'), 'utf8')).items;
const recipes = JSON.parse(fs.readFileSync(path.join(data, 'recipes.json'), 'utf8'));
const byId = Object.fromEntries(items.map(i => [i.id, i]));

// Capturas que quedaron tapadas por otros objetos: no van al PDF
const SKIP = new Set(['Meeseeks de contrabando', 'Rick Caleno + camiseta del America', 'Rick Caleño + camiseta del América', 'Cubo Prison Realm']);

const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const name = id => byId[id] ? byId[id].name : id;
const thumb = id => fs.existsSync(path.join(capDir, `item_${id}.jpg`)) ? `capturas/item_${id}.jpg` : null;

const shots = manifest.shots.filter(s => !s.file.startsWith('item_') && !SKIP.has(s.title));
const sections = [];
for (const s of shots) {
  let sec = sections.find(x => x.name === s.section);
  if (!sec) { sec = { name: s.section, shots: [] }; sections.push(sec); }
  sec.shots.push(s);
}

const catLabel = {
  base_liquido: 'Ingrediente base (liquido)', base_solido: 'Ingrediente base (solido)', criatura: 'Criatura',
  arma: 'Arma', comida: 'Comida', absurdo: 'Objeto absurdo', final: 'Objeto final'
};

function shotCards(list) {
  let html = '';
  for (let i = 0; i < list.length; i += 4) {
    html += '<div class="grid">' + list.slice(i, i + 4).map(s => `
      <figure>
        <img src="capturas/${s.file}">
        <figcaption><b>${esc(s.title)}</b>${esc(s.caption)}</figcaption>
      </figure>`).join('') + '</div>';
  }
  return html;
}

const recipeRows = recipes.recipes.map((r, i) => {
  const t = thumb(r.result);
  return `<tr>
    <td class="n">${i + 1}</td>
    <td class="th">${t ? `<img src="${t}">` : ''}</td>
    <td><b>${esc(name(r.result))}</b><div class="desc">${esc(byId[r.result]?.description)}</div></td>
    <td class="ing">${r.ingredients.map(id => `<span class="chip ${byId[id]?.category?.startsWith('base') ? 'base' : 'made'}">${esc(name(id))}</span>`).join(' + ')}</td>
  </tr>`;
}).join('');

const fin = recipes.finalRecipe;
const baseItems = items.filter(i => i.category.startsWith('base'));
const galleryItems = items.filter(i => thumb(i.id));

const html = `<!doctype html>
<html lang="es"><head><meta charset="utf-8">
<title>Rick & Morty VR - Recorrido del juego</title>
<style>
  @page { size: A4 landscape; margin: 11mm 12mm; }
  * { box-sizing: border-box; }
  body { font-family: 'Segoe UI', Arial, sans-serif; color: #1d2320; margin: 0; font-size: 10.5pt; }
  h1, h2, h3 { margin: 0; }
  .page { page-break-after: always; }
  .cover { height: 186mm; display: flex; flex-direction: column; justify-content: center; position: relative;
           background: #0b1a10 url('capturas/${shots.find(s => s.title === 'Vista general del garaje')?.file}') center/cover; color: #fff; padding: 18mm; border-radius: 4mm; overflow: hidden; }
  .cover::before { content: ''; position: absolute; inset: 0; background: linear-gradient(90deg, rgba(4,14,8,.93) 0%, rgba(4,14,8,.75) 55%, rgba(4,14,8,.25) 100%); }
  .cover > * { position: relative; }
  .cover h1 { font-size: 34pt; color: #7dff6b; line-height: 1.05; }
  .cover h2 { font-size: 16pt; font-weight: 400; margin-top: 3mm; color: #d8ffd2; }
  .cover p { max-width: 150mm; font-size: 11pt; line-height: 1.5; margin-top: 8mm; color: #e9f5e7; }
  .cover .meta { margin-top: 10mm; font-size: 9.5pt; color: #a9d6a2; }
  h2.sec { font-size: 17pt; color: #1a6b2c; border-bottom: 2.5px solid #57d65a; padding-bottom: 2mm; margin-bottom: 4mm; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 4mm 6mm; page-break-inside: avoid; margin-bottom: 4mm; }
  figure { margin: 0; page-break-inside: avoid; }
  figure img { width: 100%; border-radius: 2mm; display: block; border: 1px solid #c9d3cc; }
  figcaption { font-size: 9pt; line-height: 1.35; margin-top: 1.5mm; color: #39443d; }
  figcaption b { display: block; font-size: 10pt; color: #14331c; margin-bottom: .5mm; }
  .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 8mm; }
  .box { background: #f2f8f1; border: 1px solid #cfe3cc; border-radius: 3mm; padding: 4mm 5mm; }
  .box h3 { color: #1a6b2c; font-size: 12pt; margin-bottom: 2mm; }
  ol, ul { margin: 0; padding-left: 5mm; } li { margin: 1mm 0; line-height: 1.4; }
  table { width: 100%; border-collapse: collapse; }
  th { text-align: left; background: #1a6b2c; color: #fff; padding: 2mm; font-size: 9.5pt; }
  td { border-bottom: 1px solid #dde6de; padding: 1.2mm 2mm; vertical-align: middle; font-size: 9.5pt; }
  tr { page-break-inside: avoid; }
  td.n { color: #7a8a7e; width: 7mm; } td.th { width: 17mm; } td.th img { width: 15mm; height: 15mm; object-fit: cover; border-radius: 1.5mm; display: block; }
  td.ing { width: 45%; } .desc { color: #59665d; font-size: 8.5pt; }
  .chip { display: inline-block; border-radius: 3mm; padding: .5mm 2.2mm; font-size: 8.8pt; margin: .4mm 0; }
  .chip.base { background: #e4f6df; border: 1px solid #9fd796; } .chip.made { background: #fff1d6; border: 1px solid #f0c46b; }
  .final { background: #10240f; color: #e6ffe0; border-radius: 3mm; padding: 4mm 5mm; margin-top: 4mm; display: flex; gap: 5mm; align-items: center; }
  .final img { width: 30mm; border-radius: 2mm; }
  .final b { color: #7dff6b; }
  .gal { display: grid; grid-template-columns: repeat(6, 1fr); gap: 3mm; }
  .gal div { page-break-inside: avoid; font-size: 8.3pt; line-height: 1.25; }
  .gal img { width: 100%; border-radius: 2mm; display: block; }
  .gal b { display: block; font-size: 8.8pt; margin-top: 1mm; color: #14331c; }
  .gal span { color: #6a776e; }
  .note { font-size: 9pt; color: #5b665e; margin-top: 3mm; }
  .kbd { font-family: Consolas, monospace; background: #e9eee9; border-radius: 1mm; padding: 0 1.5mm; }
</style></head><body>

<section class="page">
  <div class="cover">
    <h1>Rick &amp; Morty<br>y las mezclas de otra dimension</h1>
    <h2>Recorrido completo del juego en VR - capturas, recetas, objetos e interacciones</h2>
    <p>Experiencia VR para Meta Quest 2/3 (Unity 6 + XR Interaction Toolkit). Morty se despierta en el garaje, Rick le
    ensena a usar la mesa de mezclas y se va dejandole una mision: fabricar una Pistola de Portales real antes de que el
    FBI entre a la casa. Todas las capturas son del juego corriendo (automatico, de principio a fin).</p>
    <div class="meta">Proyecto Final - Fundamentos de Realidad Virtual 2026 · ${new Date().toISOString().slice(0, 10)} · ${shots.length} capturas de juego · ${galleryItems.length} objetos · ${recipes.recipes.length + 1} recetas</div>
  </div>
</section>

<section class="page">
  <h2 class="sec">Como se juega</h2>
  <div class="cols">
    <div class="box"><h3>Flujo de la partida</h3><ol>
      <li><b>Intro:</b> pantalla en negro, Rick despierta a Morty.</li>
      <li><b>Onboarding VR:</b> mirar el reloj, caminar, girar, agarrar y usar el gatillo.</li>
      <li><b>Tutorial:</b> 3 mezclas que pide Rick (si fallas la 3a, "reapareces").</li>
      <li><b>Rick se va:</b> abre el garaje y desaparece por un portal.</li>
      <li><b>Juego libre (15 min):</b> experimentar recetas, salir a la calle, eventos inesperados, pistas de Rick.</li>
      <li><b>Final:</b> armar la Pistola de Portales en el soporte, disparar a una pared y cruzar el portal.</li>
      <li><b>Cierre:</b> resultados (puntaje, rango, estadisticas, historial) y creditos.</li>
      <li><b>Fallo:</b> si se acaba el tiempo, el FBI entra (pantalla de derrota).</li>
    </ol></div>
    <div class="box"><h3>Controles (Quest)</h3><ul>
      <li><b>Joystick izquierdo:</b> caminar (movimiento continuo).</li>
      <li><b>Joystick derecho:</b> girar (por pasos o suave, se elige en pausa).</li>
      <li><b>Grip (dedo medio):</b> agarrar / soltar / lanzar.</li>
      <li><b>Gatillo (indice):</b> usar el objeto (disparar, encender, invocar...).</li>
      <li><b>Boton Menu izquierdo:</b> pausa y opciones de confort.</li>
      <li><b>Mezclar:</b> meter 2 o 3 cosas en el tazon y presionar el boton rojo (con el dedo o agarrandolo).</li>
      <li><b>Comer / ponerse algo:</b> acercarlo a la boca / cabeza.</li>
    </ul>
    <h3 style="margin-top:4mm">Inventario</h3>
    <p style="margin:0;line-height:1.45">No hay inventario magico (decision de diseno): solo se carga lo que cabe en las dos manos.
    La <b>coleccion</b> de lo descubierto esta en el holograma del reloj (mirar la muneca izquierda), que ademas muestra la receta de cada objeto ya descubierto.</p></div>
  </div>
  <div class="cols" style="margin-top:6mm">
    <div class="box"><h3>Puntaje</h3><ul>
      <li>+100 por cada objeto nuevo descubierto, +10 si se repite.</li>
      <li>+150 por cada mision de Rick, +1000 por la Pistola de Portales, bono por tiempo restante.</li>
      <li>+50 por easter egg, bonos por interacciones (vecinos, dardos, comida...).</li>
      <li>-50 por explosion, -100 por salir de la cinta, -75 por abrazo del Tio Steve, penalizacion moral por matar al capibara o al Sr. Pantalones.</li>
      <li>Rango final S / A / B / C / D. Todo queda guardado en JSON (mejor puntaje, historial, coleccion).</li>
    </ul></div>
    <div class="box"><h3>Ingredientes base (siempre disponibles)</h3>
      <div class="gal" style="grid-template-columns:repeat(4,1fr)">${baseItems.map(i => `<div>${thumb(i.id) ? `<img src="${thumb(i.id)}">` : ''}<b>${esc(i.name)}</b><span>${i.category === 'base_liquido' ? 'Liquido' : 'Solido'}</span></div>`).join('')}</div>
    </div>
  </div>
</section>

${sections.map(sec => `<section class="page"><h2 class="sec">${esc(sec.name)}</h2>${shotCards(sec.shots)}</section>`).join('\n')}

<section class="page">
  <h2 class="sec">Recetas (${recipes.recipes.length} + la final)</h2>
  <table><thead><tr><th>#</th><th></th><th>Resultado</th><th>Ingredientes</th></tr></thead><tbody>${recipeRows}</tbody></table>
  <div class="final">
    ${thumb(fin.result) ? `<img src="${thumb(fin.result)}">` : ''}
    <div><b>RECETA FINAL - ${esc(name(fin.result))}</b><br>${fin.ingredients.map(name).map(esc).join(' + ')}<br>
    <span style="font-size:9pt;color:#bfe8b8">No se hace en el tazon: la Portal Gun de carton se asegura en el Soporte de Ensamblaje y se le acercan los cristales y el aguardiente.
    Camino corto: Acido + Polvo de Plumbus = Pistola Laser → Pistola Laser + Polvo de Plumbus = Portal Gun de carton · Jugo de Fleeb + Acido = Aguardiente.</span></div>
  </div>
  <p class="note"><span class="chip base">verde</span> ingrediente base · <span class="chip made">amarillo</span> objeto que primero hay que fabricar. Cualquier combinacion que no este en la lista explota.</p>
</section>

<section class="page">
  <h2 class="sec">Galeria de objetos</h2>
  <div class="gal">${galleryItems.map(i => `<div><img src="${thumb(i.id)}"><b>${esc(i.name)}</b><span>${esc(catLabel[i.category] || i.category)} · ${esc(i.description)}</span></div>`).join('')}</div>
  <p class="note">Modelos: algunos descargados de Sketchfab y optimizados para Quest; el resto generados por codigo en Blender. Las texturas y voces son provisionales.</p>
</section>

</body></html>`;

fs.writeFileSync(path.join(doc, 'RickMorty_Recorrido.html'), html);
console.log('HTML listo:', shots.length, 'capturas,', sections.length, 'secciones,', galleryItems.length, 'objetos');
