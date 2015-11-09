using System.ComponentModel;

namespace Starter3D.Plugin.UniverseSimulator
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(string.Empty));
        }
        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
