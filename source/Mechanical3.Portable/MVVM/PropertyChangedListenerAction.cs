using System;
using System.ComponentModel;
using Mechanical3.Core;
using Mechanical3.Misc;

namespace Mechanical3.MVVM
{
#pragma warning disable SA1600 // ElementsMustBeDocumented
    internal class PropertyChangedListenerAction : IPropertyChangedListener
    {
        private readonly Action action;
        private readonly FileLineInfo registrationPos;

        internal PropertyChangedListenerAction( Action a, FileLineInfo srcPos )
        {
            if( a.NullReference() )
                throw new ArgumentNullException(nameof(a)).StoreFileLine();

            this.action = a;
            this.registrationPos = srcPos;
        }

        public void OnPropertyChanged( INotifyPropertyChanged source, string propertyName )
        {
            try
            {
                this.action();
            }
            catch( Exception ex )
            {
                ex.StoreFileLine(this.registrationPos);
                MechanicalApp.EnqueueException(ex);
            }
        }
    }
#pragma warning restore SA1600 // ElementsMustBeDocumented
}
