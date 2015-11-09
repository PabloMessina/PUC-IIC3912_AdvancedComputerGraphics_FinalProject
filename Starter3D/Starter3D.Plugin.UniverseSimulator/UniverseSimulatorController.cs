using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry.primitives;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;
using System.Windows.Input;
using Starter3D.API.geometry;
using Starter3D.API.math;


namespace Starter3D.Plugin.UniverseSimulator
{
    public enum Mode { Navigate, Insert, Pick, Simulate };

    public class UniverseSimulatorController : ViewModelBase, IController
    {
        #region Atributos
        private const string ScenePath = @"scenes/scene.xml";
        private const string ResourcePath = @"resources/resources.xml";

        private readonly IRenderer _renderer;
        private readonly ISceneReader _sceneReader;
        private readonly IResourceManager _resourceManager;
        private readonly IScene _scene;

        private readonly UniverseControllerView _centralView;
        private readonly LeftToolView _leftView;
        private readonly RightToolView _rightView;
        private readonly BottomToolView _bottomView;

        private float _width;
        private float _height;

        private bool _cameraDirty = true;

        private PhysicsSolver _solver;

        //El modo actual
        private Mode _mode;

        //Mouse
        private bool _isMouseDownLeft = false;
        private bool _isMouseDownRight = false;

        //Picking
        private IMaterial _hoverMaterial;
        private IMaterial _selectedMaterial;

        //Materiales temporales para volver al material original de los objetos que se les hacen picking
        private IMaterial _materialDefault;

        private ShapeNode _hover;
        private ShapeNode _selected;

        //ViewModel para el cuerpo celeste actualmente seleccionado
        private CelestialBodyViewModel _selectedViewModel;

        //Listas con los cuerpos celestes
        private List<CelestialBody> _celestialBodies = new List<CelestialBody>();
        private ShapeNode _base;

        //Simulación
        private bool _simulationRunning = false;
        private HashSet<CelestialBody> _gravitySources = new HashSet<CelestialBody>();

        //Skybox
        private Cube _skybox;
        private Matrix4 _skyboxTransform;
        private float _skyboxSize = 50000.0f;
        
        private string _tooltipMessage = "";

        #endregion

        ///////////////////////////////Metodos publicos que se usan desde las distintas views (left, right, bottom)///////////////////////////////////////
        //Metodos para actualizar los estados de la barra izquierda

        #region Métodos auxiliares de cámara   
        private Matrix4 getProjectionMatrix(CameraNode camera)
        {
            if (camera is PerspectiveCamera)
            {
                var perspCamera = camera as PerspectiveCamera;
                return Matrix4.CreatePerspectiveFieldOfView(perspCamera.FieldOfView,
                        perspCamera.AspectRatio, perspCamera.NearClip, perspCamera.FarClip);
            }
            else
            {
                var ortCamera = camera as OrtographicCamera;
                return Matrix4.CreateOrthographic(ortCamera.Width, ortCamera.Height,
                    ortCamera.NearClip, ortCamera.FarClip);
            }
        }
        private Matrix4 getViewMatrix(CameraNode camera)
        {
            return Matrix4.LookAt(camera.Position, camera.Target, camera.Up);
        }
        #endregion

        private Vector3 GetMouseWorldPosition(int x, int y)
        {
            //to clipping
            float adjustedX = (2.0f * (float)x / (float)_width) - 1;
            float adjustedY = (2.0f * (float)(_height - y) / (float)_height) - 1;
            var m_clipping = new Vector4(adjustedX, adjustedY, -1, 1);

            //to camera
            var currCamera = _scene.CurrentCamera;
            var inverse_projMatrix = getProjectionMatrix(currCamera).Inverted();
            var m_camera = Vector4.Transform(m_clipping, inverse_projMatrix);
            m_camera /= m_camera.W;
            m_camera.Z = -currCamera.NearClip;

            //to world
            var viewMatrix = getViewMatrix(currCamera);
            var inverse_viewMatrix = viewMatrix.Inverted();
            var m_mundo = Vector4.Transform(m_camera, inverse_viewMatrix);

            //project onto plane z = 0
            var dir = (m_mundo.Xyz - currCamera.Position).Normalized();
            //p.z + d.z * t = 0
            //t = -p.z / d.z	
            //WARNING: this assumes that dir.Z != 0
            var t = -currCamera.Position.Z / dir.Z;
            var mousePoint = currCamera.Position + dir * t;
            return mousePoint;
        }


