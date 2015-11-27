using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.UniverseSimulator
{
    public delegate void HandleCollision(CelestialBody cb1, CelestialBody cb2);
    public class PhysicsSolver
    {
        public static readonly float G = 9.81f;
        public static readonly float TimeDelta = 0.05f;

        public event HandleCollision CollisionDetected;

        private List<BoundingBox_X> _bboxXList = new List<BoundingBox_X>();
        private HashSet<CelestialBody> _actives = new HashSet<CelestialBody>();
        private int _destroyedCount = 0;

        public PhysicsSolver() { }

        //obtiene la aceleración generada por una lista de fuentes de gravedad sobre una posición dada
        private Vector3 AccelerationOver(CelestialBody target, Vector3 pos, IEnumerable<CelestialBody> gravitySources)
        {
            Vector3 acc = Vector3.Zero;
            foreach (var source in gravitySources)
            {
                if (target == source) continue;
                var dir = (source.Position - pos);
                var r = dir.Length;
                dir.Normalize();

                acc += dir * (G * source.Mass / (r * r));
            }
            return acc;
           
        }

        //resuelve el siguiente estado del objeto celeste usando método RungeKutta
        public void SolveNextState(CelestialBody cb, IEnumerable<CelestialBody> gravitySources)
        {

            var x1 = cb.Position;
            var v1 = cb.Velocity;
            var a1 = AccelerationOver(cb, x1, gravitySources);

            var x2 = x1 + 0.5f * v1 * TimeDelta;
            var v2 = v1 + 0.5f * a1 * TimeDelta;
            var a2 = AccelerationOver(cb, x2, gravitySources);

            var x3 = x1 + 0.5f * v2 * TimeDelta;
            var v3 = v1 + 0.5f * a2 * TimeDelta;
            var a3 = AccelerationOver(cb, x3, gravitySources);

            var x4 = x1 + v3 * TimeDelta;
            var v4 = v1 + a3 * TimeDelta;
            var a4 = AccelerationOver(cb, x4, gravitySources);

            cb.NextPosition += (TimeDelta / 6.0f) * (v1 + 2 * v2 + 2 * v3 + v4);
            cb.NextVelocity += (TimeDelta / 6.0f) * (a1 + 2 * a2 + 2 * a3 + a4);

        }

        public void SolveNextState(IEnumerable<CelestialBody> cbs, IEnumerable<CelestialBody> gravitySources)
        {
            foreach (var cb in cbs) SolveNextState(cb, gravitySources);
        }

        //registra los extremos x de la bb del cb dado
        public void AddBoundingBoxXComponents(CelestialBody cb)
        {
            _bboxXList.Add(new BoundingBox_X(cb, true));
            _bboxXList.Add(new BoundingBox_X(cb, false));
        }
        public void AddBoundingBoxXComponents(IEnumerable<CelestialBody> cbs)
        {
            foreach (var cb in cbs)
            {
                _bboxXList.Add(new BoundingBox_X(cb, true));
                _bboxXList.Add(new BoundingBox_X(cb, false));
            }
        }

        //Ordena los extremos X de las bounding boxes con insertion sort
        public void SortBoundingBoxXComponents()
        {
            for (int i = 1; i < _bboxXList.Count; ++i)
            {
                var p = _bboxXList[i];
                int j = i;
                while (j > 0 && _bboxXList[j - 1].X > p.X)
                {
                    _bboxXList[j] = _bboxXList[j - 1];
                    j--;
                }
                _bboxXList[j] = p;
            }
        }

        /*
         * Barre la lista de extremos X de las bounding boxes, lleva una colección de celestial bodies activos
         * y trata de chequear colisíón entre el siguiente cb de la lista y todos los que ya estaban en la colección de activos.
         **/
        public void SweepBoundingBoxesForCollisions()
        {
            foreach (var bbx in _bboxXList)
            {
                if (bbx.Min)
                {
                    foreach (var active_cb in _actives)
                        if (CheckBoundingBoxCollision(bbx.CelestialBody, active_cb))
                            CheckActualCollision(bbx.CelestialBody, active_cb);
                    _actives.Add(bbx.CelestialBody);
                }
                else _actives.Remove(bbx.CelestialBody);
            }
        }

        //Hace lo que estás pensando que hace :D
        public void ClearBoundingBoxXList()
        {
            _bboxXList.Clear();
        }

        //Algoritmo de colisión entre bounding boxes alineadas con los ejes
        private bool CheckBoundingBoxCollision(CelestialBody cb1, CelestialBody cb2)
        {
            var bbmin1 = cb1.BoundingBox_Min(Step.Next);
            var bbmax1 = cb1.BoundingBox_Max(Step.Next);
            var bbmin2 = cb2.BoundingBox_Min(Step.Next);
            var bbmax2 = cb2.BoundingBox_Max(Step.Next);

            return (bbmax1.X > bbmin2.X && bbmax1.Y > bbmin2.Y && bbmax1.Z > bbmin2.Z &&
                    bbmin1.X < bbmax2.X && bbmin1.Y < bbmax2.Y && bbmin1.Z < bbmax2.Z);
        }

        //Algoritmo de colisión entre esferas
        private void CheckActualCollision(CelestialBody cb1, CelestialBody cb2)
        {
            // |(x1 + v1 * t) - (x2 + v2 * t)| = r1 + r2
            // |(x1 - x2) + t * (v1 - v2)| = r1 + r2
            // (x1-x2)*(x1-x2) + 2*(x1-x2)*(v1-v2)*t + (v1-v2)*(v1-v2)*t^2 = (r1+r2)^2
            // 
            // c =  (x1-x2)*(x1-x2) - (r1+r2)^2
            // b = 2*(x1-x2)*(v1-v2)
            // a = (v1-v2)*(v1-v2)
            // 
            // a*t^2 + b*t + c = 0
            // 
            // t = (-b +/- sqrt(b2 - 4ac))/2a

            var x1_rel = cb1.Position - cb2.Position;
            var v1_rel = cb1.Velocity - cb2.Velocity;

            var k = (cb1.Radius + cb2.Radius);
            var a = v1_rel.LengthSquared;
            var b = 2 * Vector3.Dot(x1_rel, v1_rel);
            var c = x1_rel.LengthSquared - k * k;

            float t = 999999;

            // chequeo naive
            if (x1_rel.LengthSquared <= k * k)
            {
                RaiseCollisionDetected(cb1, cb2);
                return;
            }           
            
            // chequeo hard
            if (a != 0)
            {
                var delta = b * b - 4 * a * c;
                if (delta < 0)
                {
                    return;
                }
                else
                {
                    delta = (float)Math.Sqrt(delta);
                    var t1 = (-b - delta) / (2 * a);
                    var t2 = (-b + delta) / (2 * a);
                    if (t1 >= 0 && t1 < TimeDelta)
                        t = t1;
                    if (t2 >= 0 && t2 < TimeDelta)
                        if (t2 < t) t = t2;
                    if (t > TimeDelta)
                        return;
                }
            }
            else
            {
                if (b == 0)
                    return;
                else
                {
                    t = -c / b;
                    if (t < 0 || t >= TimeDelta)
                        return;
                }
            }

            RaiseCollisionDetected(cb1, cb2);

        }

        //Gatillador del evento CollisionDetected
        private void RaiseCollisionDetected(CelestialBody cb1, CelestialBody  cb2)
        {
            if (CollisionDetected != null) CollisionDetected(cb1, cb2);
        }

        /*
         * Marca al celestial body (cb) como destruído (alive = false) y aumenta el contador _destroyedCount en 2 ya que
         * por cada cb se guardan los 2 extremos X de su bounding box.
         * NOTA: esto asume que el cb fue registrado en algún momento llamando al método AddBoundingBoxXComponents(cb).
         * */
        public void SetAsDestroyed(CelestialBody cb)
        {
            if (cb.Alive)
            {
                cb.Alive = false;
                _destroyedCount += 2;
            }
        }

        /*
         * Cuando se destruyen celestial bodies, los extremos de su boundig box guardados en _bboxXList deben borrarse también.
         * Este algoritmo borra en O(n) todos esos extremos. El algoritmo supone que para cada celestial body borrado se llamó antes al
         * método SetAsDestroyed(cb).
         */
        public void RemoveDestroyedBoundingBoxes()
        {
            if (_destroyedCount == 0) return;

            int size = _bboxXList.Count;
            int i = 0;
            while (true)
            {
                if (_bboxXList[i].CelestialBody.Alive) i++;
                else
                {
                    int j = size - 1;

                    while (true)
                    {
                        if (_bboxXList[j].CelestialBody.Alive)
                        {
                            _bboxXList[i] = _bboxXList[j];
                            size--;
                            if (--_destroyedCount == 0) goto RESIZE_SECTION;
                            break;
                        }
                        else
                        {
                            size--;
                            j--;
                            if (--_destroyedCount == 0) goto RESIZE_SECTION;
                            if (j < i) throw new Exception("Bug: j < i but _destroyedCount = " + _destroyedCount + " !!!");
                        }
                    }
                }
            }

        RESIZE_SECTION:
            _bboxXList.RemoveRange(size, _bboxXList.Count - size);

        }

    }

    /*
     * Clase auxiliar para obtener la coordenada X menor/mayor del bounding box que rodea un
     * cuerpo celeste, me permite saber si estoy comenzando a o terminando de barrer
     * el respectivo bounding box
     */
    public class BoundingBox_X
    {
        private CelestialBody _celestialBody;
        private bool _min;
        public CelestialBody CelestialBody { get { return _celestialBody; } }
        public bool Min { get { return _min; } }
        public BoundingBox_X(CelestialBody celestialBody, bool min)
        {
            _celestialBody = celestialBody;
            _min = min;
        }
        public float X
        {
            get { return _celestialBody.Position.X + (_min ? -_celestialBody.Radius : _celestialBody.Radius); }
        }

    }

    public enum Step { Current, Next }

}
