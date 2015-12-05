using OpenTK;
using OpenTK.Graphics;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Starter3D.Plugin.UniverseSimulator
{
    public struct CB_Simulation_Backup
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 RotAxis;
        public Quaternion AxisAlignmentRot;
        public Quaternion AroundAxisRot;
        public float AngularVel;
        public float AngularRot;
        public float Mass;
        public float Radius;
        public bool HasGravity;
        public bool IsLightSource;
    }

    public class CelestialBody
    {
        #region private fields

        private static readonly float _2PI = (float)(2 * Math.PI);
        private static readonly Quaternion TextureCorrection = GetTextureCorrection();
        private static Vector3 DummyVector3 = Vector3.Zero;
        private static int _axisCounter = 0;

        //Diccionario estático para registrar y encontrar eficientemente celestial bodies
        private static Dictionary<ShapeNode, CelestialBody> _shapeToCelestialBodyMap = new Dictionary<ShapeNode, CelestialBody>();

        //Atributos del cuerpo celeste
        private Vector3 _position;
        private Vector3 _velocity;
        private Vector3 _rotAxis;
        private Quaternion _axisAlignmentRot;
        private Quaternion _aroundAxisRot;
        private float _angularVel;
        private float _angularRot;
        private float _mass;
        private float _radius;
        private bool _hasGravity;
        private bool _isLightSource;
        private bool _isLightInScene;
        private string _name;

        //Atributos extras por motivos de simulación
        private Vector3 _nextPosition;
        private Vector3 _nextVelocity;
        private Vector3 _bb_backleftbottom; //bb = bounding box
        private Vector3 _bb_frontrightup;
        private bool _alive = true;


        //El Shape
        private IMaterial _defaultMaterial;
        private ShapeNode _shape;

        //Las luces asociadas
        private PointLight _lightUp;
        private PointLight _lightDown;
        private PointLight _lightLeft;
        private PointLight _lightRight;
        private PointLight _lightFront;
        private PointLight _lightBack;
        //private PointLight _lightCenter;

        //Línea para representar el eje en modo edición
        private ICurve _axisLine;
        private IVertex _axisBottom;
        private IVertex _axisTop;

        //Referencia al scene
        private IScene _scene;

        //Referencia al renderer
        private IRenderer _renderer;

        //backup struct para la simulación
        CB_Simulation_Backup _simulationBackup;

        #endregion

        #region getters y setters

        public bool Alive
        {
            get { return _alive; }
            set { _alive = value; }
        }

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                if (_isLightInScene)
                    UpdateLightsPositions();
                if (!UniverseSimulatorController.SimulationRunning)
                    UpdateAxisLine();
            }
        }

        public Vector3 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public Vector3 NextPosition
        {
            get { return _nextPosition; }
            set { _nextPosition = value; }
        }

        public Vector3 NextVelocity
        {
            get { return _nextVelocity; }
            set { _nextVelocity = value; }
        }

        public Vector3 RotationAxis
        {
            get { return _rotAxis; }
            private set { _rotAxis = value; }
        }

        public Quaternion AxisAlignmentRotation
        {
            get { return _axisAlignmentRot; }
            set
            {
                _axisAlignmentRot = value;
                if (_shape != null)
                {
                    _rotAxis = Vector3.Transform(Vector3.UnitY, _axisAlignmentRot);
                    _aroundAxisRot = Quaternion.FromAxisAngle(_rotAxis, _angularRot);
                    _shape.Rotation = _aroundAxisRot * _axisAlignmentRot;
                    UpdateAxisLine();
                }
            }
        }

        public float AngularVelocity
        {
            get { return _angularVel; }
            set { _angularVel = value; }
        }

        public float Mass
        {
            get { return _mass; }
            set { _mass = value; }
        }

        public float Radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                if (_shape != null)
                    _shape.Scale = new Vector3(_radius);
                if (_axisBottom != null && !UniverseSimulatorController.SimulationRunning)
                    UpdateAxisLine();
            }
        }

        public bool HasGravity
        {
            get { return _hasGravity; }
            set { _hasGravity = value; }
        }

        public bool IsLightSource
        {
            get { return _isLightSource; }
            set
            {
                _isLightSource = value;
                if (_isLightSource) { UpdateLightsPositions(); AddLightsToScene(); }
                else RemoveLightsFromScene();
            }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public IMaterial Material
        {
            get { return _defaultMaterial; }
            set
            {
                _defaultMaterial = value;
                if (_shape != null) _shape.Shape.Material = value;
            }
        }

        public ShapeNode Shape
        {
            get { return _shape; }
            set { _shape = value; }
        }

        public ICurve AxisLine
        {
            get { return _axisLine; }
        }

        #endregion

        public CelestialBody(Vector3 position, ShapeNode shape, IScene scene, IMaterial axisMaterial, IRenderer renderer)
        {
            _shape = shape;
            _defaultMaterial = shape.Shape.Material;

            _scene = scene;
            _renderer = renderer;

            _radius = 20;
            _mass = 100;
            _position = position;


            _velocity = Vector3.UnitX;
            _rotAxis = Vector3.UnitZ;
            _angularRot = 0;
            _angularVel = 0;

            _hasGravity = false;
            _isLightSource = false;
            _isLightInScene = false;
            _name = "No name";

            _nextPosition = _position;
            _nextVelocity = _velocity;

            _shapeToCelestialBodyMap.Add(_shape, this);

            _shape.Scale = new Vector3(_radius);
            _shape.Position = _position;
            _shape.Rotation = TextureCorrection;

            _lightUp = new PointLight(Color4.White);
            _lightDown = new PointLight(Color4.Yellow);
            _lightLeft = new PointLight(Color4.Yellow);
            _lightRight = new PointLight(Color4.Yellow);
            _lightFront = new PointLight(Color4.Yellow);
            _lightBack = new PointLight(Color4.Yellow);
            //_lightCenter = new PointLight(Color4.Yellow);

            _axisLine = new Curve("axis" + _axisCounter++, 4);
            _axisLine.Material = axisMaterial;
            _axisBottom = new Vertex(_position - _rotAxis * _radius * 1.4f, DummyVector3, DummyVector3);
            _axisTop = new Vertex(_position + _rotAxis * _radius * 1.4f, DummyVector3, DummyVector3);
            _axisLine.AddPoint(_axisBottom);
            _axisLine.AddPoint(_axisTop);
            _axisLine.Configure(renderer);
            _axisAlignmentRot = TextureCorrection;
            _aroundAxisRot = Quaternion.Identity;
        }

        public CelestialBody() { }

        //se actualizan variables internas para el siguiente step de la simulación
        public void UpdateVariablesForNextStep()
        {
            Velocity = _nextVelocity;
            Position = _nextPosition; //por debajo se están seteando las posiciones de las luces también
            _shape.Position = _nextPosition; //para que la nueva posición se refleje en el shape

            //actualizar rotación
            UpdateRotation();

        }

        public void UpdateRotation()
        {
            if (_angularVel == 0) return;

            _angularRot += _angularVel;
            while (_angularRot >= _2PI)
                _angularRot -= _2PI;
            while (_angularRot < 0)
                _angularRot += _2PI;
            _aroundAxisRot = Quaternion.FromAxisAngle(_rotAxis, _angularRot);
            _shape.Rotation = _aroundAxisRot * _axisAlignmentRot;
        }

        public void UpdateAxisLine()
        {
            _axisBottom.Position = _position - _rotAxis * _radius * 1.4f;
            _axisTop.Position = _position + _rotAxis * _radius * 1.4f;
            _axisLine.Clear();
            _axisLine.AddPoint(_axisBottom);
            _axisLine.AddPoint(_axisTop);
            _axisLine.Configure(_renderer);

        }

        // copia todo menos posición y shape
        // (posición se setea por mouse)
        // (copiar shape produce bugs y no es necesario)
        // Actualmente lo uso en el CelestialBodyViewModel
        public void CopyFrom(CelestialBody other)
        {
            if (other == null) return;
            HasGravity = other._hasGravity;
            IsLightSource = other._isLightSource;
            Mass = other._mass;
            Material = other._defaultMaterial;
            Name = other._name;
            Radius = other._radius;
            Velocity = other._velocity;
            NextVelocity = _velocity;
            AngularVelocity = other._angularVel;
            RotationAxis = other._rotAxis;
            AxisAlignmentRotation = other._axisAlignmentRot;
        }

        public void Clone(CelestialBody other)
        {
            if (other == null) return;
            _hasGravity = other._hasGravity;
            _isLightSource = other._isLightSource;
            _mass = other._mass;
            _defaultMaterial = other._defaultMaterial;
            _name = other._name;
            _radius = other._radius;
            _velocity = other._velocity;
            _position = other._position;
            _nextPosition = _position;
            _nextVelocity = _velocity;

            _axisLine = new Curve("axis" + _axisCounter++, 4);
            _axisLine.Material = other._axisLine.Material;

            _axisBottom = other._axisBottom;
            _axisTop = other._axisTop;
            _axisLine.AddPoint(_axisBottom);
            _axisLine.AddPoint(_axisTop);
            _axisLine.Configure(other._renderer);
            _axisAlignmentRot = TextureCorrection;
            _aroundAxisRot = Quaternion.Identity;
            _renderer = other._renderer;


        }


        private void UpdateLightsPositions()
        {
            var r = _radius * 0.95f;
            _lightBack.Position = _position - Vector3.UnitZ * r;
            _lightFront.Position = _position + Vector3.UnitZ * r;
            _lightLeft.Position = _position - Vector3.UnitX * r;
            _lightRight.Position = _position + Vector3.UnitX * r;
            _lightDown.Position = _position - Vector3.UnitY * r;
            _lightUp.Position = _position + Vector3.UnitY * r;
            //_lightCenter.Position = _position;
        }

        public void ToggleLightsInScene()
        {
            if (_isLightInScene)
                RemoveLightsFromScene();
            else
                AddLightsToScene();
        }

        public void AddLightsToScene()
        {
            if (_isLightInScene) return;

            _scene.AddLight(_lightLeft);
            _scene.AddLight(_lightRight);
            _scene.AddLight(_lightUp);
            _scene.AddLight(_lightDown);
            _scene.AddLight(_lightBack);
            _scene.AddLight(_lightFront);
            _isLightInScene = true;

        }
        public void RemoveLightsFromScene()
        {
            if (!_isLightInScene) return;

            UpdateLightsPositions();
            _scene.RemoveLight(_lightLeft);
            _scene.RemoveLight(_lightRight);
            _scene.RemoveLight(_lightUp);
            _scene.RemoveLight(_lightDown);
            _scene.RemoveLight(_lightBack);
            _scene.RemoveLight(_lightFront);
            _isLightInScene = false;

        }



        //retorna la posición menor de la bounding box
        public Vector3 BoundingBox_Min(Step step)
        {
            if (step == Step.Current)
            {
                _bb_backleftbottom.X = _position.X - _radius;
                _bb_backleftbottom.Y = _position.Y - _radius;
                _bb_backleftbottom.Z = _position.Z - _radius;
            }
            else
            {
                _bb_backleftbottom.X = _nextPosition.X - _radius;
                _bb_backleftbottom.Y = _nextPosition.Y - _radius;
                _bb_backleftbottom.Z = _nextPosition.Z - _radius;
            }
            return _bb_backleftbottom;
        }

        //retorna la posición mayor de la bounding box
        public Vector3 BoundingBox_Max(Step step)
        {
            if (step == Step.Current)
            {
                _bb_frontrightup.X = _position.X + _radius;
                _bb_frontrightup.Y = _position.Y + _radius;
                _bb_frontrightup.Z = _position.Z + _radius;
            }
            else
            {
                _bb_frontrightup.X = _nextPosition.X + _radius;
                _bb_frontrightup.Y = _nextPosition.Y + _radius;
                _bb_frontrightup.Z = _nextPosition.Z + _radius;
            }
            return _bb_frontrightup;
        }

        public void RemoveFromScene()
        {
            _scene.RemoveShape(_shape);
            RemoveLightsFromScene();
        }

        public void BackupForSimulation()
        {
            _simulationBackup.AngularRot = _angularRot;
            _simulationBackup.AngularVel = _angularVel;
            _simulationBackup.AroundAxisRot = _aroundAxisRot;
            _simulationBackup.AxisAlignmentRot = _axisAlignmentRot;
            _simulationBackup.HasGravity = _hasGravity;
            _simulationBackup.IsLightSource = _isLightSource;
            _simulationBackup.Mass = _mass;
            _simulationBackup.Position = _position;
            _simulationBackup.Radius = _radius;
            _simulationBackup.RotAxis = _rotAxis;
            _simulationBackup.Velocity = _velocity;
        }

        public void RestoreFromSimulationBackup()
        {
            //==================================================
            // Primero restauramos a nivel de variables privadas
            //==================================================

            _angularRot = _simulationBackup.AngularRot;
            _angularVel = _simulationBackup.AngularVel;
            _aroundAxisRot = _simulationBackup.AroundAxisRot;
            _axisAlignmentRot = _simulationBackup.AxisAlignmentRot;
            _hasGravity = _simulationBackup.HasGravity;
            _isLightSource = _simulationBackup.IsLightSource;
            _mass = _simulationBackup.Mass;
            _position = _simulationBackup.Position;
            _radius = _simulationBackup.Radius;
            _rotAxis = _simulationBackup.RotAxis;
            _velocity = _simulationBackup.Velocity;

            _nextPosition = _position;
            _nextVelocity = _velocity;

            //=====================================================
            // Segundo, actualizamos todos los elementos visuales asociados
            // para que se reflejen los cambios en la GUI
            //==================================================

            //lights
            if (_isLightSource) { AddLightsToScene(); UpdateLightsPositions(); }
            else RemoveLightsFromScene();

            //shape
            _shape.Scale = new Vector3(_radius);
            _shape.Position = _position;
            _shape.Rotation = _aroundAxisRot * _axisAlignmentRot;

            //axis
            UpdateAxisLine();
        }


        public XElement ToXml()
        {
            var invCul = CultureInfo.InvariantCulture;

            XElement element =
             new XElement("CelestialBody",
                 new XElement("position",
                     new XElement("x", _position.X.ToString(invCul)),
                     new XElement("y", _position.Y.ToString(invCul)),
                     new XElement("z", _position.Z.ToString(invCul))
                    ),
                 new XElement("velocity",
                     new XElement("x", _velocity.X.ToString(invCul)),
                     new XElement("y", _velocity.Y.ToString(invCul)),
                     new XElement("z", _velocity.Z.ToString(invCul))),
                 new XElement("rotAxis",
                     new XElement("x", _rotAxis.X.ToString(invCul)),
                     new XElement("y", _rotAxis.Y.ToString(invCul)),
                     new XElement("z", _rotAxis.Z.ToString(invCul))),
                 new XElement("axisAlignmentRot",
                     new XElement("x", _axisAlignmentRot.X.ToString(invCul)),
                     new XElement("y", _axisAlignmentRot.Y.ToString(invCul)),
                     new XElement("z", _axisAlignmentRot.Z.ToString(invCul)),
                     new XElement("w", _axisAlignmentRot.W.ToString(invCul))),
                 new XElement("aroundAxisRot",
                     new XElement("x", _aroundAxisRot.X.ToString(invCul)),
                     new XElement("y", _aroundAxisRot.Y.ToString(invCul)),
                     new XElement("z", _aroundAxisRot.Z.ToString(invCul)),
                     new XElement("w", _aroundAxisRot.W.ToString(invCul))),
                 new XElement("aroundAxisRot",
                     new XElement("x", _aroundAxisRot.X.ToString(invCul)),
                     new XElement("y", _aroundAxisRot.Y.ToString(invCul)),
                     new XElement("z", _aroundAxisRot.Z.ToString(invCul)),
                     new XElement("w", _aroundAxisRot.W.ToString(invCul))),
                 new XElement("angularVel", _angularVel.ToString(invCul)),
                 new XElement("angularRot", _angularRot.ToString(invCul)),
                 new XElement("mass", _mass.ToString(invCul)),
                 new XElement("radius", _radius.ToString(invCul)),
                 new XElement("hasGravity", _hasGravity),
                 new XElement("isLightSource", _isLightSource),
                 new XElement("name", _name),
                 new XElement("material", _defaultMaterial.Name)
             );
            return element;
        }



        #region Métodos públicos estáticos auxiliares

        //para crear desde xml
        public static CelestialBody CreateFromXml(XElement element, IResourceManager resourceManager,
            ShapeNode shape, IScene scene, IMaterial axisMaterial, IRenderer renderer)
        {
            //position
            Vector3 position = new Vector3();
            var prop = element.Element("position");
            position.X = float.Parse(prop.Element("x").Value, CultureInfo.InvariantCulture);
            position.Y = float.Parse(prop.Element("y").Value, CultureInfo.InvariantCulture);
            position.Z = float.Parse(prop.Element("z").Value, CultureInfo.InvariantCulture);

            //velocity
            Vector3 velocity = new Vector3();
            prop = element.Element("velocity");
            velocity.X = float.Parse(prop.Element("x").Value, CultureInfo.InvariantCulture);
            velocity.Y = float.Parse(prop.Element("y").Value, CultureInfo.InvariantCulture);
            velocity.Z = float.Parse(prop.Element("z").Value, CultureInfo.InvariantCulture);

            //rotAxis
            Vector3 rotAxis = new Vector3();
            prop = element.Element("rotAxis");
            rotAxis.X = float.Parse(prop.Element("x").Value, CultureInfo.InvariantCulture);
            rotAxis.Y = float.Parse(prop.Element("y").Value, CultureInfo.InvariantCulture);
            rotAxis.Z = float.Parse(prop.Element("z").Value, CultureInfo.InvariantCulture);

            //axisAlignmentRot
            Quaternion axisAlignmentRot = new Quaternion();
            prop = element.Element("axisAlignmentRot");
            axisAlignmentRot.X = float.Parse(prop.Element("x").Value, CultureInfo.InvariantCulture);
            axisAlignmentRot.Y = float.Parse(prop.Element("y").Value, CultureInfo.InvariantCulture);
            axisAlignmentRot.Z = float.Parse(prop.Element("z").Value, CultureInfo.InvariantCulture);
            axisAlignmentRot.W = float.Parse(prop.Element("w").Value, CultureInfo.InvariantCulture);

            //aroundAxisRot
            Quaternion aroundAxisRot = new Quaternion();
            prop = element.Element("aroundAxisRot");
            aroundAxisRot.X = float.Parse(prop.Element("x").Value, CultureInfo.InvariantCulture);
            aroundAxisRot.Y = float.Parse(prop.Element("y").Value, CultureInfo.InvariantCulture);
            aroundAxisRot.Z = float.Parse(prop.Element("z").Value, CultureInfo.InvariantCulture);
            aroundAxisRot.W = float.Parse(prop.Element("w").Value, CultureInfo.InvariantCulture);

            //angularVel
            prop = element.Element("angularVel");
            float angularVel = float.Parse(prop.Value, CultureInfo.InvariantCulture);

            //angularRot
            prop = element.Element("angularRot");
            float angularRot = float.Parse(prop.Value, CultureInfo.InvariantCulture);

            //mass
            prop = element.Element("mass");
            float mass = float.Parse(prop.Value, CultureInfo.InvariantCulture);

            //radius
            prop = element.Element("radius");
            float radius = float.Parse(prop.Value, CultureInfo.InvariantCulture);

            //hasGravity
            prop = element.Element("hasGravity");
            bool hasGravity = bool.Parse(prop.Value);

            //isLightSource
            prop = element.Element("isLightSource");
            bool isLightSource = bool.Parse(prop.Value);

            //name
            prop = element.Element("name");
            string name = prop.Value;

            //material
            prop = element.Element("material");
            string materialName = prop.Value;
            IMaterial material = resourceManager.GetMaterial(materialName);

            var cb = new CelestialBody(position, shape, scene, axisMaterial, renderer);
            cb.Velocity = velocity;
            cb.RotationAxis = rotAxis;
            cb.AxisAlignmentRotation = axisAlignmentRot;
            cb._aroundAxisRot = aroundAxisRot;
            cb.AngularVelocity = angularVel;
            cb._angularRot = angularRot;
            cb.Mass = mass;
            cb.Radius = radius;
            cb.HasGravity = hasGravity;
            cb.IsLightSource = isLightSource;
            cb.Name = name;
            cb.Material = material;
            return cb;
        }

        //Método para encontrar un cuerpo celeste dependiendo del shape
        public static CelestialBody FindCelestialBody(ShapeNode shape)
        {
            return _shapeToCelestialBodyMap[shape];
        }

        public static void ClearCelestialBody(ShapeNode shape)
        {
            _shapeToCelestialBodyMap.Remove(shape);
        }

        public static void AddCelestialBodies(CelestialBody celestialBody, ShapeNode shape)
        {
            _shapeToCelestialBodyMap.Add(shape, celestialBody);
        }
        #endregion
        #region Métodos privados estáticos auxiliares
        private static Quaternion GetTextureCorrection()
        {
            var axis = Vector3.Cross(Vector3.UnitY, Vector3.UnitZ);
            var angle = (float)Math.PI * 0.5f;
            return Quaternion.FromAxisAngle(axis, angle);
        }
        #endregion

    }


}
