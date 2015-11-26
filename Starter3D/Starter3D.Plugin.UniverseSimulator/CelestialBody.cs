using OpenTK;
using OpenTK.Graphics;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.UniverseSimulator
{
    public class CelestialBody
    {
        #region private fields
        //Atributos del cuerpo celeste
        private Vector3 _position;
        private Vector3 _velocity;
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
        
        //Referencia al scene
        private IScene _scene;

        //Diccionario estático para registrar y encontrar eficientemente celestial bodies
        private static Dictionary<ShapeNode, CelestialBody> _shapeToCelestialBodyMap = new Dictionary<ShapeNode, CelestialBody>();
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
                if(_isLightInScene) UpdateLightsPositions();
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
                if(_shape != null) 
                    _shape.Scale = new Vector3(_radius);
            }
        }

        public bool Gravity
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
                ToggleLightsInScene();                
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
                if(_shape != null) _shape.Shape.Material = value;
            }
        }

        public ShapeNode Shape
        {
            get { return _shape; }
            set { _shape = value; }
        }
        #endregion

        public CelestialBody(Vector3 position, ShapeNode shape, IScene scene)
        {
            _shape = shape;
            _defaultMaterial = shape.Shape.Material;

            _scene = scene;

            _radius = 20;
            _mass = 100;
            _position = position;
            _velocity = new Vector3(1, 0, 0);
            _hasGravity = false;
            _isLightSource = false;
            _isLightInScene = false; 
            _name = "No name";

            _nextPosition = _position;
            _nextVelocity = _velocity;

            _shapeToCelestialBodyMap.Add(_shape, this);

            _shape.Scale = new Vector3(_radius);
            _shape.Position = _position;

            _lightUp = new PointLight(Color4.Yellow);
            _lightDown = new PointLight(Color4.Yellow);
            _lightLeft = new PointLight(Color4.Yellow);
            _lightRight = new PointLight(Color4.Yellow);
            _lightFront = new PointLight(Color4.Yellow);
            _lightBack = new PointLight(Color4.Yellow);
            //_lightCenter = new PointLight(Color4.Yellow);
            
        }

        public CelestialBody() { }

        //se actualizan variables internas para el siguiente step de la simulación
        public void UpdateVariablesForNextStep()
        {
            Velocity = _nextVelocity;
            Position = _nextPosition; //por debajo se está seteando laa posiciones de las luces también
            _shape.Position = _nextPosition; //para que la nueva posición se refleje en el shape
        }

        // copia todo menos posición y shape
        // (posición se setea por mouse)
        // (copiar shape produce bugs y no es necesario)
        // Actualmente lo uso en el CelestialBodyViewModel
       public void CopyFrom(CelestialBody other) 
       {
           if (other == null) return;
           Gravity = other.Gravity;
           IsLightSource = other.IsLightSource;
           Mass = other.Mass;
           Material = other.Material;
           Name = other.Name;
           Radius = other.Radius;
           Velocity = other.Velocity;
           NextVelocity = _velocity;           
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


       //removemos o agregamos las luces en escena
       private void ToggleLightsInScene()
       {
           if (_lightBack == null) return;
           //if (_lightCenter == null) return;
           if (_isLightInScene != _isLightSource)
           {
               if (_isLightSource)
               {
                   UpdateLightsPositions();
                   _scene.AddLight(_lightLeft);
                   _scene.AddLight(_lightRight);
                   _scene.AddLight(_lightUp);
                   _scene.AddLight(_lightDown);
                   _scene.AddLight(_lightBack);
                   _scene.AddLight(_lightFront);
                   //_scene.AddLight(_lightCenter);
               }
               else
               {
                   _scene.RemoveLight(_lightLeft);
                   _scene.RemoveLight(_lightRight);
                   _scene.RemoveLight(_lightUp);
                   _scene.RemoveLight(_lightDown);
                   _scene.RemoveLight(_lightBack);
                   _scene.RemoveLight(_lightFront);
                   //_scene.RemoveLight(_lightCenter);
               }
               _isLightInScene = _isLightSource;
           }
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


    #region Métodos públicos estáticos auxiliares

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

    }
    

}
