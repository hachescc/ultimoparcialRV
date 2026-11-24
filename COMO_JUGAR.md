# Cómo jugar en el PC (y cómo pasarse el juego)

Guía para probar **Rick y Morty: Mezclas de otra dimensión** en el computador, sin visor.
En el PC se juega con el simulador XR de Unity: el juego cree que tienes un Quest puesto, y el teclado y el mouse hacen de cabeza y manos.

## 1. Abrir el juego

1. Abre el proyecto en Unity Hub (Unity **6000.3.13f1**).
2. En la ventana *Project* abre la escena `Assets/_RickMorty/Scenes/RickGarage.unity`.
3. Dale **Play**. Haz **un clic dentro de la vista Game** para que reciba el teclado.
4. Si sale la ventana de *Project Validation* con avisos amarillos, ciérrala: son opcionales (ver CLAUDE.md).

## 2. Controles en el PC

| Tecla | Qué hace |
|---|---|
| **W A S D** | Caminar (chocas con las paredes) |
| **F** (sostenida) | Correr |
| **Clic derecho sostenido + mover el mouse** | Mirar alrededor (las manos te siguen) |
| **G** | Agarrar lo que apunta la mano derecha. **G otra vez** lo suelta |
| **T** | Usar lo que tienes en la mano (gatillo): disparar, abrir la caja. Con comida o bebida en la mano, **T** te la comes |
| **Shift + G / Shift + T** | Lo mismo, pero con la mano izquierda |
| **Apuntar al botón rojo y G** | Mezclar lo que hay en el tazón |
| **Mirar abajo a la izquierda** | Reloj Multiversal (tiempo, puntos, colección) y holograma de recetas |
| **P** | Pausa (pista de Rick, reiniciar, salir) |
| **B** | Pantalla de controles |

Cómo apuntar: de cada mano sale un rayo. Pon el rayo sobre el objeto (se agranda un poco y sale su nombre) y presiona **G**: el objeto vuela a tu mano.
Para soltar algo dentro del tazón, acércate a la mesa, apunta al tazón con lo que tienes en la mano y presiona **G** otra vez.

En el visor (Meta Quest) los controles son los normales: joystick izquierdo camina, derecho gira, Grip agarra, gatillo usa, tocar el botón rojo con la mano mezcla, mirar la muñeca izquierda muestra el reloj.

## 3. El recorrido paso a paso

### Inicio (1 minuto)
Rick te despierta golpeando la mesa. Lo que dice sale en el **monitor** sobre la mesa y en la **franja de avisos** que flota abajo de tu vista.

### Tutorial de controles (Rick te pide 4 cosas)
1. **Mirar el reloj**: mira hacia abajo a tu mano izquierda (clic derecho + mouse) hasta que aparezca el holograma.
2. **Caminar**: ve con W A S D hasta el **círculo verde** del piso.
3. **Mirar alrededor**: clic derecho + mouse, da media vuelta.
4. **Agarrar**: vuelve a la mesa, apunta a cualquier frasco y presiona **G**.

Si te demoras, Rick sigue solo después de un rato. Cada paso bien hecho suena un "ding".

### Las 3 misiones de Rick (aprendes a mezclar)
En la mesa hay 8 frascos base en sus soportes, cada uno con su nombre en una placa. En el centro está el **tazón** y a su lado el **botón rojo MEZCLAR**.

| Misión | Qué meter al tazón | Resultado |
|---|---|---|
| 1 | Extracto de Mega-Semilla + Esencia Meeseeks | Caja de Meeseeks |
| 2 | Polvo de Plumbus + Jugo de Fleeb + Esporas Parasitarias | Plumbus |
| 3 | El Plumbus que hiciste + Cristales Kalaxianos | Espada de Zord |

Para cada una: agarra un frasco (G), apúntalo al tazón y suéltalo (G), repite con el otro, y luego apunta al botón rojo y presiona G.
Después de la misión 1 Rick te pide **usar la Caja de Meeseeks**: agárrala con G y presiona **T**. Sale un Meeseeks que limpia la basura.
Si en la misión 3 metes otra cosa, el tazón explota y Rick te "reinicia" (pantalla blanca). Vuelve a intentarlo.

### Rick se va
Rick explica que rompió su pistola de portales, abre la puerta del garaje y se va por un portal. Empieza el **temporizador del FBI: 8 minutos** (se ve en el reloj de la muñeca).

## 4. Final bueno: escapar por el portal (la ruta más corta)

La meta es la **Pistola de Portales** = Portal Gun de cartón + Cristales Kalaxianos + Aguardiente.

