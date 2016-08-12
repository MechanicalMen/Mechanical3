using System.ComponentModel;

namespace Mechanical3.MVVM
{
    internal interface IPropertyChangedListener
    {
        void OnPropertyChanged( INotifyPropertyChanged source, string propertyName );
    }
}
