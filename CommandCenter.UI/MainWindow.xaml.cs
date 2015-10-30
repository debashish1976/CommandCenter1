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
using Path = System.IO.Path;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace CommandCenter.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dispatcher _dispatcher;

        public MainWindow()
        {
            InitializeComponent();
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        private void Provision_Click(object sender, RoutedEventArgs e)
        {
            MessageLog.Clear();
            ProvisionTenant(TenantName.Text);
        }

        private void ProvisionTenant(string tenantName)
        {
            Provision.IsEnabled = false;
            var provisioner = new TenantProvisioner();
            provisioner.OnUpdateStatus +=provisioner_OnUpdateStatus;
            provisioner.OnProvisionComplete += provisioner_OnProvisionComplete;
            provisioner.ProvisionTenant(tenantName);
        }

        void provisioner_OnProvisionComplete(object sender, ProgressEventArgs e)
        {
            _dispatcher.Invoke(
                () =>
                {
                    MessageLog.AppendText(e.Status);
                    MessageLog.AppendText(Environment.NewLine);
                    MessageLog.ScrollToEnd();

                    Provision.IsEnabled = true;
                });
        }

        private void provisioner_OnUpdateStatus(object sender, ProgressEventArgs e)
        {
            _dispatcher.Invoke(
                () =>
                {
                    MessageLog.AppendText(e.Status);
                    MessageLog.AppendText(Environment.NewLine);
                    MessageLog.ScrollToEnd();
                });
        }
    }
}
