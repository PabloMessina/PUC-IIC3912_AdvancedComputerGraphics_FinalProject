﻿using System;
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
using System.Text;
using System.Xml.Linq;


namespace Starter3D.Plugin.UniverseSimulator
{
    public enum Mode { Insert, Pick, Simulate };

    struct CameraBackup
    {
        public Vector3 Position;
        public Vector3 Target;
        public Vector3 Up;
    }

    public class UniverseSimulatorController : ViewModelBase, IController
    {
        #region Atributos
        private const string ScenePath = @"scenes/scene.xml";
        private const string ResourcePath = @"resources/resources.xml";

        private readonly IRenderer _renderer;
        private readonly ISceneReader _sceneReader;
        private readonly IResourceManager _resourceManager;
        private IScene _scene;
        //private IScene _sceneBackup;

        private readonly UniverseControllerView _centralView;
        private readonly LeftToolView _leftView;
        private readonly RightToolView _rightView;
        private readonly BottomToolView _bottomView;
        private readonly TopToolView _topView;

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
        private IMaterial _velBallSelectedMaterial;

        //Materiales temporales para volver al material original de los objetos que se les hacen picking
        private IMaterial _materialDefault;
        //private CelestialBody _selected;
        //private CelestialBody _hover;
        private CelestialBody _selectedCB;
        private CelestialBody _hoverCB;
        private bool _selectedIsVelBall = false;
        private bool _hoverIsVelBall = false;

        //private ShapeNode _selectedToChangeVelocity

        //ViewModel para el cuerpo celeste actualmente seleccionado
        private CelestialBodyViewModel _selectedViewModel;

        //Listas con los cuerpos celestes
        private List<CelestialBody> _celestialBodies = new List<CelestialBody>();
        private List<CelestialBody> _celestialBodiesBackup = new List<CelestialBody>();
        private ShapeNode _base;
        //private CelestialBody _velocitySphere;

        private IMaterial _axisMaterial;
        private IMaterial _velocityMaterial;

        //Simulación
        private bool _simulationRunning = false;
        private bool _pauseSimulation = false;
        private int _currentPlanet = -2;
        private HashSet<CelestialBody> _gravitySources = new HashSet<CelestialBody>();

        //Skybox
        private Cube _skybox;
        private Matrix4 _skyboxTransform;
        private float _skyboxSize = 50000.0f;

        private string _tooltipMessage = "";

        private float _rotAngle = (float)(Math.PI / 180 * 1f);
        private float _camVelocity = 50f;
        private CameraBackup _camBackup;


        #endregion

        public bool IsInMode(Mode mode) { return mode == _mode; }

        public double SimulationTimeStepMaximum { get { return 1000; } }
        public double SimulationTimeStepMinimum { get { return 1; } }
        public double SimulationTimeStep
        {
            get { return PhysicsSolver.TimeDelta * 1000; }
            set { PhysicsSolver.TimeDelta = (float)(value / 1000.0); RaisePropertyChanged("SimulationTimeStep"); }
        }


        ///////////////////////////////Metodos publicos que se usan desde las distintas views (left, right, bottom)///////////////////////////////////////
        //Metodos para actualizar los estados de la barra izquierda

        #region Métodos auxiliares de cámara

        #region Métodos matriciales
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

        #region Movimiento y rotación de cámara
        private void rotateCameraDownward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var horiztonalAxis = Vector3.Cross(forwardDir, cam.Up);
            var rotMatrix = Quaternion.FromAxisAngle(horiztonalAxis, -_rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
            _cameraDirty = true;
        }

        private void rotateCameraUpward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var horiztonalAxis = Vector3.Cross(forwardDir, cam.Up);
            var rotMatrix = Quaternion.FromAxisAngle(horiztonalAxis, _rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
            _cameraDirty = true;
        }

        private void rotateCameraRightward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var verticalAxis = cam.Up;
            var rotMatrix = Quaternion.FromAxisAngle(verticalAxis, -_rotAngle);
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
            _cameraDirty = true;
        }

