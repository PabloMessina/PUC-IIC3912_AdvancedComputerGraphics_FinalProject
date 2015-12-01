using OpenTK;
using Starter3D.API.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Starter3D.Plugin.UniverseSimulator
{
    /*
     * ViewModel wrapper para un celestial body 
     */
    public class CelestialBodyViewModel : ViewModelBase
    {
        #region Campos privados
        //referencia al celestialBody wrappeado
        private CelestialBody _celestialBody = null;
        private CelestialBody _backup = new CelestialBody(); //para backupear y poder resetear los cambios

        private IEnumerable<IMaterial> _materials;

        //por optimización dirección y magnitud de la velocidad se manipulan aparte
        private float _velMagnitude;
        private Vector3 _velDirection;

        //se guarda una referencia a la colección de gravity sources para poder agregar/quitar
        //el celestialBody si se le setea/des-setea la gravedad
        private HashSet<CelestialBody> _gravitySources;

        private bool _saved = false;
        private bool _hasChanges = false;
        private bool _hasCelestialBody = false;
        private string _feedback = "";
        #endregion

        //constructor
        public CelestialBodyViewModel(HashSet<CelestialBody> gravitySources, IEnumerable<IMaterial> materials)
        {
            _gravitySources = gravitySources;
            _materials = materials;
        }

        #region Getters y Setters para data-binding con la vista
        public CelestialBody CelestialBody
        {
            set
            {
                if (_celestialBody == value) return;

                if (_celestialBody != null && _saved == false)
                    Reset();

                _celestialBody = value;

                if (value == null)
                    _hasCelestialBody = false;
                else
                {
                    _backup.CopyFrom(_celestialBody);
                    _velDirection = _celestialBody.Velocity.Normalized();
                    _velMagnitude = _celestialBody.Velocity.Length;
                    _hasCelestialBody = true;
                }

                _saved = true;
                _hasChanges = false;
                _feedback = "";
                RaisePropertyChanged();
            }
        }
        public bool HasChanges
        {
            get { return _hasChanges; }
            private set { if (_hasChanges != value) { _hasChanges = value; RaisePropertyChanged("HasChanges"); } }
        }
        public bool HasCelestialBody
        {
            get { return _hasCelestialBody; }
        }
        public bool HasGravity
        {
            get { return _hasCelestialBody ? _celestialBody.Gravity : false; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.Gravity == value) return;
                _celestialBody.Gravity = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("HasGravity");
            }
        }

        public bool HasLight
        {
            get { return _hasCelestialBody ? _celestialBody.IsLightSource : false; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.IsLightSource == value) return;
                _celestialBody.IsLightSource = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("HasLight");
            }
        }
        public string Name
        {
            get { return _hasCelestialBody ? _celestialBody.Name : string.Empty; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.Name == value) return;
                _celestialBody.Name = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("Name");
            }
        }
        public float Radius
        {
            get { return _hasCelestialBody ? _celestialBody.Radius : 0; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.Radius == value) return;
                _celestialBody.Radius = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("Radius");
            }
        }
        public float Mass
        {
            get { return _hasCelestialBody ? _celestialBody.Mass : 0; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.Mass == value) return;
                _celestialBody.Mass = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("Mass");
            }
        }
        public float VelocityX
        {
            get { return _hasCelestialBody ? _velDirection.X : 0; }
            set
            {
                if (_celestialBody == null) return;
                if (_velDirection.X == value) return;
                _velDirection.X = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("VelocityX");
            }
        }
        public float VelocityY
        {
            get { return _hasCelestialBody ? _velDirection.Y : 0; }
            set
            {
                if (_celestialBody == null) return;
                if (_velDirection.Y == value) return;
                _velDirection.Y = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("VelocityY");
            }
        }
        public float VelocityZ
        {
            get { return _hasCelestialBody ? _velDirection.Z : 0; }
            set
            {
                if (_celestialBody == null) return;
                if (_velDirection.Z == value) return;
                _velDirection.Z = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("VelocityZ");
            }
        }
        public float VelocityMagnitude
        {
            get { return _hasCelestialBody ? _velMagnitude : 0; }
            set
            {
                if (_celestialBody == null) return;
                if (_velMagnitude == value) return;
                _velMagnitude = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("VelocityMagnitude");
            }
        }

        public float AngularVelocity
        {
            get { return _hasCelestialBody ? _celestialBody.AngularVelocity : 0; }
            set
            {
                if (_celestialBody == null) return;
                _celestialBody.AngularVelocity = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("AngularVelocity");
            }
        }

        public IMaterial Material
        {
            get { return _hasCelestialBody ? _celestialBody.Material : null; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.Material == value) return;
                _celestialBody.Material = value;
                _saved = false;
                HasChanges = true;
                RaisePropertyChanged("Material");
            }
        }

        public string Feedback { get { return _feedback; } private set { _feedback = value; RaisePropertyChanged("Feedback"); } }
        public IEnumerable<IMaterial> Materials { get { return _materials; } }
        #endregion

        #region Métodos para los botones de la vista   

        // Validar cambios y mostrar feedback de los errores
        private bool ValidateChanges()
        {
            _feedback = "";
            bool valid = true;
            if (_velMagnitude < 0)
            {
                _feedback += "Velocity Magnitude cannot be negative!\n";
                valid = false;
            }
            if (_velDirection.X == 0 && _velDirection.Y == 0 && _velDirection.Z == 0)
            {
                _feedback += "Velocity Direction cannot be equal to (0,0,0)!\n";
                valid = false;
            }
            if (_celestialBody.Radius <= 0)
            {
                _feedback += "Radius must be positive!\n";
                valid = false;
            }
            RaisePropertyChanged("Feedback");
            return valid;
        }
        
        
        public void Save()
        {
            //validamos
            if (!ValidateChanges()) return;

            //seteamos y respaldamos la velocidad aparte
            var vel =  _velDirection.Normalized() * _velMagnitude;
            _backup.Velocity = _celestialBody.Velocity = vel;
            _backup.NextVelocity = _celestialBody.NextVelocity = vel;

            //respaldamos todo lo demás de forma estándar
            _backup.CopyFrom(_celestialBody);

            //agregamos en/removemos de gravity sources
            if (_celestialBody.Gravity) _gravitySources.Add(_celestialBody);
            else _gravitySources.Remove(_celestialBody);

            _saved = true;
            _hasChanges = false;
            RaisePropertyChanged();
        }
        
        // Devolver todo a lo último guardado (respaldado en _backup)
        public void Reset()
        {
            _celestialBody.CopyFrom(_backup);
            _velDirection = _backup.Velocity.Normalized();
            _velMagnitude = _backup.Velocity.Length;            

            if (_celestialBody.Gravity) _gravitySources.Add(_celestialBody);
            else _gravitySources.Remove(_celestialBody);

            _saved = true;
            _hasChanges = false;           
            _feedback = "";
            RaisePropertyChanged();
        }

        float _rotAngle = 0.2f;
        public void RotateRight()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitY, _rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            _saved = false;
            HasChanges = true;
        }
        public void RotateLeft()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitY, -_rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            _saved = false;
            HasChanges = true;
        }
        public void RotateUp()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitX, -_rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            _saved = false;
            HasChanges = true;
        }
        public void RotateDown()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitX, _rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            _saved = false;
            HasChanges = true;
        }


        #endregion

    }
}