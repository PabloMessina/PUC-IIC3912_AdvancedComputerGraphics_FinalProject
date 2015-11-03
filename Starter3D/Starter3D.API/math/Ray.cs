using System;
using OpenTK;
using Starter3D.API.geometry;
using System.Collections.Generic;

namespace Starter3D.API.math
{
  public class Ray
  {
    protected internal Vector3 Position;
    protected internal Vector3 Direction;
    private const float Precission = 0.000001f;

    public Ray (Vector3 position, Vector3 direction)
    {
      Position = position;
      Direction = direction.Normalized();
    }

    public static bool Intersect(Ray ray, IMesh mesh, out float distance)
    {
        distance = float.MaxValue;
        bool retorno = false;
        foreach (IPolygon triangle in mesh.GetTriangles())
        {
            IEnumerable<IVertex> vertex = triangle.Vertices;
            IVertex[] vertex_array = new IVertex[3];
            int i = 0;
            foreach (Vertex v in vertex)
            {
                vertex_array[i] = v;
                i++;
            }
            float distance_t = 0.0F;
            if (Intersect(ray, vertex_array, out distance_t))
            {
                if (distance > distance_t && distance_t > Precission)
                {
                    distance = distance_t;
                    retorno = true;
                }
            }
        }
        return retorno;
    }

    public static bool Intersect(Ray ray, IVertex[] vertex, out float distance)
    {
        distance = 0.0F;

        float D = determinante(ray.Direction, (vertex[1].Position - vertex[0].Position), (vertex[2].Position - vertex[0].Position));
        float Dx = determinante((vertex[0].Position - ray.Position), (vertex[1].Position - vertex[0].Position), (vertex[2].Position - vertex[0].Position));
        float Dy = determinante(ray.Direction, (vertex[0].Position - ray.Position), (vertex[2].Position - vertex[0].Position));
        float Dz = determinante(ray.Direction, (vertex[1].Position - vertex[0].Position), (vertex[0].Position - ray.Position));

        float t = Dx / D;

        float beta = -Dy / D;
        float gama = -Dz / D;
        float alfa = 1 - beta - gama;
        if (alfa > 0 && alfa < 1 && t > 0 && beta > 0 && gama > 0)
        {
            distance = t;
            return true;
        }
        return false;
    }

    private static float determinante(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float det_of_x = v1.X * v2.Y * v3.Z + v1.Y * v2.Z * v3.X + v1.Z * v2.X * v3.Y;
        det_of_x -= v3.X * v2.Y * v1.Z + v3.Y * v2.Z * v1.X + v3.Z * v2.X * v1.Y;

        return det_of_x;
    }

    public static bool Intersect(Ray ray, BoundingBox box, out float distance){
      // current minimum t to get inside
      float t = 0.0f; 
      // max t before going outside in one dim
      float tmax = float.MaxValue;

      for (int dim = 0; dim < 3; dim++) {
        if (Math.Abs (ray.Direction [dim]) < Precission) {
          // parallel to X face
          if (ray.Position [dim] < box.Minimum [dim] || ray.Position [dim] > box.Maximum [dim]) {
            distance = 0.0f;
            return false;
          }
        } else {
          // ray eq:
          // p = p0 + t * d
          // t = (p.x - p0.x)/d.x

          float tin = (box.Minimum [dim] - ray.Position [dim]) / ray.Direction [dim];
          float tout = (box.Maximum [dim] - ray.Position [dim]) / ray.Direction [dim];

          if (tin > tout) {
            float aux = tin;
            tin = tout;
            tout = aux;
          }
          t = Math.Max (tin, t);
          tmax = Math.Min (tout, tmax);
          if (t > tmax) {
            // is out of another dim when entering this one.
            distance = 0.0f;
            return false;
          }
        }
      }
      distance = t;
      return true;
    }

    public static bool Intersect(Ray ray, BoundingSphere sphere, out float distance){
      Vector3 diff = sphere.Center - ray.Position;
      float dd = (diff).LengthSquared;
      float rr = sphere.Radius * sphere.Radius;

      // check if ray point is inside sphere
      if ( dd < rr){
        distance = 0.0f;
        return true;
      }

      float dot = Vector3.Dot (diff, ray.Direction);
      if (dot < 0.0) {
        distance = 0.0f;
        return false;
      }

      // temp = ||diff||^2 - ||diff||^2 cos^2(theta)
      // temp = ||diff||^2 sin^2(theta)
      float temp = dd - dot * dot;
      if (temp > rr) {
        distance = 0.0f;
        return false;
      }

      distance = dot - (float)Math.Sqrt(rr - temp);
      return true;
    }

    public static bool Intersect(Ray ray, Plane plane, out float distance){
      // ray:   {x | x = p + t * d} 
      // plane: {x | x dot n + D = 0} 
      // solve for t:
      //    (p + t * d) dot n + D = 0
      //     (t * d) dot n = (-(p dot n)-D) / (d dot n)

      float dotDir = Vector3.Dot(ray.Direction , plane.Normal);
      if (Math.Abs (dotDir) < Precission) {
        distance = 0;
        return false;
      }
      float dotPos = Vector3.Dot(ray.Position , plane.Normal);
      float t = (-dotPos - plane.D) / dotDir;

      if (t < 0.0f) {
        if (t < -Precission) {
          distance = 0;
          return false;
        } else {
          // consider it intersection at 0
          t = 0.0f;
        }
      }
      distance = t;
      return true;
    }
  }
}

