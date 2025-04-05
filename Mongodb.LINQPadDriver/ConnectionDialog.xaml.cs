using System;
using System.IO;
using System.Linq;
using System.Windows;
using LINQPad.Extensibility.DataContext;

namespace MongoDB.LINQPadDriver
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
    {
        private readonly IConnectionInfo _cxInfo;

        public ConnectionDialog(IConnectionInfo cxInfo)
        {
            if (string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.CustomCxString))
            {
                cxInfo.DatabaseInfo.CustomCxString = "mongodb://mongo:27017";
            }
            _cxInfo = cxInfo;
            DataContext = cxInfo;
            InitializeComponent();

            var debugElement =_cxInfo.DriverData.Element("Debug");
            if (debugElement != null)
            {
                chkDebug.IsChecked = (bool)debugElement;
            }


        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_cxInfo.DatabaseInfo.Server))
            {
                MessageBox.Show("Document Type Namespace must be specified");
                DialogResult = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(_cxInfo.DatabaseInfo.Database))
            {
                MessageBox.Show("Database must be specified");
                DialogResult = null;
                return;
            }

            _cxInfo.DriverData.SetElementValue("Debug", chkDebug.IsChecked);

            DialogResult = true;
        }

        private void BrowseAssembly(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Choose custom assembly",
                DefaultExt = ".dll",
                Filter = "Assemblies|*.dll|Source Code|*.cs|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _cxInfo.CustomTypeInfo.CustomAssemblyPath = dialog.FileName;
            }
        }

        private void ChooseNamespace(object sender, RoutedEventArgs e)
        {
            //Debugger.Launch();
            var assemPath = _cxInfo.CustomTypeInfo.CustomAssemblyPath;
            if (assemPath.Length == 0)
            {
                MessageBox.Show("First enter a path to an assembly.");
                return;
            }

            if (!File.Exists(assemPath))
            {
                MessageBox.Show("File '" + assemPath + "' does not exist.");
                return;
            }

            string[] customTypes;
            try
            {
                customTypes =
                    (from t in _cxInfo.CustomTypeInfo.GetCustomTypesInAssembly()
                     let ts = t.Split('.')
                     select string.Join('.', ts.SkipLast(1))).ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error obtaining custom types: " + ex.Message);
                return;
            }

            if (customTypes.Length == 0)
            {
                MessageBox.Show("There are no public types in that assembly.");  // based on.........
                return;
            }

            var result = (string)LINQPad.Extensibility.DataContext.UI.Dialogs.PickFromList("Choose Namespace", customTypes);
            if (result != null)
            {
                _cxInfo.DatabaseInfo.Server = result;
            }
        }

        private void BrowseAppConfig(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "Choose application config file",
                DefaultExt = ".config",
            };

            if (dialog.ShowDialog() == true)
            {
                _cxInfo.AppConfigPath = dialog.FileName;
            }
        }
    }
}
