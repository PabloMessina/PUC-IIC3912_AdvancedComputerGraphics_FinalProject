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
    /// Interaction logic for UpperToolView.xaml
    /// </summary>
    public partial class TopToolView : UserControl
    {
        private UniverseSimulatorController _controller;

        public TopToolView(UniverseSimulatorController controller)
        {
            InitializeComponent();
            _controller = controller;
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Hide()
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Show()
        {
            this.Visibility = System.Windows.Visibility.Visible;
        }
                
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
                _controller.PreviousCamera();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
                _controller.ResetCamera();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_controller != null)
                _controller.NextCamera();
        }
        
    }


}
