using Microsoft.Win32;
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
                var btn = (System.Windows.Controls.Primitives.ToggleButton)sender;
                _controller.SetMode((Mode)btn.Tag);
            }
        }

        public void Hide()
        {
            this.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Show()
        {
            this.Visibility = System.Windows.Visibility.Visible;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.AddExtension = true;
            dlg.Filter = "Text documents (.xml)|*.xml";
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\saved_scenes";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                _controller.SaveSceneAsXML(dlg.FileName);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.AddExtension = true;
            dlg.Filter = "Text documents (.xml)|*.xml";
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\saved_scenes";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                _controller.LoadSceneFromXmlFile(dlg.FileName);
            }
        }
    }
}
