using System.ComponentModel;
using System.Threading;
using System.Windows;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.Misc;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// Helps dealing with Visual Studio's designer.
    /// </summary>
    public static class WpfDesigner
    {
        //// NOTE: The designer only instantiates the view being displayed.
        ////       Initialization code in other files is not run (like App.xaml.cs or MainWindow.xaml.cs)

        #region ExceptionReporter

        private class ExceptionReporter : IEventHandler<UnhandledExceptionEvent>
        {
            public void Handle( UnhandledExceptionEvent evnt )
            {
                MessageBox.Show(
                    SafeString.DebugPrint(evnt.Exception),
                    "Design time Mechanical3 exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK);
            }
        }

        #endregion

        private static IUIThreadHandler uiThreadHandler = null;
        private static ExceptionReporter exceptionReporter;

        static WpfDesigner()
        {
            IsInDesigner = DesignerProperties.GetIsInDesignMode(new DependencyObject());
        }

        /// <summary>
        /// Gets a value indicating whether the current code is executed by the visual studio designer.
        /// </summary>
        /// <value><c>true</c> if the current code runs in the designer; otherwise, <c>false</c>.</value>
        public static bool IsInDesigner { get; }

        /// <summary>
        /// Does basic Mechanical3 initialization, when first set to <c>true</c> in the designer.
        /// </summary>
        public static readonly DependencyProperty InitializeDesignerMechanical3Property = DependencyProperty.RegisterAttached(
            "InitializeDesignerMechanical3",
            typeof(bool),
            typeof(WpfDesigner),
            new PropertyMetadata(defaultValue: false));

        /// <summary>
        /// Gets the value of the <see cref="InitializeDesignerMechanical3Property"/> attached property.
        /// </summary>
        /// <param name="element">The element from which to read the property value.</param>
        /// <returns>The property value read.</returns>
        public static bool GetInitializeDesignerMechanical3( UIElement element )
        {
            return (bool)element.GetValue(InitializeDesignerMechanical3Property);
        }

        /// <summary>
        /// Sets the value of the <see cref="InitializeDesignerMechanical3Property"/> attached property.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetInitializeDesignerMechanical3( UIElement element, bool value )
        {
            if( value != GetInitializeDesignerMechanical3(element) )
            {
                element.SetValue(InitializeDesignerMechanical3Property, value);
                Mechanical3DesignerInitialization();
            }
        }

        /// <summary>
        /// If invoked from the Designer, for the first time, then it does basic <see cref="MechanicalApp"/> initialization.
        /// Does not do anything otherwise.
        /// </summary>
        public static void Mechanical3DesignerInitialization()
        {
            if( IsInDesigner
             && uiThreadHandler.NullReference() )
            {
                // make sure the initialization code is executed on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if( Interlocked.CompareExchange(ref uiThreadHandler, ThreadSynchronizationContextUIHandler.FromCurrent(), comparand: null).NullReference() )
                    {
                        MechanicalApp.Initialize(
                            uiThreadHandler,
                            new TaskEventQueue(),
                            logUnhandledExceptionEvents: false);

                        WpfHelper.LogAppDomainExceptions();
                        WpfHelper.EnqueueDispatcherExceptionsFrom(Application.Current);

                        exceptionReporter = new ExceptionReporter();
                        MechanicalApp.EventQueue.Subscribe(exceptionReporter);
                    }
                });
            }
        }
    }
}