1. **Pistola Láser** = Ácido Galaxiano + Polvo de Plumbus (en el tazón, botón rojo).
2. **Portal Gun de cartón** = Pistola Láser + Polvo de Plumbus.
3. **Aguardiente Blanco del Valle** = Jugo de Fleeb + Ácido Galaxiano.
4. Pon la **Portal Gun de cartón** en el **Soporte de Ensamblaje** (el aro verde al lado del tazón, no adentro del tazón).
5. Acércale los **Cristales Kalaxianos** y el **Aguardiente** (suéltalos encima o al lado del soporte). El cartón arde, levita y se convierte en la Pistola de Portales real (+1000 puntos).
6. Agarra la pistola (G). Aparece el holograma de Rick y habla.
7. Apunta a una **pared** (o al piso) y presiona **T**: se abre un portal verde.
8. **Camina hacia el portal** y crúzalo. Sale la pantalla de resultados con tu rango y luego los créditos.

Los frascos base nunca se acaban: cuando los usas o se caen, vuelven solos a su soporte en unos segundos.
Si te pierdes, en la pausa (**P**) está "Pedir una pista a Rick" (cuesta 25 puntos). Rick también da pistas solo si pasa un minuto sin que descubras nada.

## 5. Final malo: te atrapa el FBI

No fabriques la pistola y deja que el reloj llegue a 0:00 (8 minutos). Suenan sirenas a los 5, 3 y 1 minuto. Al final la pantalla se pone roja, el FBI entra y sale la pantalla de resultados con "Atrapado por el FBI" (rango C o D).
Para probarlo rápido en el editor puedes usar los trucos de prueba (abajo).

## 6. Rangos (variantes del final bueno)

El puntaje final suma los puntos de la partida más un bono de **2 puntos por cada segundo que sobró**.

| Rango | Puntos |
|---|---|
| S | 3500 o más |
| A | 2500 o más |
| B | 1600 o más |
| C | menos de 1600 (o perder con 800 o más) |
| D | perder con menos de 800 |

De dónde salen los puntos: cada objeto nuevo descubierto +100, cada misión de Rick +150, la Pistola de Portales +1000, cada secreto +50, probar comida +15, asustar a los vecinos +20.
Restan: cada explosión −50, salir de la cinta del FBI −100 (y 30 segundos), cada abrazo del Falso Tío Steve, pedir pista −25.
Para sacar **S**: descubre unas 10 o 12 recetas, encuentra varios secretos y escapa con mucho tiempo de sobra.

## 7. Los 8 secretos (se encuentran agarrándolos o tocándolos)

1. **Casco de Iron Man**: en la repisa de abajo de la mesa.
2. **Cubo Prison Realm**: en la estantería del fondo.
3. **Esfera del Dragón**: en un frasco de formol de la estantería.
4. **Figura de Hatsune Miku**: estantería, repisa alta.
5. **Llama de Fortnite**: estantería, arriba.
6. **Calcomanía de Colombia**: pegada en la secadora (lavandería, lado izquierdo del garaje).
7. **Pokebola vieja**: en el piso junto a la lavadora, entre calcetines.
8. **Capibara cuántico**: el capibara con gafas VR, al fondo a la derecha.

## 8. Cosas que pasan solas (eventos)

Durante el juego libre, cada rato pasa algo al azar: **apagón**, **invasión del Falso Tío Steve** (te abraza y resta puntos, mátalo con un arma), un **Meeseeks pirata** que se roba un frasco, o un **temblor**.
Si mezclas algo que no existe, el tazón **explota** (−50).
Si cruzas la **cinta amarilla del FBI** afuera, te disparan: −100 puntos, −30 segundos y reapareces en el garaje.

## 9. Trucos para probar (solo en el editor)

- **F1**: salta el tutorial de Rick.
- **F2**: pone en la mesa el kit final (Portal Gun de cartón, Cristales y Aguardiente).
- **F3**: +60 segundos al reloj del FBI.
- **F4**: dispara un evento al azar (Tío Steve, Meeseeks pirata o temblor).
- **F5**: crea un objeto al azar y lo cuenta como descubierto.
- **F6**: gana de una (final bueno).
- **F7**: pierde de una (final del FBI).
- En la pausa también está el botón "Saltar la explicación de Rick" mientras Rick explica o se despide (F1 hace lo mismo).
- En el componente **GameManager** de la escena: casilla *Skip Tutorial* para empezar directo en el juego libre.
- En el componente **GameTimer**: *Duration* (segundos) para probar el final malo con poco tiempo.
