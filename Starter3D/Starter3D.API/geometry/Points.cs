using System.Collections.Generic;
using System.Linq;
using OpenTK;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using System;

namespace Starter3D.API.geometry
{
  public class Points : IPoints
  {
    private IMaterial _material;
    private readonly string _name;

    private float _pointSize;

    private List<IVertex> _vertices = new List<IVertex>();

    public IMaterial Material
    {
      get { return _material; }
      set { _material = value; }
    }

    public string Name
    {
      get { return _name; }
    }

    public Points(string name, float pointSize)
    {
      _name = name;
      _pointSize = pointSize;
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
      if (_vertices.Count > 0)
      {
        _material.Configure(renderer);
        renderer.LoadObject(_name);
        renderer.SetVerticesData(_name, GetVerticesData());
        renderer.SetIndexData(_name, GetIndexData());
        _vertices.First().Configure(_name, _material.Shader.Name, renderer);
      }
    }

    public void Update(IRenderer renderer)
    {
      renderer.UpdateVerticesData(_name, GetVerticesData());
      renderer.UpdateIndexData(_name, GetIndexData());
    }

    public void Render(IRenderer renderer, Matrix4 transform)
    {
      if (_vertices.Count > 0)
      {
        _material.Render(renderer);
        renderer.SetMatrixParameter("modelMatrix", transform, _material.Shader.Name);
        renderer.DrawPoints(_name, _vertices.Count, _pointSize);
      }
    }

    public void AddPoint(IVertex vertex)
    {
      _vertices.Add(vertex);
    }

    public void Clear()
    {
      _vertices.Clear();
    }

    private List<Vector3> GetVerticesData()
    {
      var data = new List<Vector3>();
      foreach (var vertex in _vertices)
      {
        vertex.AppendData(data);
      }
      return data;
    }

    private List<int> GetIndexData()
    {
      var data = new List<int>();
      for (int i = 0; i < _vertices.Count; i++)
      {
        data.Add(i);
      }
      return data;
    }

    public IShape Clone()
    {
        throw new NotImplementedException();
    }
  }
}