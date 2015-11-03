using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Starter3D.Plugin.UniverseSimulator
{
    /// <summary>
    /// Lógica de interacción para LeftToolView.xaml
    /// </summary>
    public partial class LeftToolView : UserControl
    {
        private UniverseSimulatorController _controller;

        public LeftToolView(UniverseSimulatorController controller)
        {
            InitializeComponent();
            _controller = controller;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
            {
                _controller.ChangeMode(Mode.Navigate);
                _controller.SetTooltip("Left click to drag around");
            }

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
            {
                _controller.ChangeMode(Mode.Insert);
                _controller.SetTooltip("Left click insert a new planet");
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
            {
                _controller.ChangeMode(Mode.Pick);
                _controller.SetTooltip("Left click to select a planet. Hold down to move it around");
            }
        }
        
      
    }
}
