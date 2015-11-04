using OpenTK;
using Starter3D.API.resources;
using Starter3D.API.scene.nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.UniverseSimulator
{
    public class Planet
    {
        //Atributos del planeta
        private Vector3 _position;
        private Vector3 _velocity;
        private float _mass;
        private float _radius;                
        private bool _hasGravity;
        private string _name;
        

        //El shape
        private IMaterial _defaultMaterial;
        private ShapeNode _shape;

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public Vector3 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public float Mass
        {
            get { return _mass; }
            set { _mass = value; }
        }

        public float Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        public bool Gravity
        {
            get { return _hasGravity; }
            set { _hasGravity = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public IMaterial Material
        {
            get { return _defaultMaterial; }
            set { _defaultMaterial = value; }
        }
        

        public Planet(Vector3 position, ShapeNode shape)
        {
            _shape = shape;
            _defaultMaterial = shape.Shape.Material;

            _radius = 1;
            _mass = 1;
            _position = position;
            _velocity = new Vector3(1, 0, 0);
            _hasGravity = false;
            _name = "No name";
        }

        public void UpdateMaterial(IMaterial material)
        {
            _shape.Shape.Material = material;
            _defaultMaterial = material;
        }

        public void UpdatePlanet(float radius, float mass, Vector3 velocityDirection, float velocityMagnitude, bool hasGravity, string name)
        {
            _radius = radius;
            _mass = mass;
            _velocity = velocityDirection * velocityMagnitude;
            _hasGravity = hasGravity;
            _name = name;
            UpdateShapeNode();
        }

        private void UpdateShapeNode()
        {
            _shape.Scale = new Vector3(_radius, _radius, _radius);
        }

        //Metodos publicos estaticos auxiliares

        //Metodo para encontrar un planeta dependiendo del shape
        public static Planet FindPlanet(List<Planet> planets, ShapeNode shape)
        {
            foreach (Planet planet in planets)
            {
                if (planet._shape == shape)
                    return planet;
            }
            return null;
        }


        
    }
}
