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
    public enum Mode { Navigate, Insert, Pick };
    

    public class UniverseSimulatorController : IController
    {
        ///////////////////////////////Atributos///////////////////////////////////////
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
        //private IMaterial _hoverMaterialDefault;
        //private IMaterial _selectedMaterialDefault;

        private ShapeNode _hover;
        private ShapeNode _selected;
        
        //Listas con los planetas
        private List<Planet> _planets = new List<Planet>();
        private ShapeNode _base;

        ///////////////////////////////Metodos publicos que se usan desde las distintas views (left, right, bottom)///////////////////////////////////////
        //Metodos para actualizar los estados de la barra izquierda
        public void ChangeMode(Mode mode)
        {
            _mode = mode;                        
        }

        public void SetTooltip(string s)
        {
            _bottomView.UpdateTooltip(s);
        }

        public IMaterial GetMaterial(string s)
        {
            foreach (IMaterial material in _resourceManager.GetMaterials())
            {
                if (material.Name == s)
                    return material;
            }

            return null;
        }

        ///////////////////////////////Métodos auxiliares de camara///////////////////////////////////////        
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
            Vector3 mousePoint = currCamera.Position + (m_mundo.Xyz - currCamera.Position).Normalized() * 15;
            return mousePoint;
        }

        ///////////////////////////////Métodos heredados de IController///////////////////////////////////////
        //Metodos heredados de IController
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
          _rightView = new RightToolView(this);
          _bottomView = new BottomToolView();

          _mode = Mode.Navigate;
        }

        public void Load()
        {
            InitRenderer();

            //Planeta base
            //Para crear nuevos planetas lo que hago es tomar el planeta base y crear un clon de este
            //El metodo clon lo hice dentro de ShapeNode
            _base = _scene.Shapes.ElementAt(0);
            Planet basePlanet = new Planet(new Vector3(0, 0, 0), _base);
            _planets.Add(basePlanet);

            _resourceManager.Configure(_renderer);
            _scene.Configure(_renderer);

            //Cargar materiales en la vista
            _rightView.SetMaterials(_resourceManager.GetMaterials());   

            //Cargar los materias de hover y select de planetas
            _materialDefault = _resourceManager.GetMaterials().ElementAt(0);
            _hoverMaterial = _resourceManager.GetMaterials().ElementAt(2);
            _selectedMaterial = _resourceManager.GetMaterials().ElementAt(1);
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
            _scene.Render(_renderer);
        }

        public void Update(double deltaTime)
        {
            
        }

        public void MouseDown(API.utils.ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Left)
            {
                _isMouseDownLeft = true;
                if (_mode == Mode.Insert)
                {
                    Vector3 mousePoint = GetMouseWorldPosition(x, y);

                    //Clonar, cambiar posicion y agregar a la escena
                    ShapeNode planetNode = _base.Clone();
                    planetNode.Position = mousePoint;
                    planetNode.Shape.Material = _materialDefault;
                    planetNode.Scale = new Vector3(1, 1, 1);
                    planetNode.Configure(_renderer);
                    _scene.AddShape(planetNode);

                    //Agregar planeta a la lista de planetas
                    Planet planet = new Planet(mousePoint, planetNode);
                    _planets.Add(planet);            
                }
                else if (_mode == Mode.Pick)
                {
                    //Para hacer pick se debe tener algun planeta en estado hover
                    if (_hover != null)
                    {
                        //Devolver el material original al antiguo planeta seleccionado
                        if (_selected != null)
                        {
                            _selected.Shape.Material = Planet.FindPlanet(_planets, _selected).Material;
                        }

                        //El objeto en hover se transforma en el seleccionado
                        _selected = _hover;

                        //Setear el planeta en la vista derecha
                        _rightView.SetPlanet(Planet.FindPlanet(_planets, _selected));
                        
                        //Seteo el material de selección                       
                        _selected.Shape.Material = _selectedMaterial;
                                                
                        _hover = null;
                    }
                    //Si no hay ningun planeta en hover y hay un planeta seleccionado este se des-selecciona
                    else if (_selected != null)
                    {
                        _selected.Shape.Material = Planet.FindPlanet(_planets, _selected).Material;
                        _selected = null;                    
                        _rightView.UnsetPlanet();                    
                    }
                }

            }
            else if (button == ControllerMouseButton.Right)
            {
                _isMouseDownRight = true;
            }
        }       

        public void MouseUp(API.utils.ControllerMouseButton button, int x, int y)
        {
            if (_isMouseDownRight)
                _isMouseDownRight = false;

            if (_isMouseDownLeft)
                _isMouseDownLeft = false;
        }

        public void MouseWheel(int delta, int x, int y)
        {
            _scene.CurrentCamera.Zoom(delta);            
        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (_mode == Mode.Navigate)
            {
                if (_isMouseDownLeft)
                {
                    _scene.CurrentCamera.Drag(deltaX, deltaY);                  
                }
                else if (_isMouseDownRight)
                {
                    //_scene.CurrentCamera.Orbit(deltaX, deltaY);        
                }
            }
            else if(_mode == Mode.Pick)
            {
                //Arrastar un planeta
                if (_isMouseDownLeft) 
                {
                    //Si hay un planeta seleccionado
                    if (_selected != null)
                    {
                        Vector3 mousePoint = GetMouseWorldPosition(x, y);
                        _selected.Position = mousePoint;
                        Planet.FindPlanet(_planets, _selected).Position = mousePoint;
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

                    Vector4 camara = new Vector4(_scene.CurrentCamera.Position.X, _scene.CurrentCamera.Position.Y, _scene.CurrentCamera.Position.Z, 1F);
                    bool intersect = false;
                    float distance = float.MaxValue;


                    if (_hover != null)
                    {
                        _hover.Shape.Material = Planet.FindPlanet(_planets, _hover).Material;
                        _hover = null;
                    }

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
                }
            }
            
        }

        public void KeyDown(int key)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (key == 49)
                {
                    _mode = Mode.Navigate;
                    SetTooltip("Left click to drag around");
                }
                else if (key == 50)
                {
                    _mode = Mode.Insert;
                    SetTooltip("Left click insert a new planet");
                }
                else if (key == 51)
                {
                    _mode = Mode.Pick;
                    SetTooltip("Left click to select a planet. Hold down to move it around");
                }
            }
        }

        public void UpdateSize(double width, double height)
        {
            _width = (float)width;
            _height = (float)height;

            var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
            if (perspectiveCamera != null)
                perspectiveCamera.AspectRatio = (float)(width / height);
        }
    }
}
