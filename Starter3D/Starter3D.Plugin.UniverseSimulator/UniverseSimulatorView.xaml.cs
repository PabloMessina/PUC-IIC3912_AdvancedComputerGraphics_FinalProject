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
    /// Lógica de interacción para UniverseControllerView.xaml
    /// </summary>
    public partial class UniverseControllerView : UserControl
    {
        private UniverseSimulatorController _controller;

        public UniverseControllerView(UniverseSimulatorController controller)
        {
            InitializeComponent();
            _controller = controller;
        }
                

    }
}
