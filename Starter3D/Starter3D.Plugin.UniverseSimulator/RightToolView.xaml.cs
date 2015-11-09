using Starter3D.API.resources;
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
    /// Lógica de interacción para RightToolView.xaml
    /// </summary>
    public partial class RightToolView : UserControl
    {
        public RightToolView()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).Save();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).Reset();
        }

    }
}
