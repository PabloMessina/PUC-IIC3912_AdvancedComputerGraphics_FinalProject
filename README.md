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