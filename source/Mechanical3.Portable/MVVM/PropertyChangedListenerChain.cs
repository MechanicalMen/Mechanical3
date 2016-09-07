using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Mechanical3.Core;

namespace Mechanical3.MVVM
{
#pragma warning disable SA1600 // ElementsMustBeDocumented
    internal class PropertyChangedListenerChain : DisposableObject, IPropertyChangedListener
    {
        #region Private Fields

        private PropertyInfo propertyInfo;
        private PropertyChangedSource previousPropertyValue;

        #endregion

        #region Constructor

        internal PropertyChangedListenerChain()
            : base()
        {
            this.previousPropertyValue = new PropertyChangedSource();
        }

        #endregion

        #region IDisposableObject

        /// <summary>
        /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        protected override void OnDispose( bool disposing )
        {
            if( disposing )
            {
                //// dispose-only (i.e. non-finalizable) logic
                //// (managed, disposable resources you own)

                if( this.previousPropertyValue.NotNullReference() )
                {
                    this.previousPropertyValue.Dispose();
                    this.previousPropertyValue = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)
            this.propertyInfo = null;

            base.OnDispose(disposing);
        }

        #endregion

        #region IPropertyChangedListener

        public void OnPropertyChanged( INotifyPropertyChanged declaringTypeInstance, string propertyName )
        {
            try
            {
                if( this.propertyInfo.NullReference() )
                {
                    var declaringTypeInfo = declaringTypeInstance.GetType().GetTypeInfo();

                    var p = declaringTypeInfo.GetDeclaredProperty(propertyName);
                    if( p.NullReference() )
                        throw new Exception("Invalid property: property not found!").StoreFileLine();

                    if( !p.CanRead )
                        throw new Exception("Invalid property: can not read!").StoreFileLine();

                    if( !p.GetMethod.IsPublic )
                        throw new Exception("Invalid property: not public!").StoreFileLine();

                    if( p.PropertyType.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(i => i == typeof(INotifyPropertyChanged)).NullReference() )
                        throw new Exception("Invalid property: property type does not implement INotifyPropertyChanged!").StoreFileLine();

                    this.propertyInfo = p;
                }

                //// signal property changed:
                ////  - if the property value has changed
                ////  - even if the value has not changed, this event may indicate that it's state has,
                ////    and therefore it's properties being listened to may have as well, so we should propagate this
                ////  - however, if the value was previously null, and it still is, there is obviously no state that could have changed

                INotifyPropertyChanged newPropertyValue;
                if( declaringTypeInstance.NullReference() )
                    newPropertyValue = null;
                else
                    newPropertyValue = (INotifyPropertyChanged)this.propertyInfo.GetMethod.Invoke(declaringTypeInstance, null);

                var lastPropertyValue = this.previousPropertyValue.Source;
                if( !(lastPropertyValue.NullReference() && newPropertyValue.NullReference()) )
                {
                    this.previousPropertyValue.Source = newPropertyValue; // implicitly notifies listeners if it changed

                    if( object.ReferenceEquals(lastPropertyValue, newPropertyValue) ) // explicitly notify listeners, even if there was no change, but the state is not null
                        this.previousPropertyValue.Listeners.NotifyAllPropertiesChanged(newPropertyValue);
                }
            }
            catch( Exception ex )
            {
                ex.Store("declaringType", declaringTypeInstance?.GetType());
                ex.Store(nameof(propertyName), propertyName);
                throw;
            }
        }

        #endregion

        #region Internal Members

        internal PropertyChangedListenerCollection Listeners
        {
            get
            {
                this.ThrowIfDisposed();

                return this.previousPropertyValue.Listeners;
            }
        }

        #endregion
    }
#pragma warning restore SA1600 // ElementsMustBeDocumented
}
