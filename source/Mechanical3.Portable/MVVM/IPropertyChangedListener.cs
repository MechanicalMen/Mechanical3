using System.ComponentModel;

namespace Mechanical3.MVVM
{
#pragma warning disable SA1600 // ElementsMustBeDocumented
    internal interface IPropertyChangedListener
    {
        void OnPropertyChanged( INotifyPropertyChanged source, string propertyName );
    }
#pragma warning restore SA1600 // ElementsMustBeDocumented
}
