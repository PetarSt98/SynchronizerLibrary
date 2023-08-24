using Microsoft.EntityFrameworkCore;
using SynchronizerLibrary.Data;
using SynchronizerLibrary.Loggers;
using SynchronizerLibrary.Loggers;
using System.Net;
using System.Net.Mail;
using SynchronizerLibrary.SOAPservices;

namespace SynchronizerLibrary.DataBuffer
{
    public class DatabaseSynchronizator
    {
        private GlobalInstance globalInstance;
        private Dictionary<string, RAP_ResourceStatus> databaseStatusUpdater;
        private Dictionary<string, bool> partialStatus;
        private string username = "pstojkov";
        private string password = "GeForce9800GT.";
        public DatabaseSynchronizator()
        {
            globalInstance = GlobalInstance.Instance;
        }

        public void AverageGatewayReults()
        {
            databaseStatusUpdater = new Dictionary<string, RAP_ResourceStatus>();
            partialStatus = new Dictionary<string, bool>();

            foreach (var name in globalInstance.Names)
            {
                foreach (var obj in globalInstance.ObjectLists[name])
                {
                    var key = $"{obj.ComputerName}-{obj.GroupName}";
                    if (!databaseStatusUpdater.ContainsKey(key))
                    {
                        databaseStatusUpdater[key] = obj;
                        partialStatus[key] = obj.Status;
                    }
                    else
                    {
                        databaseStatusUpdater[key].Status &= obj.Status;
                        partialStatus[key] |= obj.Status;
                    }
                }
            }
        }

        public void UpdateDatabase()
        {
            bool sendEmail = true;
            using (var db = new RapContext())
            {
                foreach (var pair in databaseStatusUpdater.Zip(partialStatus, (item, partial) => (item, partial)))
                {
                    var obj = pair.item.Value;
                    if (!pair.item.Value.Status)
                    {
                        if (sendEmail)
                        {
                            string logMessage = $"Unsuccessfully synchronized Local Group: {obj.GroupName} and Device: {obj.ComputerName}";
                            LoggerSingleton.Raps.Warn(logMessage);
                            //string body = logMessage;

                            Dictionary<string, string> deviceInfo = Task.Run(() => SOAPMethods.ExecutePowerShellSOAPScript(obj.ComputerName, username, password)).Result;

                            string firstName = deviceInfo["UsersName"]; // Dodaj ime
                            firstName = firstName.ToLower(); // Convert the entire string to lowercase first
                            char firstLetter = char.ToUpper(firstName[0]); // Convert the first character to uppercase
                            firstName = firstLetter + firstName.Substring(1);
                            string users = obj.GroupName.Replace("Lg-", "");
                            string RemoteMachine = obj.ComputerName;

                            // Load the HTML template from a file or from a string, 
                            // then replace the placeholders with the actual values
                            string template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestFailed.htm");  // Replace with actual path
                            template = template.Replace("$firstName", firstName);
                            template = template.Replace("$users", users);
                            template = template.Replace("$RemoteMachine", RemoteMachine);

                            // Now use the template as the body of your email
                            string toAddress = obj.GroupName.Replace("LG-", "") + "@cern.ch";
                            //string toAddressCC = resource.resourceOwner.Replace(@"CERN\", "") + "@cern.ch";
                            string toAddressCC = null;
                            string subject = "noreply - Remote Desktop Service Synchronization Notification";
                            string body = template;



                            SendEmail(toAddress, toAddressCC, subject, body);

                            if (!pair.partial.Value)
                            {
                                var rapResourcesToDelete = db.rap_resource.Where(rr => (rr.RAPName == obj.GroupName.Replace("LG-", "RAP_") && string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase))).ToList();
                                db.rap_resource.RemoveRange(rapResourcesToDelete);

                                LoggerSingleton.General.Warn($"Deleting unsynchronized RAP_Resource RAP_Name: {obj.GroupName.Replace("LG-", "RAP_")} resourceName: {obj.ComputerName} from MySQL database");
                                LoggerSingleton.Raps.Warn($"Deleting unsynchronized RAP_Resource RAP_Name: {obj.GroupName.Replace("LG-", "RAP_")} resourceName: {obj.ComputerName} from MySQL database");
                                Console.WriteLine($"Deleting unsynchronized RAP_Resource RAP_Name: {obj.GroupName.Replace("LG-", "RAP_")} resourceName: {obj.ComputerName} from MySQL database");

                                db.SaveChanges();
                            }

                        }
                        continue;
                    }
                    Console.WriteLine($"  ComputerName: {obj.ComputerName}, GroupName: {obj.GroupName}");

                    // Selecting matching raps
                    var matchingRaps = db.raps
                        .Where(r => r.resourceGroupName == obj.GroupName)
                        .Include(r => r.rap_resource)  // Including related rap_resources
                        .ToList();

                    // Filtering unsynchronized raps and their resources
                    var unsynchronizedRaps = matchingRaps
                        .Where(r => r.rap_resource.Any(rr => !rr.synchronized && string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase) && !rr.toDelete))
                        .ToList();

                    foreach (var unsynchronizedRap in unsynchronizedRaps)
                    {
                        foreach (var resource in unsynchronizedRap.rap_resource.Where(rr => string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase)))
                        {
                            resource.synchronized = true;
                            string logMessage = $"Successfully synchronized User: {resource.RAPName.Replace("RAP_", "")} and Device: {resource.resourceName}";
                            LoggerSingleton.Raps.Info(logMessage);
                            if (sendEmail)
                            {
                                // Prepare the email
                                string toAddress = resource.RAPName.Replace("RAP_", "") + "@cern.ch";
                                string toAddressCC = resource.resourceOwner.Replace(@"CERN\", "") + "@cern.ch";
                                string subject = "noreply - Remote Desktop Service Synchronization Notification";
                                //string body = logMessage;

                                Dictionary<string, string> deviceInfo = Task.Run(() => SOAPMethods.ExecutePowerShellSOAPScript(obj.ComputerName, username, password)).Result;
                                // ako ja dodam maria ima da posalje njemu mejla al ce reci Dear Petar mesto mario
                                string firstName = deviceInfo["UsersName"]; // Dodaj ime
                                firstName = firstName.ToLower(); // Convert the entire string to lowercase first
                                char firstLetter = char.ToUpper(firstName[0]); // Convert the first character to uppercase
                                firstName = firstLetter + firstName.Substring(1);
                                string users = obj.GroupName.Replace("Lg-", "");
                                string RemoteMachine = obj.ComputerName;

                                // Load the HTML template from a file or from a string, 
                                // then replace the placeholders with the actual values
                                string template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestSucceeded.htm");  // Replace with actual path
                                template = template.Replace("$firstName", firstName);
                                template = template.Replace("$users", users);
                                template = template.Replace("$RemoteMachine", RemoteMachine);
                                string body = template;

                                SendEmail(toAddress, toAddressCC, subject, body);
                            }
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        public void SendEmail(string toAddress,string toAddressCC, string subject, string body)
        {

            MailMessage message = new MailMessage();
            message.From = new MailAddress("noreply@cern.ch");

            message.To.Add(new MailAddress(toAddress));
            if (toAddressCC != null)
                message.CC.Add(new MailAddress(toAddressCC));
            message.Bcc.Add(new MailAddress("cernts-tsgateway-admin@cern.ch"));

            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            SmtpClient client = new SmtpClient("cernmx.cern.ch");
            client.Send(message);
            Console.WriteLine($"Send and email to {toAddress}");
        }

    }

}