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
        private UniverseSimulatorController _controller;
        private Planet _planet;
        private List<Control> _inputs;

        public RightToolView(UniverseSimulatorController controller)
        {
            InitializeComponent();
            _controller = controller;
            
            //Agregar todos los inputs a la lista
            _inputs = new List<Control>();
            _inputs.Add(this.NameBox);
            _inputs.Add(this.Mass);
            _inputs.Add(this.Radius);
            _inputs.Add(this.VelocityVectorX);
            _inputs.Add(this.VelocityVectorY);
            _inputs.Add(this.VelocityVectorZ);
            _inputs.Add(this.VelocityMagnitude);
            _inputs.Add(this.HasGravity);

            _inputs.Add(this.Materials);
            _inputs.Add(this.SaveButton);
        }

        public void SetMaterials(IEnumerable<IMaterial> materials)
        {
            foreach (IMaterial material in materials)
            {
                this.Materials.Items.Add(material.Name);
            }
        }

        public void SetPlanet(Planet planet)
        {
            _planet = planet;
            ActivatePlanetEditMenu();
            EnableAll();
        }

        public void UnsetPlanet()
        {
            _planet = null;
            DisableAll();
        }

        private void ActivatePlanetEditMenu()
        {
            this.NameBox.Text = _planet.Name;
            this.Radius.Text = "" + _planet.Radius;
            this.Mass.Text = "" + _planet.Mass;            
            
            this.VelocityVectorX.Text = "" + _planet.Velocity.Normalized().X;
            this.VelocityVectorY.Text = "" + _planet.Velocity.Normalized().Y;
            this.VelocityVectorZ.Text = "" + _planet.Velocity.Normalized().Z;
            this.VelocityMagnitude.Text = "" + _planet.Velocity.Length;

            this.HasGravity.IsChecked = _planet.Gravity;

            this.Materials.SelectedItem = _planet.Material.Name;
        }

        private void SavePlanet(object sender, RoutedEventArgs e)
        {
            //Falta validar valores correctos         
            _planet.UpdatePlanet(ToFloat(this.Radius.Text), ToFloat(this.Mass.Text), new OpenTK.Vector3(ToFloat(this.VelocityVectorX.Text), ToFloat(this.VelocityVectorY.Text), ToFloat(this.VelocityVectorZ.Text)), ToFloat(this.VelocityMagnitude.Text), (bool)this.HasGravity.IsChecked, this.NameBox.Text);
            _planet.UpdateMaterial(_controller.GetMaterial(this.Materials.SelectedItem.ToString()));
            _controller.SetTooltip("Correctly Saved");
        }

        private void DisableAll()
        {
            foreach (Control input in _inputs)
            {
                input.IsEnabled = false;
            }
        }

        private void EnableAll()
        {
            foreach (Control input in _inputs)
            {
                input.IsEnabled = true;
            }
        }

        //Transformar string en float
        public float ToFloat(string s)
        {
            return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture); 
        }

    }
}
