using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.UI
{
    public class TenantProvisioner
    {
        public delegate void StatusUpdateHandler(object sender, ProgressEventArgs e);
        public event StatusUpdateHandler OnUpdateStatus;
        public event StatusUpdateHandler OnProvisionComplete;


        public void ProvisionTenant(string tenantName)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(DoProvisionTenant), tenantName);
        }

        public void DoProvisionTenant(object tenantName)
        {
            try
            {
                string siteName = "mtws-" + tenantName;
                string siteAdmin = "mtws-admin";


                UpdateStatus("Preparing client environment");
                // updated to remove the need for .pubsettings file
                var cmd = "$subscriptionID = '01efbbcd-874b-49b6-9b69-dd126bd9afd9'" + Environment.NewLine
                    + "$thumbPrint = 'F0392364B94C5F8A50AC15041C21D8D97FE71CA6'" + Environment.NewLine
                    + @"$certificate = Get-Item cert:\\CurrentUser\My\$thumbprint" + Environment.NewLine
                    + "Set-AzureSubscription -SubscriptionName $subscriptionName -SubscriptionId $subscriptionID -Certificate $certificate" + Environment.NewLine
                    + "Select-AzureSubscription -SubscriptionName $subscriptionName";

                // delimit the comman with quotes
                ExecutePSCommand("\"" + cmd + "\"");



       
                cmd = "$env:Path=" + "'C:\\Program Files (x86)\\Git\\cmd;'" + " + $env:Path";
                ExecutePSCommand(cmd);

                

                UpdateStatus("Creating website..");
                cmd = "New-AzureWebsite -Name " + siteName
                    + " -Location 'East US' -Git -PublishingUserName " + siteAdmin;

                ExecutePSCommand(cmd);

                UpdateStatus("Copying the template site..");

                UpdateStatus("Added tenant to settings..");

                cmd = "$settings = New-Object Hashtable" + Environment.NewLine
                    + "$settings['tenant']='" + tenantName + "'" + Environment.NewLine
                    + "Set-AzureWebsite -AppSettings $settings " + siteName;
                ExecutePSCommand(cmd);

                var tempDir = Path.Combine(Path.GetTempPath(), siteName);
                Directory.CreateDirectory(tempDir);

                cmd = "Set-Location -Path '" + tempDir + "' -PassThru";
                ExecutePSCommand(cmd);

                cmd = "Git init";
                ExecutePSCommand(cmd);

                cmd = "Git pull https://mtws-admin:password123@mtws-sitetemplate.scm.azurewebsites.net/MTWS-SiteTemplate.git";
                ExecutePSCommand(cmd);

                cmd = string.Format("Git remote add azure https://{0}:password123@{1}.scm.azurewebsites.net/{1}.git", siteAdmin, siteName);
                ExecutePSCommand(cmd);

                UpdateStatus("Pushing to tenant site");

                cmd = "Git push azure master";
                ExecutePSCommand(cmd);

                NotifyComplete("Tenat provisioning completed !");
            }
            catch (Exception ex)
            {
                NotifyComplete("Tenant provisioning failed !" + Environment.NewLine + "Error : " + ex.ToString());
            }
        }

        private void ExecutePSCommand(string command)
        {
            var output = command.ExecutePS();
            UpdateStatus(output);
        }

        private void UpdateStatus(string status)
        {
            if (OnUpdateStatus == null) return;

            ProgressEventArgs args = new ProgressEventArgs(status);
            OnUpdateStatus(this, args);
        }

        private void NotifyComplete(string status)
        {
            if (OnProvisionComplete == null) return;

            ProgressEventArgs args = new ProgressEventArgs(status);
            OnProvisionComplete(this, args);
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public string Status { get; private set; }

        public ProgressEventArgs(string status)
        {
            Status = status;
        }
    }
}