        private void rotateCameraLeftward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var verticalAxis = cam.Up;
            var rotMatrix = Quaternion.FromAxisAngle(verticalAxis, _rotAngle);
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
            _cameraDirty = true;
        }
        private void rotateCameraClockwise(CameraNode cam)
        {
            var forwardAxis = (cam.Target - cam.Position);
            var rotMatrix = Quaternion.FromAxisAngle(forwardAxis, _rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
            _cameraDirty = true;
        }

        private void rotateCameraCounterClockwise(CameraNode cam)
        {
            var forwardAxis = (cam.Target - cam.Position);
            var rotMatrix = Quaternion.FromAxisAngle(forwardAxis, -_rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
            _cameraDirty = true;
        }

        private void moveCameraRightward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            var rightDir = Vector3.Cross(forwardDir, cam.Up);
            rightDir.NormalizeFast();

            pos += rightDir * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
            _cameraDirty = true;
        }

        private void moveCameraBackward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos -= forwardDir * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
            _cameraDirty = true;
        }

        private void moveCameraLeftward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            var leftDir = Vector3.Cross(cam.Up, forwardDir);
            leftDir.NormalizeFast();

            pos += leftDir * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
            _cameraDirty = true;
        }

        private void moveCameraForward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos += forwardDir * _camVelocity;

            //aseguramos que la cámara no avance más allá del plano z = 100, excepto en simulación
            if (_mode != Mode.Simulate && pos.Z < 100f) pos.Z = 100f;

            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
            _cameraDirty = true;
        }

        private void moveCameraUpward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos += cam.Up * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
            _cameraDirty = true;
        }

        private void moveCameraDownward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos -= cam.Up * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
            _cameraDirty = true;
        }

        #endregion

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

        private Vector3 GetMouseWorldPositionOnNearPlane(int x, int y)
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
            return m_mundo.Xyz;
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
            get { return _topView; }
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
            _topView = new TopToolView(this);

            //inicializamos el solver
            _solver = new PhysicsSolver();
            _solver.CollisionDetected += HandleCollisionDetected;

            //recolectamos materiales para mostrar
            var materials = new List<IMaterial>();
            bool separatorFound = false;
            foreach (var mat in _resourceManager.GetMaterials())
            {
                if (separatorFound) materials.Add(mat);
                else if (mat.Name == "separator") separatorFound = true;
            }

            //seteamos data contexts para las vistas
            _selectedViewModel = new CelestialBodyViewModel(this, _gravitySources, materials);
            _rightView.DataContext = _selectedViewModel;
            _bottomView.DataContext = this;
            _topView.DataContext = this;

            SetMode(Mode.Insert);
        }

        private void HandleCollisionDetected(CelestialBody cb1, CelestialBody cb2)
        {
            TooltipMessage = "Collision between " + cb1.GetHashCode() + " and " + cb2.GetHashCode() + " !!";

            //destruímos al cb de menor masa
            var destroyed = cb1.Mass < cb2.Mass ? cb1 : cb2;
            _solver.SetAsDestroyed(destroyed);
            _celestialBodies.Remove(destroyed);
            destroyed.RemoveLightsFromScene();
            if (destroyed.HasGravity) _gravitySources.Remove(destroyed);
        }

        public void Load()
        {
            InitRenderer();

            _resourceManager.Configure(_renderer);
            _scene.Configure(_renderer);

            //Cuerpo celeste base
            //Para crear nuevos cuerpos celestes lo que hago es tomar el base y crear un clon de este
            //El metodo clon lo hice dentro de ShapeNode
            //_base = _scene.Shapes.ElementAt(0);
            //_velocitySphere = new CelestialBody(new Vector3(0, 0, 0), _scene.Shapes.ElementAt(1), _scene, _resourceManager.GetMaterials().ElementAt(0), _renderer);
            //CelestialBody baseCelestialBody = new CelestialBody(new Vector3(0, 0, 0), _base, _scene);
            //_celestialBodies.Add(baseCelestialBody);
            //_celestialBodies.Add(_velocitySphere);

            //Cargar los materias de hover y select de cuerpos celestes
            _materialDefault = _resourceManager.GetMaterial("redSpecular");
            _selectedMaterial = _resourceManager.GetMaterial("blueDiffuse");
            _hoverMaterial = _resourceManager.GetMaterial("yellowSpecular");
            _velocityMaterial = _resourceManager.GetMaterial("orangeDiffuse");
            _axisMaterial = _resourceManager.GetMaterial("yellow");
            _velBallSelectedMaterial = _resourceManager.GetMaterial("greenDiffuse");

            //Cuerpo celeste base
            //Para crear nuevos cuerpos celestes lo que hago es tomar el base y crear un clon de este
            //El metodo clon lo hice dentro de ShapeNode
            _base = _scene.Shapes.ElementAt(0);

            //shape para la pelotita de velocidad
            ShapeNode velBall = _base.Clone();
            velBall.Shape.Material = _velocityMaterial;
            velBall.Configure(_renderer);

            CelestialBody baseCelestialBody =
                new CelestialBody(
                    this,
                    Vector3.Zero,
                    _base, _scene,
                    _axisMaterial,
                    _renderer,
                    velBall,
                    _velocityMaterial);
            _celestialBodies.Add(baseCelestialBody);

            //inicializamos el skybox
            var size = _skyboxSize;
            var hsize = size / 2;
            _skybox = new Cube("skybox", -hsize, -hsize, -hsize, size, size, size);
            _skybox.Material = _resourceManager.GetMaterial("skyboxMaterial");
            _skybox.Configure(_renderer);

            //borramos los shapes de _scene ya que queremos renderearlos explícitamente nosotros
            _scene.ClearShapes();
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

            //_velocitySphere.Shape.Render(_renderer);
            if (_mode == Mode.Simulate)
            {
                _celestialBodies.ForEach(cb => cb.Shape.Render(_renderer)); //sólo cuerpo
            }
            else
            {
                _celestialBodies.ForEach(cb => {
                    cb.Shape.Render(_renderer); //cuerpo
                    cb.AxisLine.Render(_renderer, Matrix4.Identity); //eje
                    cb.VelocityLine.Render(_renderer, Matrix4.Identity); //línea de velocidad
                    cb.VelocityBall.Render(_renderer); //pelotita de velocidad
                });
            }

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
            var cam = _scene.CurrentCamera as PerspectiveCamera;
            if (_simulationRunning && (cam != null))
            {                
                if (_currentPlanet == -2)
                {
                    cam.Position = _camBackup.Position;
                    cam.Target = _camBackup.Target;
                    cam.Up = _camBackup.Up;
                    _currentPlanet = -1;
                }
                else if (_currentPlanet >= 0)
                {
                    try
                    {
                        var delta = cam.Position - _celestialBodies[_currentPlanet].Position;
                        cam.Position = _celestialBodies[_currentPlanet].Position - Vector3.One * _celestialBodies[_currentPlanet].Radius;
                    }
                    catch (Exception)
                    {
                        _currentPlanet = 0;                        
                    }
                    
                    //cam.Target = cam.Target - delta;
                }
            }
            if (_simulationRunning && !_pauseSimulation)
            {
                //ejecutamos métodos de simulación
                _solver.SolveNextState(_celestialBodies, _gravitySources);
                _solver.SortBoundingBoxXComponents();
                _solver.SweepBoundingBoxesForCollisions();
                _solver.RemoveDestroyedBoundingBoxes();
                RefreshBodiesForNextStep();
            }
            else
            {
                //actualizamos rotaciones en modo edición
                foreach (var cb in _celestialBodies) cb.UpdateRotation();
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

                    //Clonar shape para el celestial body
                    ShapeNode celestialBodyNode = _base.Clone();
                    celestialBodyNode.Shape.Material = _materialDefault;
                    celestialBodyNode.Configure(_renderer);

                    //Clonar shape para pelotita de velocidad
                    ShapeNode velBall = _base.Clone();
                    velBall.Shape.Material = _velocityMaterial;
                    velBall.Configure(_renderer);

                    //Agregar cuerpo celeste a la lista de cuerpos celestes
                    CelestialBody celestialBody =
                        new CelestialBody(this, mousePoint, celestialBodyNode,
                            _scene, _axisMaterial, _renderer, velBall, _velocityMaterial);
                    _celestialBodies.Add(celestialBody);
                }
                else if (_mode == Mode.Pick)
                {
                    //Obtengo el punto
                    var mouseWorld = GetMouseWorldPositionOnNearPlane(x, y);
                    var rayOrigin = _scene.CurrentCamera.Position;
                    var rayDir = (mouseWorld - rayOrigin);
                    rayDir.Normalized();

                    bool intersect = false;
                    bool pickedAVelBall = false;
                    float distance = float.MaxValue;

                    CelestialBody pickedCB = null;

                    //Reviso cada objeto de la escena y lanzo un rayo para ver la interseccion                    
                    foreach (CelestialBody cb in _celestialBodies)
                    {

                        //chequeamos intersección con celestial body
                        float distance_t = 0;
                        bool intersect_t = SphereLineIntersection(cb.Position, cb.Radius, rayOrigin, rayDir, out distance_t);
                        if (intersect_t && distance > distance_t && distance_t > 0)
                        {
                            distance = distance_t;
                            pickedCB = cb;
                            intersect = true;
                            pickedAVelBall = false;
                        }

                        //chequeamos intersección con la pelotita de velocidad
                        distance_t = 0;
                        intersect_t = SphereLineIntersection(
                            cb.VelocityBallPosition,
                            cb.VelocityBallRadius,
                            rayOrigin, rayDir, out distance_t);
                        if (intersect_t && distance > distance_t && distance_t > 0)
                        {
                            distance = distance_t;
                            pickedCB = cb;
                            intersect = true;
                            pickedAVelBall = true;
                        }

                    }

                    //si hay intersección
                    if (intersect)
                    {
                        //si habíamos intersectado algo antes, tenemos que chequear
                        //qué era para ver si hay que restaurar materiales originales
                        if (_selectedCB != null)
                        {
                            //si intersectamos el mismo cb (o su respectiva vel ball)
                            if (_selectedCB == pickedCB)
                            {
                                //si antes era una vel ball pero ahora no
                                if (_selectedIsVelBall && !pickedAVelBall)
                                {
                                    //devolvemos a material default para vel balls
                                    _selectedCB.VelocityBall.Shape.Material = _velocityMaterial;
                                }
                            }
                            // intersectamos un cb diferente
                            else
                            {
                                //restauramos tanto shape como vel ball a sus materiales originales
                                _selectedCB.VelocityBall.Shape.Material = _velocityMaterial;
                                _selectedCB.Shape.Shape.Material = _selectedCB.Material;
                            }
                        }

                        //pickeamos una vel ball: hacemos que tanto la vel ball como el shape se vean
                        //pickeados
                        if (pickedAVelBall)
                        {
                            pickedCB.Shape.Shape.Material = _selectedMaterial;
                            pickedCB.VelocityBall.Shape.Material = _velBallSelectedMaterial;
                        }
                        //pickeamos el shape del cb: sólo mostramos el shape como pickeado
                        else
                        {
                            pickedCB.Shape.Shape.Material = _selectedMaterial;
                        }
                    }

                    //no hay intersección (se hizo click en el vacío)
                    else
                    {
                        //habíamos pickeado algo antes, restauramos sus materials originales
                        if (_selectedCB != null)
                        {
                            _selectedCB.VelocityBall.Shape.Material = _velocityMaterial;
                            _selectedCB.Shape.Shape.Material = _selectedCB.Material;
                        }
                    }

                    //finalmente, actualizamos variables
                    _selectedCB = pickedCB;
                    _selectedViewModel.CelestialBody = pickedCB;
                    _selectedIsVelBall = pickedAVelBall;
                    _hoverCB = null;

                }

            }
            else if (button == ControllerMouseButton.Right)
            {
                _isMouseDownRight = true;
            }
        }

        private bool SphereLineIntersection(Vector3 sphereCenter, float radius, Vector3 rayOrigin, Vector3 dir, out float t)
        {
            t = -1;

            //a*x2 + b*x + c = 0
            //x = (-b +/- sqrt(b2 -4ac))/2a

            var oc = rayOrigin - sphereCenter;
            float a = dir.LengthSquared;
            float b = 2 * Vector3.Dot(dir, oc);
            float c = oc.LengthSquared - radius * radius;

            var delta = b * b - 4 * a * c;

            if (delta < 0) return false; //no hay intersección
            else if (delta == 0) t = -b / (2 * a);
            else
            {
                delta = (float)Math.Sqrt(delta);
                var t1 = (-b - delta) / (2 * a);
                var t2 = (-b + delta) / (2 * a);
                if (t1 >= 0) t = t1;
                if (t2 >= 0 && (t1 < 0 || t2 < t1)) t = t2;
            }
            return t >= 0;
        }

        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
            if (_isMouseDownRight)
                _isMouseDownRight = false;

            if (_isMouseDownLeft)
                _isMouseDownLeft = false;

            /*
            if (_selectedToChangeVelocity != null)
            {
                SetNewVelocity();
            }
             * */


        }

        public void MouseWheel(int delta, int x, int y)
        {
            var cam = _scene.CurrentCamera;
            if (delta > 0)
                moveCameraForward(cam);
            else
                moveCameraBackward(cam);
            _cameraDirty = true;
        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (_mode == Mode.Pick)
            {
                //Arrastar un celestial body
                if (_isMouseDownLeft)
                {
                    //Si hay un celestial body seleccionado
                    if (_selectedCB != null)
                    {
                        Vector3 mousePoint = GetMouseWorldPosition(x, y);

                        //lo seleccionado es la vel ball del cb
                        if (_selectedIsVelBall)
                        {
                            _selectedCB.AlignVelocityWithPoint(mousePoint.Xy);
                        }
                        //lo seleccionado es el shape del cb
                        else
                        {
                            _selectedCB.Position = mousePoint;
                            _selectedCB.NextPosition = mousePoint;
                            _selectedCB.Shape.Position = mousePoint;
                        }
                    }

                }
                else if (_isMouseDownRight)
                {
                    _scene.CurrentCamera.Drag(deltaX, deltaY);
                    _cameraDirty = true;
                }
                else
                {

                    //Seleccionar un planeta con hover////////////////////
                    var mouseWorld = GetMouseWorldPositionOnNearPlane(x, y);
                    var rayOrigin = _scene.CurrentCamera.Position;
                    var rayDir = (mouseWorld - rayOrigin);
                    rayDir.Normalized();

                    bool intersect = false;
                    float distance = float.MaxValue;

                    CelestialBody hoverCB = null;
                    bool hoverIsVelBall = false;

                    //Reviso cada objeto de la escena y lanzo un rayo para ver la interseccion
                    foreach (CelestialBody cb in _celestialBodies)
                    {
                        //intersección con shape
                        float distance_t = 0;
                        bool intersect_t = SphereLineIntersection(cb.Position, cb.Radius, rayOrigin, rayDir, out distance_t);
                        if (intersect_t && distance > distance_t && distance_t > 0)
                        {
                            distance = distance_t;
                            hoverCB = cb;
                            hoverIsVelBall = false;
                            intersect = true;
                        }

                        //intersección con vel ball
                        distance_t = 0;
                        intersect_t = SphereLineIntersection(cb.VelocityBallPosition, cb.VelocityBallRadius, rayOrigin, rayDir, out distance_t);
                        if (intersect_t && distance > distance_t && distance_t > 0)
                        {
                            distance = distance_t;
                            hoverCB = cb;
                            hoverIsVelBall = true;
                            intersect = true;
                        }
                    }

                    //si antes había alguien en hover y es diferente al hover actual (puede ser null)
                    //entonces tenemos que restaurar su material original
                    if (_hoverCB != null && _hoverCB != hoverCB)
                    {
                        if (_hoverIsVelBall)
                            _hoverCB.VelocityBall.Shape.Material = _velocityMaterial;
                        else if( _hoverCB != _selectedCB)
                            _hoverCB.Shape.Shape.Material = _hoverCB.Material;
                    }
                    if (intersect)
                    {
                        if (hoverIsVelBall)
                            hoverCB.VelocityBall.Shape.Material = _hoverMaterial;
                        else if(hoverCB != _selectedCB)
                            hoverCB.Shape.Shape.Material = _hoverMaterial;
                    }
                    _hoverCB = hoverCB;
                    _hoverIsVelBall = hoverIsVelBall;
                }
            }
            else if (_mode == Mode.Insert)
            {
                if (_isMouseDownRight)
                {
                    _scene.CurrentCamera.Drag(deltaX, deltaY);
                    _cameraDirty = true;
                }
            }
            else if (_mode == Mode.Simulate)
            {
                if (_isMouseDownRight)
                {
                    var cam = _scene.CurrentCamera;
                    if (deltaX > 0)
                        rotateCameraRightward(cam);
                    else if (deltaX < 0)
                        rotateCameraLeftward(cam);
                    if (deltaY < 0)
                        rotateCameraUpward(cam);
                    else if (deltaY > 0)
                        rotateCameraDownward(cam);
                }
            }

        }
        /*
        private bool Intersect(ref Vector3 rayOrigin, ref Vector3 rayDir, ref float distance, CelestialBody cb)
        {
            if (cb.Shape == _selected)
                return false;

            float distance_t = 0;
            bool intersect_t = SphereLineIntersection(cb.Position, cb.Radius, rayOrigin, rayDir, out distance_t);

            if (intersect_t && distance > distance_t && distance_t > 0)
            {
                distance = distance_t;
                _hover = cb.Shape;
                return true;
            }
            return false;
        }*/

        #endregion

        #region Manejo de Teclado
        public void KeyDown(int key)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (key)
                {
                    case (49): //2
                        SetMode(Mode.Insert); break;
                    case (49 + 2 - 1): //3
                        SetMode(Mode.Pick); break;
                }
            }
            else
            {

                switch (_mode)
                {
                    case (Mode.Simulate):
                        switch (key)
                        {
                            case (80):
                                _pauseSimulation = !_pauseSimulation;
                                break;
                            case (27):
                                SetMode(Mode.Insert);
                                break;
                        }
                        break;
                }
                //Controles de la camara para todos los modos
                var cam = _scene.CurrentCamera;
                switch (key)
                {
                    case (34): //av pag
                        moveCameraForward(cam);
                        break;
                    case (33): //av pag
                        moveCameraBackward(cam);
                        break;
                    case (38): //a
                    case (65 + 'w' - 'a'): //w
                        moveCameraUpward(cam);
                        break;
                    case (65): //a
                    case (37): //a
                        moveCameraLeftward(cam);
                        break;
                    case (40): //a
                    case (65 + 's' - 'a'): //s
                        moveCameraDownward(cam);
                        break;
                    case (39):
                    case (65 + 'd' - 'a'): //d
                        moveCameraRightward(cam);
                        break;
                    case (65 + 'q' - 'a'): //q
                        moveCameraBackward(cam);
                        break;
                    case (65 + 'e' - 'a'): //e
                        moveCameraForward(cam);
                        break;
                    case (49 + 2 - 1): //2
                        rotateCameraCounterClockwise(cam);
                        break;
                    case (49 + 3 - 1)://3
                        rotateCameraClockwise(cam);
                        break;
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
        
        public void NextCamera()
        {
            _currentPlanet++;
            if (_currentPlanet > _celestialBodies.Count - 1)
                _currentPlanet = 0;
        }

        public void PreviousCamera()
        {
            _currentPlanet--;
            if (_currentPlanet < 0)
                _currentPlanet = _celestialBodies.Count - 1;
        }

        public void ResetCamera()
        {
            _currentPlanet = -2;
        }
        
        //los métodos son autoexplicativos
        public void SetMode(Mode mode)
        {
            LeaveCurrentMode();
            _mode = mode;
            switch (_mode)
            {
                case Mode.Insert: EnterInsertMode(); break;
                case Mode.Pick: EnterPickMode(); break;
                case Mode.Simulate: EnterSimulateMode(); break;
            }
        }

        private void EnterInsertMode()
        {
            TooltipMessage = "Left click insert a new planet";
        }
        private void EnterPickMode()
        {
            TooltipMessage = "Left click to select a planet. Hold down to move it around";
        }
        private void EnterSimulateMode()
        {
            BackupScene();

            _simulationRunning = true;
            _solver.AddBoundingBoxXComponents(_celestialBodies);

            TooltipMessage = "Press ESC to exit. Use right click to rotate the camera view and keys a, s, d, w, q, e, 2, 3 to move the camera around. Press p to pause/continue simulation";
            _leftView.Hide();
            _rightView.Hide();
            _topView.Show();
            //_scene.Lights.First().Color = new OpenTK.Graphics.Color4(0.1F, 0.1F, 0.1F, 1F);

            //backupear cámara
            var cam = _scene.CurrentCamera;
            _camBackup.Position = cam.Position;
            _camBackup.Target = cam.Target;
            _camBackup.Up = cam.Up;
        }

        private void LeaveCurrentMode()
        {
            switch (_mode)
            {
                case Mode.Insert: LeaveInsertMode(); break;
                case Mode.Pick: LeavePickMode(); break;
                case Mode.Simulate: LeaveSimulateMode(); break;
            }
        }
        private void LeaveInsertMode() { }
        private void LeavePickMode()
        {
            //si hay uno seleccionado, le seteamos el material default
            if (_selectedCB != null)
            {                
                _selectedCB.VelocityBall.Shape.Material = _velocityMaterial;
                _selectedCB.Shape.Shape.Material = _selectedCB.Material;
                _selectedCB = null;
            }

            if (_hoverCB != null)
            {
                _hoverCB.VelocityBall.Shape.Material = _velocityMaterial;
                _hoverCB.Shape.Shape.Material = _selectedCB.Material;
                _hoverCB = null;
            }

            //seteamos el celestial body a null en el viewmodel para que
            //los inputs de la vista de edición se bloqueen automáticamente
            _selectedViewModel.CelestialBody = null;
        }

        private void LeaveSimulateMode()
        {
            _simulationRunning = false;
            _solver.ClearBoundingBoxXList();

            _leftView.Show();
            _rightView.Show();
            _topView.Hide();
            _scene.Lights.First().Color = new OpenTK.Graphics.Color4(0.5F, 0.5F, 0.5F, 1);
            _currentPlanet = -2;

            //devolver cámara a su estado original
            var cam = _scene.CurrentCamera;
            cam.Position = _camBackup.Position;
            cam.Target = _camBackup.Target;
            cam.Up = _camBackup.Up;

            RestoreSceneFromBackup();

            //actualizar ejes
            //foreach (var cb in _celestialBodies) cb.UpdateAxisLine();
        }

        private void RefreshMode()
        {
            SetMode(_mode);
        }


        private void BackupScene()
        {
            //copiamos todos los celestial bodies en la lista de backup
            _celestialBodiesBackup.Clear();
            _celestialBodiesBackup.AddRange(_celestialBodies);

            //respaldamos internamente los celestial bodies
            _celestialBodies.ForEach(cb => cb.BackupForSimulation());
        }

        private void RestoreSceneFromBackup()
        {
            //limpiamos estructuras originales
            _celestialBodies.Clear();
            _gravitySources.Clear();

            //restauramos desde backup
            _celestialBodiesBackup.ForEach(cb =>
            {
                cb.RestoreFromSimulationBackup();
                _celestialBodies.Add(cb);
                if (cb.HasGravity) _gravitySources.Add(cb);
            });

        }


        //private void BackUpScene()
        //{
        //    _celestialBodiesBackup = new List<CelestialBody>();
        //    CelestialBody oldVelocitySphere = null;
        //    List<CameraNode> cameras = new List<CameraNode>();
        //    foreach (CameraNode c in _scene.Cameras)
        //    {
        //        cameras.Add(c);
        //    }
        //    List<ShapeNode> shapes = new List<ShapeNode>();
        //    foreach (ShapeNode s in _scene.Shapes)
        //    {
        //        CelestialBody newBody = new CelestialBody();
        //        CelestialBody oldBody = CelestialBody.FindCelestialBody(s);
        //        ShapeNode clone = s.Clone();
        //        newBody.Clone(oldBody);
        //        newBody.Shape = clone;
        //        shapes.Add(clone);
        //        clone.Configure(_renderer);
        //        _celestialBodiesBackup.Add(newBody);
        //        CelestialBody.ClearCelestialBody(s);
        //        CelestialBody.AddCelestialBodies(newBody, clone);
        //
        //        /*
        //        if (s == _velocitySphere.Shape)
        //        {
        //            oldVelocitySphere = _velocitySphere;
        //            _velocitySphere = newBody;
        //        }*/
        //
        //
        //    }
        //    List<LightNode> lights = new List<LightNode>();
        //    foreach (LightNode l in _scene.Lights)
        //    {
        //        lights.Add(l);
        //    }
        //
        //    //Limpiar la esfera de velocidad al comenzar la simulacion
        //    _scene.RemoveShape(oldVelocitySphere.Shape);
        //    CelestialBody.ClearCelestialBody(oldVelocitySphere.Shape);
        //    _celestialBodies.Remove(oldVelocitySphere);
        //
        //    _sceneBackup = new Scene(cameras, shapes, lights);
        //}

        //private void UnBackUpScene()
        //{
        //    _scene = _sceneBackup;
        //    _celestialBodies = _celestialBodiesBackup;
        //    _gravitySources.Clear();
        //
        //    foreach (CelestialBody celestialBody in _celestialBodies)
        //    {
        //        if (celestialBody.Gravity)
        //            _gravitySources.Add(celestialBody);
        //    }
        //    _scene.Configure(_renderer);
        //}

        /*
        private void SetVelocityPoint()
        {
            _velocitySphere.Shape.Scale = new Vector3(5f, 5f, 5f);
            _velocitySphere.Position = _selected.Position + CelestialBody.FindCelestialBody(_selected).Velocity.Normalized() * CelestialBody.FindCelestialBody(_selected).Radius + CelestialBody.FindCelestialBody(_selected).Velocity;
            _velocitySphere.Radius = 5F;
            _velocitySphere.Shape.Position = _velocitySphere.Position;
        }*/

        /*
        private void UnSetVelocityPoint()
        {
            _velocitySphere.Shape.Scale = new Vector3(0f, 0f, 0f);
            _velocitySphere.Radius = 0F;
            _velocitySphere.Shape.Position = new Vector3(0, 0, -10000);
        }*/

        /*
        private void SetNewVelocity()
        {
            CelestialBody celestialBodyToChangeVelocity = CelestialBody.FindCelestialBody(_selectedToChangeVelocity);
            celestialBodyToChangeVelocity.Velocity = _velocitySphere.Position - (celestialBodyToChangeVelocity.Position + CelestialBody.FindCelestialBody(_selected).Velocity.Normalized() * _velocitySphere.Radius);
            celestialBodyToChangeVelocity.NextVelocity = celestialBodyToChangeVelocity.Velocity;
            _selected = _selectedToChangeVelocity;
            _selected.Shape.Material = _selectedMaterial;
            _selectedViewModel.CelestialBody = CelestialBody.FindCelestialBody(_selected);
            _selectedToChangeVelocity = null;
        }*/

        #endregion

        public void SaveSceneAsXML(string path)
        {
            XElement root = new XElement("Scene");
            _celestialBodies.ForEach(cb => root.Add(cb.ToXml()));
            root.Save(path);
        }
        public void LoadSceneFromXmlFile(string path)
        {
            //---------------------------------------------------------------------
            // Primero, creamos una lista de celestial spheres a partir del xml
            //---------------------------------------------------------------------
            XElement root = XElement.Load(path);
            var cbList = new List<CelestialBody>();

            foreach (var xchild in root.Elements("CelestialBody"))
            {
                var shape = _base.Clone();
                shape.Shape.Material = _materialDefault;
                shape.Configure(_renderer);

                var velBall = _base.Clone();
                velBall.Shape.Material = _velocityMaterial;
                velBall.Configure(_renderer);

                var cb = CelestialBody.CreateFromXml(
                    xchild,
                    _resourceManager,
                    shape,
                    _scene,
                    _resourceManager.GetMaterial("yellow"),
                    _renderer, velBall ,_velocityMaterial, this);
                cbList.Add(cb);
            }


            //---------------------------------------------------------------------
            // Segundo, borramos todo lo que había en el universo y reiniciamos
            // con la lista recién creada
            //---------------------------------------------------------------------

            //removemos todos los objetos antiguos
            _celestialBodies.ForEach(cb =>
            {
                CelestialBody.RemoveCelestialBodyFromMap(cb.Shape);
                cb.RemoveLightsFromScene();
            });
            _celestialBodies.Clear();
            _gravitySources.Clear();

            //agregamos los elementos recién creados
            foreach (var cb in cbList)
            {
                _celestialBodies.Add(cb);
                if (cb.HasGravity) _gravitySources.Add(cb);
            }

            //seteamos algunas variables a null
            _hoverCB = null;
            _selectedCB = null;

        }

        public void DestroyCelestialBodySelected(CelestialBody cb)
        {
            cb.RemoveLightsFromScene();
            CelestialBody.RemoveCelestialBodyFromMap(cb.Shape);
            _celestialBodies.Remove(cb);
            if (cb.HasGravity) _gravitySources.Remove(cb);
            _selectedCB = null;
            _hoverCB = null;
        }

    }
}
