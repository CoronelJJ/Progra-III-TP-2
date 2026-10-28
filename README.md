# Entrega TP-2

Se evalúan tests unitarios para asegurar el funcionamiento de 2 sistemas del juego:

## 1. Máquina de estados + submáquina + Message Broker

Esto maneja el flujo del juego y asegura que la ejecución de cada componente ocurra solo si corresponde al estado actual.

**Lo que se comprueba en los tests unitarios:**

- **a.** Las transiciones solo se dan por llamada directa a clases conocidas (la siguiente o pausa/gameover); el resto están manejadas por el MB.
- **b.** Los estados deben suscribirse en `Enter` y desuscribirse en `Exit`. Un estado no activo no debe poder llamar a un evento del que está desuscrito.
- **c.** Las transiciones surgen como consecuencia de llamados de eventos mediante el MB.

## 2. Aleatoriedad controlada para los randomizadores y su establecimiento con inyección de dependencia

Con la creación de una seed maestra a combinar con el método de aleatoriedad **SplitMix64** y el sistema de **Shuffle Bag**. Este combo, gracias a la seed maestra, reproduce los resultados siempre que se use la misma seed; gracias a SplitMix64 se da aleatoriedad impredecible al jugador; y gracias a la Shuffle Bag se da una frecuencia justa e interesante de resultados para mantener la atención.

**Lo que se comprueba en los tests unitarios:**

- **a.** Cada roller funciona por separado, pero con su propia derivación de la seed maestra.
- **b.** Los tres rollers armados desde una misma seed maestra se reproducen idénticos.
- **c.** Las tiradas intercaladas de uno no alteran la secuencia de los otros.
- **d.** Los slots y su derivación (orden en que se generan las subseeds) quedan preestablecidos para que se reciban siempre en el mismo orden; un nuevo roller simplemente ocupa un nuevo slot.

## Información técnica

La versión es **Unity 6000.6.7f1**. Todos los tests son de EditMode, así que corren en C# con Mocks. Cada carpeta correspondiente tiene otro README con más detalles de la organización y la nomenclatura.

## Aclaración

### ¿Por qué el segundo test no es sobre la inyección de dependencia en el tablero?

Aún me falta definir todos los sistemas y acciones que deben tener acceso al tablero para funcionar. Si bien es una parte sumamente importante del juego y podría usar mocks temporales para algunos sistemas para testear, no ofrecería una verdadera complicación hasta saber la complejidad real del sistema que solicite su lectura o escritura, y solamente sería inyectar la referencia una vez esté establecido.

### ¿Por qué es entonces sobre aleatoriedad controlada?

- Es esencial el buen manejo del RNG en este juego, y los tests unitarios sirven para comprobar que con el sistema de aleatoriedad controlada realmente esté siendo controlado y dando resultados "predecibles" (bajo la misma seed). Tener este sistema bien definido ayuda a progresar, incluso hace el test del tablero (roller para generar pieza, que lleva al colocador, que ya es un sistema de consulta al tablero, y al estar definidos los dados lleva a la lectura del tablero por el evaluador de jugadas).
- También se va a aplicar con inyección de dependencia: una vez creados con la seed maestra, se inyectarán al controlador que los usará en el estado `RollingElements`.
- Por último, también lo vimos en clase.








