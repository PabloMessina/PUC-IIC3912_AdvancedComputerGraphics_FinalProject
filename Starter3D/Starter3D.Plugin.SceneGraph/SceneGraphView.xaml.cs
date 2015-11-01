using System.Windows.Controls;

namespace Starter3D.Plugin.SceneGraph
{
  /// <summary>
  /// Interaction logic for MaterialEditorUI.xaml
  /// </summary>
  public partial class SceneGraphView : UserControl
  {
    public SceneGraphView(SceneGraphController controller)
    {
      InitializeComponent();
      this.DataContext = controller;

    }
  }
}

