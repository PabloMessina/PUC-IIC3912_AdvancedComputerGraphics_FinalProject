using OpenTK;

namespace Starter3D.API.geometry
{
  public interface IInstancedMesh : IShape
  {
    void AddInstance(Matrix4 instanceMatrix);
  }
}