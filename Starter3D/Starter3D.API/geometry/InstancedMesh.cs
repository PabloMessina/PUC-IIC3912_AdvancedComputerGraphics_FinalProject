using System.Collections.Generic;
using OpenTK;
using Starter3D.API.renderer;
using Starter3D.API.resources;

namespace Starter3D.API.geometry
{
  public class InstancedMesh : IInstancedMesh
  {
    private IMaterial _material;
    private readonly string _name;
    private readonly IMesh _mesh;

    private readonly List<Matrix4> _instanceMatrices = new List<Matrix4>();

    public string Name
    {
      get { return _name; }
    }

    public IMaterial Material
    {
      get { return _material; }
      set { _material = value; }
    }

    public InstancedMesh(string name, IMesh mesh)
    {
      _name = name;
      _mesh = mesh;
    }

    public void Load(string filePath)
    {
      throw new System.NotImplementedException();
    }

    public void Save(string filePath)
    {
      throw new System.NotImplementedException();
    }

    public void Configure(IRenderer renderer)
    {
      _material.Configure(renderer);
      _mesh.Configure(renderer);
      renderer.SetInstanceData(_name, _instanceMatrices);
      renderer.SetInstanceAttribute(_name, _material.Shader.Name, 0, "instanceMatrix", 4 * Vector4.SizeInBytes, Vector4.SizeInBytes);
    }

    public void Update(IRenderer renderer)
    {
      _mesh.Update(renderer);
      renderer.UpdateInstanceData(_name, _instanceMatrices);
    }

    public void Render(IRenderer renderer, Matrix4 modelTransform)
    {
      _material.Render(renderer);
      renderer.SetMatrixParameter("modelMatrix", modelTransform, _material.Shader.Name);
      renderer.DrawInstancedTriangles(_name, _mesh.GetTriangleCount(), _instanceMatrices.Count);
    }

    public void AddInstance(Matrix4 instanceMatrix)
    {
      _instanceMatrices.Add(instanceMatrix);
    }
  }
}