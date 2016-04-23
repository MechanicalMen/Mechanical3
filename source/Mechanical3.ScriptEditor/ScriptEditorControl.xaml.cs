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
using Mechanical3.Core;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// Interaction logic for ScriptEditorControl.xaml
    /// </summary>
    public partial class ScriptEditorControl : UserControl
    {
        private ScriptEditorViewModel vm = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptEditorControl"/> class.
        /// </summary>
        public ScriptEditorControl()
        {
            this.InitializeComponent();

            this.DataContextChanged += ( s, e ) =>
            {
                var oldVM = this.vm;
                var newVM = this.DataContext as ScriptEditorViewModel;
                this.vm = newVM;

                if( oldVM.NotNullReference() )
                    oldVM.PropertyChanged -= this.OnVM_PropertyChanged;

                if( newVM.NotNullReference() )
                {
                    newVM.PropertyChanged += this.OnVM_PropertyChanged;
                    this.OnVM_PropertyChanged(newVM, new System.ComponentModel.PropertyChangedEventArgs(nameof(ScriptEditorViewModel.Code)));
                }
            };
        }

        private void OnVM_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( string.Equals(e.PropertyName, nameof(ScriptEditorViewModel.Code), StringComparison.Ordinal)
             && object.ReferenceEquals(this.vm, sender) )
                this.codeEditor.Text = this.vm.Code;
        }

        private async void codeEditor_KeyDown( object sender, KeyEventArgs e )
        {
            try
            {
                if( (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                 && e.Key == Key.Enter )
                {
                    // Ctrl + Enter
                    e.Handled = true;

                    var vm = this.DataContext as ScriptEditorViewModel;
                    if( vm.NotNullReference() )
                    {
                        vm.Code = this.codeEditor.Text;
                        await vm.RunCodeAsync();
                    }
                }
            }
            catch( Exception ex )
            {
                MechanicalApp.EnqueueException(ex);
            }
        }
    }
}