        #region Métodos heredados de IController
        public int Width
        {
            get { return 800; }
        }

        public int Height
        {
            get { return 600; }
        }

        public bool IsFullScreen
        {
            get { return true; }
        }

        public object CentralView
        {
            get { return _centralView; }
        }

        public object LeftView
        {
            get { return _leftView; }
        }

        public object RightView
        {
            get { return _rightView; }
        }

        public object TopView
        {
            get { return null; }
        }

        public object BottomView
        {
            get { return _bottomView; }
        }

        public bool HasUserInterface
        {
            get { return true; }
        }

        public string Name
        {
            get { return "Universe Simulator 2015"; }
        }
        #endregion

        public string TooltipMessage
        {
            get { return _tooltipMessage; }
            private set { _tooltipMessage = value; RaisePropertyChanged("TooltipMessage"); }
        }

        public UniverseSimulatorController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
        {
            if (renderer == null) throw new ArgumentNullException("renderer");
            if (sceneReader == null) throw new ArgumentNullException("sceneReader");
            if (resourceManager == null) throw new ArgumentNullException("resourceManager");
            _renderer = renderer;
            _sceneReader = sceneReader;
            _resourceManager = resourceManager;

            _resourceManager.Load(ResourcePath);
            _scene = _sceneReader.Read(ScenePath);

            _centralView = new UniverseControllerView(this);
            _leftView = new LeftToolView(this);
            _rightView = new RightToolView();
            _bottomView = new BottomToolView();

            //inicializamos el solver
            _solver = new PhysicsSolver();
            _solver.CollisionDetected += HandleCollisionDetected;
            
            //recolectamos materiales para mostrar
            var materials = new List<IMaterial>();
            bool separatorFound = false;
            foreach(var mat in _resourceManager.GetMaterials())
            {
                if (separatorFound) materials.Add(mat);
                else if (mat.Name == "separator") separatorFound = true;
            }

            //inicializamos y seteamos viewmodel para la vista de edición (derecha)
            _selectedViewModel = new CelestialBodyViewModel(_gravitySources, materials);
            _rightView.DataContext = _selectedViewModel;

            //seteamos datacontext de la vista de abajo
            _bottomView.DataContext = this;

            SetMode(Mode.Navigate);
        }

        private void HandleCollisionDetected(CelestialBody cb1, CelestialBody cb2)
        {
            TooltipMessage = "Collision between " + cb1.GetHashCode() + " and " + cb2.GetHashCode() + " !!";

            //destruímos al cb de menor masa
            var destroyed = cb1.Mass < cb2.Mass ? cb1 : cb2;
            _solver.SetAsDestroyed(destroyed);
            _celestialBodies.Remove(destroyed);
            _scene.RemoveShape(destroyed.Shape);
            if(destroyed.Gravity) _gravitySources.Remove(destroyed);
        }

        public void Load()
        {
            InitRenderer();

            //Cuerpo celeste base
            //Para crear nuevos cuerpos celestes lo que hago es tomar el base y crear un clon de este
            //El metodo clon lo hice dentro de ShapeNode
            _base = _scene.Shapes.ElementAt(0);
            CelestialBody baseCelestialBody = new CelestialBody(new Vector3(0, 0, 0), _base, _scene);
            _celestialBodies.Add(baseCelestialBody);

            _resourceManager.Configure(_renderer);
            _scene.Configure(_renderer);

            //Cargar los materias de hover y select de cuerpos celestes
            _materialDefault = _resourceManager.GetMaterials().ElementAt(0);
            _selectedMaterial = _resourceManager.GetMaterials().ElementAt(1);
            _hoverMaterial = _resourceManager.GetMaterials().ElementAt(2);

            //inicializamos el skybox
            var size = _skyboxSize;
            var hsize = size / 2;
            _skybox = new Cube("skybox", -hsize, -hsize, -hsize, size, size, size);
            _skybox.Material = _resourceManager.GetMaterial("skyboxMaterial");
            _skybox.Configure(_renderer);
        }

        private void InitRenderer()
        {
            _renderer.SetBackgroundColor(0.0f, 0.0f, 0.0f);
            _renderer.EnableZBuffer(true);
            _renderer.EnableWireframe(false);
            _renderer.SetCullMode(CullMode.Back);
        }

