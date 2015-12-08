# Demo de la aplicación

https://drive.google.com/a/uc.cl/file/d/0BzdYYHxS0KYXQUJ0dUZzLU1wY0k/view?usp=sharing

# Starter3D
Minimal 3D Graphics Engine use for teaching the course "Advanced Topics in Computer Graphics" at Universidad Catolica of Chile 

# Semana del 29/10 - 5/11

## Avances

* Creación del proyecto base
* Agregar paneles
* Creación de la clase Planet
* Agregar esferas
* Pick de esferas
* Drag de esferas
* Cambio de atributos del planeta y material

## Detalles técnicos

Usar variables _isMouseDownLeft y _isMouseDownRight para saber si estan apretados los botones del mouse

Agrege metodo Clone a interfaz IShape y a IMesh

Agregar interserct entre un rayo y un mesh en la clase Ray

Agregar imagetoworld a camara: de coordenadas de imagen a coordenadas de mundo

Agregar metodo CamaraToModel3 a ShapeNode: de coordenada de mundo  a coordenada de modelo

Left view: panel de herramientas
Right view: vista de datos de planetas
Bottom view: tooltips

Mode: enumeración que define el modo actual: Navigate, Insert, Pick

En la clase UniverseSimulatorController lo dividí en 4 secciones

* Atributos
* Metodos auxiliares de la camara
* Metodos publicos que se acceden de las vistas para actualizar algunos estados
* Metodos heredados de IController


# Semana del 6/11 - 12/11

## Avances

* Arreglar dropping/dragging para que todo esté en el plano z = 0
* Refactorizar vista de la derecha para que funcione con data-binding
* Hacer vista de la derecha más responsive
* Agregar modo de simulación con RungeKutta
* Agregar collision detection usando el algoritmo de ordenación y barrido de bounding boxes
* Agregar skybox
* Agregar más texturas

## Detalles técnicos

Hay muchos cambios, describirlos todos acá puede resultar abrumador. Recomiendo leer los comentarios, los nombres de las variables, los nombres de los métodos y ver cómo las cosas se van llamando entre sí. Las mayores modificaciones / refactorizaciones hechas sobre el código anterior se encuentran en la implementación de data-binding (aquí una buena referencia: http://www.codeproject.com/Articles/819294/WPF-MVVM-step-by-step-Basics-to-Advance-Level)