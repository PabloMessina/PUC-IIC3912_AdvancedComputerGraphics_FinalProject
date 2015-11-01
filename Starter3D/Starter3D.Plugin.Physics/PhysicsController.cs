using System;
using System.Linq;
using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;

namespace Starter3D.Plugin.SimpleMaterialEditor
{
  public class SimpleMaterialEditorController : IController
  {
    private const string ScenePath = @"scenes/physicsScene.xml";
    private const string ResourcePath = @"resources/physicsResources.xml";

    private readonly IRenderer _renderer;
    private readonly ISceneReader _sceneReader;
    private readonly IResourceManager _resourceManager;

    private readonly IScene _scene;
    
    
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
      get { return null; }
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
      get { return "Physics"; }
    }

    public SimpleMaterialEditorController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
    {
      if (renderer == null) throw new ArgumentNullException("renderer");
      if (sceneReader == null) throw new ArgumentNullException("sceneReader");
      if (resourceManager == null) throw new ArgumentNullException("resourceManager");
      _renderer = renderer;
      _sceneReader = sceneReader;
      _resourceManager = resourceManager;

      _resourceManager.Load(ResourcePath);
      _scene = _sceneReader.Read(ScenePath);
      
    }
    
    public void Load()
    {
      InitRenderer();

      _resourceManager.Configure(_renderer);

      var materialInstancing = _resourceManager.GetMaterial("redSpecularInstanced");

      var shape = _scene.Shapes.First();
      var mesh = shape.Shape as IMesh;

      var instancedMesh = new InstancedMesh(mesh.Name, mesh);
      instancedMesh.Material = materialInstancing;
      for (float i = 0; i < 1; i+= 0.1f)
      {
        instancedMesh.AddInstance(Matrix4.CreateTranslation(new Vector3(i, 0, 0)));
      }

      shape.Shape = instancedMesh;
      _scene.Configure(_renderer);

    }

    private void InitRenderer()
    {
      _renderer.SetBackgroundColor(0.9f,0.9f,1.0f);
      _renderer.EnableZBuffer(true);
      _renderer.EnableWireframe(false);
      _renderer.SetCullMode(CullMode.None);
    }

    public void Render(double time)
    {
      _scene.Render(_renderer);
    }

    public void Update(double time)
    {
      
    }

    public void UpdateSize(double width, double height)
    {
      var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
      if (perspectiveCamera != null)
        perspectiveCamera.AspectRatio = (float)(width / height);
    }

    public void MouseWheel(int delta, int x, int y)
    {

    }

    public void MouseDown(ControllerMouseButton button, int x, int y)
    {
 
    }

    public void MouseUp(ControllerMouseButton button, int x, int y)
    {
    
    }

    public void MouseMove(int x, int y, int deltaX, int deltaY)
    {
    
    }

    public void KeyDown(int key)
    {
      
    }

  }
}