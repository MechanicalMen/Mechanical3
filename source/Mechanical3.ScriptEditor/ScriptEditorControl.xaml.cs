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
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptEditorControl"/> class.
        /// </summary>
        public ScriptEditorControl()
        {
            this.InitializeComponent();

            this.codeEditor.TextChanged += this.codeEditor_TextChanged;
            this.SetBinding(CodeProperty, new Binding("DataContext.Code") { Source = this, Mode = BindingMode.TwoWay });
        }

        /// <summary>
        /// Identifies the <see cref="ScriptEditorControl.Code"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
            name: "Code",
            propertyType: typeof(string),
            ownerType: typeof(ScriptEditorControl),
            typeMetadata: new PropertyMetadata(
                defaultValue: null,
                propertyChangedCallback: OnCodeChanged));

        /// <summary>
        /// Gets or sets the code displayed in the editor.
        /// </summary>
        /// <value>The code displayed in the editor.</value>
        public string Code
        {
            get { return (string)this.GetValue(CodeProperty); }
            set { this.SetValue(CodeProperty, value); }
        }

        private static void OnCodeChanged( DependencyObject source, DependencyPropertyChangedEventArgs e )
        {
            var control = (ScriptEditorControl)source;
            var newCode = (string)e.NewValue;

            if( !control.SuppressCodeChanged )
            {
                control.SuppressCodeEditorTextChanged = true;
                control.codeEditor.Text = newCode;
                control.SuppressCodeEditorTextChanged = false;
            }
        }

        private void codeEditor_TextChanged( object sender, EventArgs e )
        {
            this.SuppressCodeChanged = true;
            this.Code = this.codeEditor.Text;
            this.SuppressCodeChanged = false;
        }

        private bool SuppressCodeEditorTextChanged
        {
            set
            {
                if( value )
                    this.codeEditor.TextChanged -= this.codeEditor_TextChanged;
                else
                    this.codeEditor.TextChanged += this.codeEditor_TextChanged;
            }
        }

        private bool SuppressCodeChanged { get; set; } = false;

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
