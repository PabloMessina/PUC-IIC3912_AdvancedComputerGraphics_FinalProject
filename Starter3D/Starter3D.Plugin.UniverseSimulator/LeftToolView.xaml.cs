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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
            {
                var btn = (Button)sender;
                _controller.SetMode((Mode)btn.Tag);
            }
        }       
      
    }
}
