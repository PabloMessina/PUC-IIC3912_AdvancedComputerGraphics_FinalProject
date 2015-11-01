using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;

namespace Starter3D.Plugin.CurveEditor
{
    public class CurveEditorController : IController
    {
      private const string ResourcePath = @"resources/curveEditorResources.xml";

      private readonly IRenderer _renderer;
      private readonly IResourceManager _resourceManager;
      private readonly CurveEditorView _view;

      private ICurve _curve;
      private IPoints _points;

      private double _width;
      private double _height;

      private bool _isFirstTime = true;

      public CurveEditorController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
      {
        _renderer = renderer;
        _resourceManager = resourceManager;

        _resourceManager.Load(ResourcePath);

        _view = new CurveEditorView();
        _view.DataContext = this;
      }

      public int Width
      {
        get { return 800; }
      }

      public int Height
      {
        get { return 600; }
      }

      public bool IsFullScreen
      {
        get { return true; }
      }

      public object CentralView
      {
        get { return _view; }
      }

      public object LeftView
      {
        get { return null; }
      }

      public object RightView
      {
        get { return null; }
      }

      public object TopView
      {
        get { return null; }
      }

      public object BottomView
      {
        get { return null; }
      }

      public bool HasUserInterface
      {
        get { return true; }
      }

      public string Name
      {
        get { return "Curve Editor"; }
      }

      public void Load()
      {
        InitRenderer();

        _resourceManager.Configure(_renderer);

        _curve = new Curve("curve", 2);
        _curve.Material = _resourceManager.GetMaterial("lineMaterial");
        _curve.Configure(_renderer);

        _points = new Points("points", 5);
        _points.Material = _resourceManager.GetMaterial("pointMaterial");
        
        _points.Configure(_renderer);

        
      }

      private void InitRenderer()
      {
        _renderer.SetBackgroundColor(0.0f, 0.0f, 1.0f);
        _renderer.EnableZBuffer(false);
        _renderer.EnableWireframe(false);
        _renderer.SetCullMode(CullMode.None);
      }

      public void Render(double deltaTime)
      {
        _curve.Render(_renderer, Matrix4.Identity);
        _points.Render(_renderer, Matrix4.Identity);
      }

      public void Update(double deltaTime)
      {
        
      }

      public void MouseDown(ControllerMouseButton button, int x, int y)
      {
        float adjustedX = (2.0f*(float) x/(float) _width) - 1;
        float adjustedY = (2.0f*(float) (_height - y)/(float) _height) - 1;
        var mousePoint = new Vector3(adjustedX, adjustedY, 0);

        _curve.AddPoint(new Vertex(mousePoint, new Vector3(), new Vector3()));
        //_points.AddPoint(new Vertex(mousePoint, new Vector3(), new Vector3()));

        if (_isFirstTime)
        {
          _curve.Configure(_renderer);
          //_points.Configure(_renderer);
          _isFirstTime = false;
        }
        else
        {
          _curve.Update(_renderer);
          //_points.Update(_renderer);
        }
      }

      public void MouseUp(ControllerMouseButton button, int x, int y)
      {
        
      }

      public void MouseWheel(int delta, int x, int y)
      {
        
      }

      public void MouseMove(int x, int y, int deltaX, int deltaY)
      {
        
      }

      public void KeyDown(int key)
      {
        
      }

      public void UpdateSize(double width, double height)
      {
        _width = width;
        _height = height;
      }
    }
}
