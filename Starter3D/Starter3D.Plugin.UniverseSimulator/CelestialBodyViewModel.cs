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

        private UniverseSimulatorController _controller;

        //referencia al celestialBody wrappeado
        private CelestialBody _celestialBody = null;
        //private CelestialBody _backup = new CelestialBody(); //para backupear y poder resetear los cambios

        private IEnumerable<IMaterial> _materials;

        //por optimización dirección y magnitud de la velocidad se manipulan aparte
        private float _velMagnitude;
        private Vector3 _velAux;
        private double _velAngle;

        //se guarda una referencia a la colección de gravity sources para poder agregar/quitar
        //el celestialBody si se le setea/des-setea la gravedad
        private HashSet<CelestialBody> _gravitySources;

        //private bool _saved = false;
        //private bool _hasChanges = false;
        private bool _hasCelestialBody = false;
        private string _feedback = "";

        private static double _2PI = Math.PI * 2;
        #endregion

        //constructor
        public CelestialBodyViewModel(UniverseSimulatorController controller, 
            HashSet<CelestialBody> gravitySources, IEnumerable<IMaterial> materials)
        {
            _controller = controller;
            _gravitySources = gravitySources;
            _materials = materials;
        }

        #region Getters y Setters para data-binding con la vista
        public CelestialBody CelestialBody
        {
            set
            {
                if (_celestialBody == value) return;


                /*if (_celestialBody != null && _saved == false)
                    Reset();*/

                _celestialBody = value;

                if (value == null)
                {
                    _hasCelestialBody = false;
                    _velAngle = 0;
                    _velMagnitude = 0;
                }
                else
                {
                    //_backup.CopyFrom(_celestialBody);
                    var vxy = _celestialBody.Velocity.Xy;
                    if (vxy == Vector2.Zero)
                    {
                        _velAngle = 0;
                        _velMagnitude = 0;
                    }
                    else
                    {
                        _velAngle = Math.Atan2(vxy.Y, vxy.X);
                        _velMagnitude = vxy.Length;
                    }
                    _hasCelestialBody = true;
                }

                //_saved = true;
                //_hasChanges = false;
                _feedback = "";
                RaisePropertyChanged();
            }
        }
        /*
        public bool HasChanges
        {
            get { return _hasChanges; }
            private set { if (_hasChanges != value) { _hasChanges = value; RaisePropertyChanged("HasChanges"); } }
        }*/

        public bool HasCelestialBody
        {
            get { return _hasCelestialBody; }
        }
        public bool HasGravity
        {
            get { return _hasCelestialBody ? _celestialBody.HasGravity : false; }
            set
            {
                if (_celestialBody == null) return;
                if (_celestialBody.HasGravity == value) return;
                _celestialBody.HasGravity = value;
                //_saved = false;
                //HasChanges = true;
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
               //_saved = false;
               //HasChanges = true;
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
                //_saved = false;
                //HasChanges = true;
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
                //_saved = false;
                //HasChanges = true;
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
                //_saved = false;
                //HasChanges = true;
                RaisePropertyChanged("Mass");
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
                _velAux.X = (float)Math.Cos(_velAngle) * _velMagnitude;
                _velAux.Y = (float)Math.Sin(_velAngle) * _velMagnitude;
                _velAux.Z = 0;
                _celestialBody.Velocity = _velAux;
                _celestialBody.NextVelocity = _velAux;

                RaisePropertyChanged("VelocityMagnitude");
            }
        }

        public double VelocityAngle
        {
            get { return _velAngle * 180.0 / Math.PI; }
            set
            {
                if (_celestialBody == null) return;

                var angle = value;
                while (angle >= 360) angle -= 360;
                while (angle <= -360) angle += 360;

                angle *= Math.PI / 180;
                _velAngle = angle;

                _velAux.X = (float) Math.Cos(angle) * _velMagnitude;
                _velAux.Y = (float) Math.Sin(angle) * _velMagnitude;
                _velAux.Z = 0;
                _celestialBody.Velocity = _velAux;
                _celestialBody.NextVelocity = _velAux;

                RaisePropertyChanged("VelocityAngle");
                RaisePropertyChanged("VelocityAngle_Negative");
            }
        }

        public double VelocityAngle_Negative
        {
            get { return - _velAngle * 180.0 / Math.PI; }
        }

        public float AngularVelocity
        {
            get { return _hasCelestialBody ? _celestialBody.AngularVelocity : 0; }
            set
            {
                if (_celestialBody == null) return;
                _celestialBody.AngularVelocity = value;
                //_saved = false;
                //HasChanges = true;
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
                //_saved = false;
                //HasChanges = true;
                RaisePropertyChanged("Material");
            }
        }

        public string Feedback { get { return _feedback; } private set { _feedback = value; RaisePropertyChanged("Feedback"); } }
        public IEnumerable<IMaterial> Materials { get { return _materials; } }
        #endregion

        #region Métodos para los botones de la vista   

        // Validar cambios y mostrar feedback de los errores
        /*
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
        }*/
        
        /*
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
            if (_celestialBody.HasGravity) _gravitySources.Add(_celestialBody);
            else _gravitySources.Remove(_celestialBody);

            _saved = true;
            _hasChanges = false;
            RaisePropertyChanged();
        }*/
        
        // Devolver todo a lo último guardado (respaldado en _backup)
        /*
        public void Reset()
        {
            _celestialBody.CopyFrom(_backup);
            _velDirection = _backup.Velocity.Normalized();
            _velMagnitude = _backup.Velocity.Length;            

            if (_celestialBody.HasGravity) _gravitySources.Add(_celestialBody);
            else _gravitySources.Remove(_celestialBody);

            //_saved = true;
            //_hasChanges = false;           
            _feedback = "";
            RaisePropertyChanged();
        }*/

        float _rotAngle = 0.2f;
        public void RotateBodyRight()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitZ, -_rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            //_saved = false;
            //HasChanges = true;
        }
        public void RotateBodyLeft()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitZ, _rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            //_saved = false;
            //HasChanges = true;
        }
        public void RotateBodyUp()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitX, -_rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            // _saved = false;
            // HasChanges = true;
        }
        public void RotateBodyDown()
        {
            var rot = _celestialBody.AxisAlignmentRotation;
            rot = Quaternion.FromAxisAngle(Vector3.UnitX, _rotAngle) * rot;
            _celestialBody.AxisAlignmentRotation = rot;
            //_saved = false;
            //HasChanges = true;
        }

        public void RotateVelocityRight()
        {
            _velAngle -= _rotAngle;
            if (_velAngle <= -_2PI) _velAngle += _2PI;

            _velAux.X = (float)Math.Cos(_velAngle) * _velMagnitude;
            _velAux.Y = (float)Math.Sin(_velAngle) * _velMagnitude;
            _velAux.Z = 0;
            _celestialBody.Velocity = _velAux;
            _celestialBody.NextVelocity = _velAux;

            RaisePropertyChanged("VelocityAngle");
            RaisePropertyChanged("VelocityAngle_Negative");
            
        }
        public void RotateVelocityLeft()
        {
            _velAngle += _rotAngle;
            if (_velAngle >= _2PI) _velAngle -= _2PI;

            _velAux.X = (float)Math.Cos(_velAngle) * _velMagnitude;
            _velAux.Y = (float)Math.Sin(_velAngle) * _velMagnitude;
            _velAux.Z = 0;
            _celestialBody.Velocity = _velAux;
            _celestialBody.NextVelocity = _velAux;

            RaisePropertyChanged("VelocityAngle");
            RaisePropertyChanged("VelocityAngle_Negative");
        }

        // Borrar el celestial body seleccionado
        public void Delete()
        {
            _controller.DestroyCelestialBodySelected(_celestialBody);
            this.CelestialBody = null;
        }


        #endregion

    }
}