        public void Render(double deltaTime)
        {
            //render scene
            _scene.Render(_renderer);

            //render skybox
            if (_cameraDirty)
            {
                _skybox.Material.SetParameter("cameraPosition", _scene.CurrentCamera.Position);
                _skyboxTransform = Matrix4.CreateTranslation(_scene.CurrentCamera.Position);
                _cameraDirty = false;
            }
            _skybox.Render(_renderer, _skyboxTransform);
        }

        public void Update(double deltaTime)
        {
            if (_simulationRunning)
            {
                _solver.SolveNextState(_celestialBodies, _gravitySources);
                _solver.SortBoundingBoxXComponents();
                _solver.SweepBoundingBoxesForCollisions();
                _solver.RemoveDestroyedBoundingBoxes();
                RefreshBodiesForNextStep();
            }
        }
        private void RefreshBodiesForNextStep()
        {
            foreach (var cb in _celestialBodies) cb.UpdateVariablesForNextStep();
        }

        #region Manejo de Mouse

        public void MouseDown(ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Left)
            {
                _isMouseDownLeft = true;
                if (_mode == Mode.Insert)
                {
                    Vector3 mousePoint = GetMouseWorldPosition(x, y);

                    //Clonar y agregar a la escena
                    ShapeNode celestialBodyNode = _base.Clone();
                    celestialBodyNode.Shape.Material = _materialDefault;
                    celestialBodyNode.Configure(_renderer);
                    _scene.AddShape(celestialBodyNode);

                    //Agregar cuerpo celeste a la lista de cuerpos celestes
                    CelestialBody celestialBody = new CelestialBody(mousePoint, celestialBodyNode, _scene);
                    _celestialBodies.Add(celestialBody);
                }
                else if (_mode == Mode.Pick)
                {
                    //Obtengo el punto
                    float xf = (float)x;
                    float yf = (float)y;
                    Vector4 mouse = new Vector4(2F * (xf / _width) - 1F, 2F * ((_height - yf) / _height) - 1F, -1F, 1F);
                    mouse = _scene.CurrentCamera.ImageToWorld(mouse);

                    Vector4 camara = new Vector4(_scene.CurrentCamera.Position, 1F);
                    bool intersect = false;
                    float distance = float.MaxValue;

                    ShapeNode picked = null;

                    //Reviso cada objeto de la escena y lanzo un rayo para ver la interseccion
                    foreach (ShapeNode s in _scene.Shapes)
                    {
                        Vector3 mouseModel, camaraModel;
                        mouseModel = s.CamaraToModel3(mouse);
                        camaraModel = s.CamaraToModel3(camara);

                        Ray r = new Ray(camaraModel, (mouseModel - camaraModel));
                        float distance_t = 0;
                        bool intersect_t = Ray.Intersect(r, (IMesh)s.Shape, out distance_t);
                        if (intersect_t && distance > distance_t && distance_t > 0)
                        {
                            distance = distance_t;
                            picked = s;
                            intersect = true;
                        }
                    }

                    if (_selected != null && _selected != picked)
                    {
                        _selected.Shape.Material = CelestialBody.FindCelestialBody(_selected).Material;
                    }

                    if (intersect)
                    {
                        _selected = picked;
                        _selected.Shape.Material = _selectedMaterial;
                        _selectedViewModel.CelestialBody = CelestialBody.FindCelestialBody(_selected);
                        _hover = null;
                    }
                    else
                    {
                        _selected = null;
                        _selectedViewModel.CelestialBody = null;
                    }

                }

            }
            else if (button == ControllerMouseButton.Right)
            {
                _isMouseDownRight = true;
            }
        }

        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
            if (_isMouseDownRight)
                _isMouseDownRight = false;

