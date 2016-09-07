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
using Mechanical3.MVVM;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// Interaction logic for ScriptCommandView.xaml
    /// </summary>
    public partial class ScriptCommandView : UserControl
    {
        private readonly PropertyChangedActions propertyChanged = new PropertyChangedActions();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCommandView"/> class.
        /// </summary>
        public ScriptCommandView()
        {
            InitializeComponent();

            this.DataContextChanged += this.OnDataContextChanged;
        }

        private void OnDataContextChanged( object sender, DependencyPropertyChangedEventArgs e )
        {
            try
            {
                this.propertyChanged.Clear();

                var vm = this.VM;
                if( vm.NotNullReference() )
                {
                    this.RebuildScriptCommandParameters();
                    this.propertyChanged.Register(vm, nameof(vm.ScriptCommand), this.RebuildScriptCommandParameters);
                }
            }
            catch( Exception ex )
            {
                MechanicalApp.EnqueueException(ex);
            }
        }

        private ScriptCommandViewModel VM
        {
            get { return (ScriptCommandViewModel)this.DataContext; }
        }

        private void RebuildScriptCommandParameters()
        {
            var cmd = this.VM?.ScriptCommand;
            if( cmd.NullReference() )
                return;

            this.scriptCommandParametersGrid.Children.Clear();
            this.scriptCommandParametersGrid.RowDefinitions.Clear();

            int rowIndex = 0;
            foreach( var parameter in cmd.Parameters )
            {
                var label = new TextBlock();
                label.Text = parameter.DisplayName;
                label.VerticalAlignment = VerticalAlignment.Center;

                UIElement control;
                if( parameter is ScriptCommandParameter.Text )
                {
                    var p = (ScriptCommandParameter.Text)parameter;
                    var ctrl = new TextBox();
                    ctrl.DataContext = p;
                    ctrl.SetBinding(TextBox.TextProperty, new Binding("Value") { Mode = BindingMode.TwoWay });
                    ctrl.SetBinding(TextBox.AcceptsReturnProperty, "IsMultiLine");
                    control = ctrl;
                }
                else if( parameter is ScriptCommandParameter.Boolean )
                {
                    var p = (ScriptCommandParameter.Boolean)parameter;
                    var ctrl = new CheckBox();
                    ctrl.DataContext = p;
                    ctrl.IsThreeState = false;
                    ctrl.SetBinding(CheckBox.IsCheckedProperty, new Binding("Value") { Mode = BindingMode.TwoWay });
                    control = ctrl;
                }
                else if( parameter is ScriptCommandParameter.SingleChoice )
                {
                    var p = (ScriptCommandParameter.SingleChoice)parameter;
                    var ctrl = new ComboBox();
                    ctrl.DataContext = p;
                    ctrl.IsEditable = false;
                    ctrl.SetBinding(ComboBox.ItemsSourceProperty, "Items");
                    ctrl.SelectedIndex = 0;
                    ctrl.SetBinding(ComboBox.SelectedIndexProperty, new Binding("SelectedIndex") { Mode = BindingMode.TwoWay });
                    control = ctrl;
                }
                else if( parameter is ScriptCommandParameter.MultipleChoice )
                {
                    var p = (ScriptCommandParameter.MultipleChoice)parameter;
                    var ctrl = new ComboBox();
                    var mainUserControlGrid = (Grid)this.scriptCommandParametersGrid.Parent;
                    ctrl.Style = (Style)mainUserControlGrid.Resources["MultiSelectComboBoxStyle"];
                    ctrl.DataContext = p;
                    ctrl.SetBinding(ComboBox.ItemsSourceProperty, nameof(p.Items));
                    ctrl.SetBinding(ComboBox.TagProperty, nameof(p.CommaSeparatedSelectedItems));

                    // setup selection change handler
                    // (strong reference kept implicitly through the registered event handler delegates)
                    new MultipleChoiceSelectionHandler(ctrl, p);

                    control = ctrl;
                }
                else
                {
                    control = new TextBlock() { Text = $"Unknwon type: {parameter.GetType().FullName}" };
                }

                // insert vertical separator
                if( this.scriptCommandParametersGrid.RowDefinitions.Count != 0 )
                {
                    this.scriptCommandParametersGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(pixels: 4) });
                    ++rowIndex;
                }

                // add to generated controls to grid
                this.scriptCommandParametersGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                this.scriptCommandParametersGrid.Children.Add(label);
                Grid.SetRow(label, rowIndex);
                Grid.SetColumn(label, 0);
                this.scriptCommandParametersGrid.Children.Add(control);
                Grid.SetRow(control, rowIndex);
                Grid.SetColumn(control, 2);
                ++rowIndex;
            }
        }

        #region MultipleChoiceSelectionHandler

        private class MultipleChoiceSelectionHandler
        {
            private readonly ComboBox comboBox;
            private readonly ScriptCommandParameter.MultipleChoice parameter;
            private readonly ListBox listBox;

            internal MultipleChoiceSelectionHandler( ComboBox ctrl, ScriptCommandParameter.MultipleChoice p )
            {
                this.comboBox = ctrl;
                this.parameter = p;

                this.comboBox.ApplyTemplate();
                this.listBox = ((ListBox)this.comboBox.Template.FindName("lstBox", this.comboBox)); // null before ApplyTemplate

                // add event handlers
                this.listBox.SelectionChanged += this.OnMultiSelectListBox_SelectionChanged;
                this.parameter.SelectedIndexes.CollectionChanged += this.OnMultipleChoice_CollectionChanged;
                this.comboBox.Unloaded += OnComboBox_Unloaded;

                // overwrite listbox initial selection
                this.OnMultipleChoice_CollectionChanged(null, null);
            }

            private void OnComboBox_Unloaded( object sender, RoutedEventArgs e )
            {
                // remove event handlers
                this.listBox.SelectionChanged -= this.OnMultiSelectListBox_SelectionChanged;
                this.parameter.SelectedIndexes.CollectionChanged -= this.OnMultipleChoice_CollectionChanged;
                this.comboBox.Unloaded -= this.OnComboBox_Unloaded;
            }

            private void OnMultiSelectListBox_SelectionChanged( object sender, SelectionChangedEventArgs e )
            {
                var selectedChoiceIndexes = this.listBox.SelectedItems.Cast<string>().Select(item => this.parameter.Items.IndexOf(item, startIndex: 0, equalityComparer: StringComparer.Ordinal)).ToArray();

                this.parameter.SelectedIndexes.CollectionChanged -= this.OnMultipleChoice_CollectionChanged;
                this.parameter.SelectedIndexes.Clear();
                foreach( var i in selectedChoiceIndexes )
                    this.parameter.SelectedIndexes.Add(i);
                this.parameter.SelectedIndexes.CollectionChanged += this.OnMultipleChoice_CollectionChanged;
            }

            private void OnMultipleChoice_CollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
            {
                var selectedChoiceIndexes = this.parameter.SelectedIndexes.ToArray();

                this.listBox.SelectionChanged -= this.OnMultiSelectListBox_SelectionChanged;
                this.listBox.SelectedItems.Clear();
                foreach( var i in selectedChoiceIndexes )
                    this.listBox.SelectedItems.Add(this.parameter.Items[i]);
                this.listBox.SelectionChanged += this.OnMultiSelectListBox_SelectionChanged;
            }
        }

        #endregion
    }
}
