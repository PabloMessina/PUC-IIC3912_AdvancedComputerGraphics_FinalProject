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

        private void RotateVelRight_Click(object sender, RoutedEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).RotateVelocityRight();
        }

        private void RotateVelLeft_Click(object sender, RoutedEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).RotateVelocityLeft();
        }

        public void Hide()
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Show()
        {
            this.Visibility = System.Windows.Visibility.Visible;
        }

        private void RotateUp_Click(object sender, MouseButtonEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).RotateBodyUp();
        }

        private void RotateDown_Click(object sender, MouseButtonEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).RotateBodyDown();
        }

        private void RotateRight_Click(object sender, MouseButtonEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).RotateBodyRight();
        }

        private void RotateLeft_Click(object sender, MouseButtonEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).RotateBodyLeft();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ((CelestialBodyViewModel)DataContext).Delete();
        }


    }
}