            if (_isMouseDownLeft)
                _isMouseDownLeft = false;
        }

        public void MouseWheel(int delta, int x, int y)
        {
            _scene.CurrentCamera.Zoom(delta);
            _cameraDirty = true;
        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (_mode == Mode.Navigate)
            {
                if (_isMouseDownLeft)
                {
                    _scene.CurrentCamera.Drag(deltaX, deltaY);
                    _cameraDirty = true;
                }
                else if (_isMouseDownRight)
                {
                    //_scene.CurrentCamera.Orbit(deltaX, deltaY);        
                }
            }
            else if (_mode == Mode.Pick)
            {
                //Arrastar un planeta
                if (_isMouseDownLeft)
                {
                    //Si hay un planeta seleccionado
                    if (_selected != null)
                    {
                        Vector3 mousePoint = GetMouseWorldPosition(x, y);
                        var cb = CelestialBody.FindCelestialBody(_selected);
                        cb.Position = mousePoint;
                        cb.NextPosition = mousePoint;
                        cb.Shape.Position = mousePoint;
                    }

                }
                //Seleccionar un planeta con hover
                else
                {
                    //Obtengo el punto
                    float xf = (float)x;
                    float yf = (float)y;
                    Vector4 mouse = new Vector4(2F * (xf / _width) - 1F, 2F * ((_height - yf) / _height) - 1F, -1F, 1F);
                    mouse = _scene.CurrentCamera.ImageToWorld(mouse);

                    Vector4 camara = new Vector4(_scene.CurrentCamera.Position, 1F);
                    bool intersect = false;
                    float distance = float.MaxValue;

                    //Reviso cada objeto de la escena y lanzo un rayo para ver la interseccion
                    foreach (ShapeNode s in _scene.Shapes)
                    {
                        if (s == _selected)
                            continue;
                        Vector3 mouseModel, camaraModel;
                        mouseModel = s.CamaraToModel3(mouse);
                        camaraModel = s.CamaraToModel3(camara);

                        Ray r = new Ray(camaraModel, (mouseModel - camaraModel));
                        float distance_t = 0;
                        bool intersect_t = Ray.Intersect(r, (IMesh)s.Shape, out distance_t);
                        if (intersect_t && distance > distance_t && distance_t > 0)
                        {
                            distance = distance_t;
                            _hover = s;
                            intersect = true;
                        }
                    }
                    if (intersect)
                    {
                        _hover.Shape.Material = _hoverMaterial;
                    }
                    else if (_hover != null)
                    {
                        _hover.Shape.Material = CelestialBody.FindCelestialBody(_hover).Material;
                        _hover = null;
                    }
                }
            }

        }
        #endregion

        #region Manejo de Teclado
        public void KeyDown(int key)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (key == '1')
                {
                    SetMode(Mode.Navigate);
                }
                else if (key == '2')
                {
                    SetMode(Mode.Insert);
                }
                else if (key == '3')
                {
                    SetMode(Mode.Pick);
                }
            }
        }
        #endregion

        public void UpdateSize(double width, double height)
        {
            _width = (float)width;
            _height = (float)height;            

            var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
            if (perspectiveCamera != null)
                perspectiveCamera.AspectRatio = (float)(width / height);            
        }

        #region Métodos para manejar entrada y salida de modo
        //los métodos son autoexplicativos
        public void SetMode(Mode mode)
        {
            LeaveCurrentMode();
            _mode = mode;
            switch (_mode)
            {
                case Mode.Insert: EnterInsertMode(); break;
                case Mode.Navigate: EnterNavigateMode(); break;
                case Mode.Pick: EnterPickMode(); break;
                case Mode.Simulate: EnterSimulateMode(); break;
            }
        }

        private void EnterInsertMode()
        {
            TooltipMessage = "Left click insert a new planet";
        }
        private void EnterNavigateMode()
        {
            TooltipMessage = "Left click to drag around";
        }
        private void EnterPickMode()
        {
            TooltipMessage = "Left click to select a planet. Hold down to move it around";
        }
        private void EnterSimulateMode()
        {
            _simulationRunning = true;
            _solver.AddBoundingBoxXComponents(_celestialBodies);
        }

        private void LeaveCurrentMode()
        {
            switch (_mode)
            {
                case Mode.Insert: LeaveInsertMode(); break;
                case Mode.Navigate: LeaveNavigateMode(); break;
                case Mode.Pick: LeavePickMode(); break;
                case Mode.Simulate: LeaveSimulateMode(); break;
            }
        }
        private void LeaveInsertMode() { }
        private void LeaveNavigateMode() { }
        private void LeavePickMode()
        {
            //si hay uno seleccionado, le seteamos el material default
            if (_selected != null)
                _selected.Shape.Material = CelestialBody.FindCelestialBody(_selected).Material;

            //seteamos el celestial body a null en el viewmodel para que
            //los inputs de la vista de edición se bloqueen automáticamente
            _selectedViewModel.CelestialBody = null;
            _selected = null;
        }

        private void LeaveSimulateMode()
        {
            _simulationRunning = false;
            _solver.ClearBoundingBoxXList();
        }

        private void RefreshMode()
        {
            SetMode(_mode);
        }

        #endregion

    }
}
