using Microsoft.EntityFrameworkCore;
using SynchronizerLibrary.Data;
using SynchronizerLibrary.Loggers;
using SynchronizerLibrary.Loggers;
using System.Net;
using System.Net.Mail;
using SynchronizerLibrary.SOAPservices;
using System.Text.Json;
using SynchronizerLibrary.DataBuffer;
using SynchronizerLibrary.RDPFile;


namespace SynchronizerLibrary.EmailService
{
    public class EmailBuilder
    {
        protected string subject;
        protected string template;
        protected string remoteMachine;
        protected string users;
        protected string body;
        protected string toAddress;
        protected string toAddressCC;
        protected string username = "pstojkov";
        protected string password = "GeForce9800GT.";
        protected string rdpContent = null;
        protected SpamFailureHandler cacheData;
        public EmailBuilder()
        {

        }

        //public EmailBuilder(RAP_ResourceStatus? obj, RapContext db)
        //{
        //    Console.WriteLine("Sending email");
        //    string logMessage = $"Unsuccessfully synchronized Local Group: {obj.GroupName} and Device: {obj.ComputerName}";
        //    LoggerSingleton.Raps.Warn(logMessage);
        //    //string body = logMessage;

        //    Dictionary<string, string> deviceInfo = Task.Run(() => SOAPMethods.ExecutePowerShellSOAPScript(obj.ComputerName, obj.GroupName.Replace("LG-", ""), username, password)).Result;
        //    if (deviceInfo != null)
        //    {
        //        Console.WriteLine(deviceInfo["UserGivenName"]);
        //        string firstName = deviceInfo["UserGivenName"]; // Dodaj ime
        //                                                        //firstName = firstName.ToLower(); // Convert the entire string to lowercase first
        //                                                        //char firstLetter = char.ToUpper(firstName[0]); // Convert the first character to uppercase

        //        //firstName = firstLetter + firstName.Substring(1);
        //        users = obj.GroupName.Replace("LG-", "");
        //        remoteMachine = obj.ComputerName;

        //        // Load the HTML template from a file or from a string, 
        //        // then replace the placeholders with the actual values

        //        Console.WriteLine(remoteMachine);
        //        Console.WriteLine(users);

        //        if (obj.StatusMessage != null)
        //        {
        //            Console.WriteLine("Status");
        //            Console.WriteLine(obj.StatusMessage);

        //            if (obj.StatusMessage == "Access is denied.")
        //            {
        //                subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
        //                template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
        //                template = template.Replace("$reason", "TS Gateway does not have access to your device.");
        //                template = template.Replace("$raison", "TS Gateway n'a pas accès à votre appareil.");
        //                uncompletedSync = true;
        //            }
        //            else if (obj.StatusMessage == "There is no such object on the server.")
        //            {
        //                subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
        //                template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
        //                template = template.Replace("$reason", $"The device {remoteMachine} cannot be found on the server.");
        //                template = template.Replace("$raison", "L'appareil n'est pas trouvé sur le serveur.");
        //                uncompletedSync = true;
        //            }
        //            else if (obj.StatusMessage.Contains("disabled"))
        //            {
        //                subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
        //                template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
        //                template = template.Replace("$reason", "Administrator user in Local User and Groups on your machine is disabled.");
        //                template = template.Replace("$raison", "L'utilisateur administrateur est désactivé dans le groupe local d'utilisateurs et de groupes de votre machine.");
        //                uncompletedSync = true;
        //            }
        //            else if (obj.StatusMessage == "Device is unreachable.")
        //            {
        //                subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
        //                template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
        //                template = template.Replace("$reason", "Your device is not reachable by TS Gateway.");
        //                template = template.Replace("$raison", "Votre appareil n'est pas accessible par TS Gateway.");
        //                uncompletedSync = true;
        //            }
        //            else if (obj.StatusMessage == "The network path was not found.")
        //            {
        //                subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
        //                template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
        //                template = template.Replace("$reason", "Your device was offline when TS Gateway tried to reach it.");
        //                template = template.Replace("$raison", "Votre appareil était hors ligne lorsque TS Gateway a essayé de le joindre.");
        //                uncompletedSync = true;
        //            }
        //            else
        //            {
        //                subject = "no-reply Remote Desktop Service device synchronization Failed";
        //                template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestFailed.htm");  // Replace with actual path
        //            }
        //        }
        //        else
        //        {
        //            subject = "no-reply Remote Desktop Service device synchronization Failed";
        //            Console.WriteLine("No Status");
        //            template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestFailed.htm");
        //        }
        //        template = template.Replace("$firstName", firstName);
        //        template = template.Replace("$users", users);
        //        template = template.Replace("$RemoteMachine", remoteMachine);

        //        // Now use the template as the body of your email
        //        toAddress = obj.GroupName.Replace("LG-", "") + "@cern.ch";
        //        //string toAddressCC = resource.resourceOwner.Replace(@"CERN\", "") + "@cern.ch";
        //        toAddressCC = null;
        //        if (deviceInfo["ResponsiblePersonUsername"].Length != 0)
        //        {
        //            toAddressCC = deviceInfo["ResponsiblePersonUsername"] + "@cern.ch";
        //        }
        //        body = template;


        //        if (uncompletedSync)
        //        {
        //            var matchingRapsUncompleted = db.raps
        //                .Where(r => r.resourceGroupName == obj.GroupName)
        //                .Include(r => r.rap_resource)  // Including related rap_resources
        //                .ToList();

        //            // Filtering unsynchronized raps and their resources
        //            var unsynchronizedRapsUncompleted = matchingRapsUncompleted
        //                .Where(r => r.rap_resource.Any(rr => !rr.synchronized && string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase) && !rr.toDelete))
        //                .ToList();
        //            foreach (var unsynchronizedRap in unsynchronizedRapsUncompleted)
        //            {
        //                foreach (var resource in unsynchronizedRap.rap_resource.Where(rr => string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase)))
        //                {
        //                    resource.synchronized = true;
        //                    resource.exception = true;
        //                }
        //            }
        //            SpamFailureHandler.CleanCache(remoteMachine, users);
        //        }
        //        else
        //        {
        //            var cacheData = new SpamFailureHandler(remoteMachine, users);
        //            cacheData.CacheSpam();
        //        }
        //    }

        //}

        public void Send()
        {

            Console.WriteLine(subject);
            if (!SpamFailureHandler.CheckStatus(remoteMachine, users, true))
            {
                Console.WriteLine(subject);
                _SendEmail(toAddress, toAddressCC, subject, body, remoteMachine, rdpContent);

            }
        }

        private void _SendEmail(string toAddress, string toAddressCC, string subject, string body, string deviceName, string rdpContent)
        {
            try
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

                if (rdpContent != null)
                {
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rdpContent)))
                    {
                        Attachment attachment = new Attachment(stream, $"{deviceName}.rdp", "application/octet-stream");
                        message.Attachments.Add(attachment);
                        SmtpClient client = new SmtpClient("cernmx.cern.ch");
                        client.Send(message);
                        Console.WriteLine($"Send an email to {toAddress}");
                    }
                }
                else
                {
                    SmtpClient client = new SmtpClient("cernmx.cern.ch");
                    client.Send(message);
                    Console.WriteLine($"Send an email to {toAddress}");
                    cacheSpams();

                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void cacheSpams()
        {
            cacheData.CacheSpam();
        }
    }

    public class UnsuccessfulEmail : EmailBuilder
    {
        public bool uncompletedSync = false;
        public UnsuccessfulEmail(RAP_ResourceStatus? obj, RapContext db)
        {
            Console.WriteLine("Sending email");
            string logMessage = $"Unsuccessfully synchronized Local Group: {obj.GroupName} and Device: {obj.ComputerName}";
            LoggerSingleton.Raps.Warn(logMessage);
            //string body = logMessage;

            Dictionary<string, string> deviceInfo = Task.Run(() => SOAPMethods.ExecutePowerShellSOAPScript(obj.ComputerName, obj.GroupName.Replace("LG-", ""), username, password)).Result;
            if (deviceInfo != null)
            {
                Console.WriteLine(deviceInfo["UserGivenName"]);
                string firstName = deviceInfo["UserGivenName"]; // Dodaj ime
                                                                //firstName = firstName.ToLower(); // Convert the entire string to lowercase first
                                                                //char firstLetter = char.ToUpper(firstName[0]); // Convert the first character to uppercase

                //firstName = firstLetter + firstName.Substring(1);
                users = obj.GroupName.Replace("LG-", "");
                remoteMachine = obj.ComputerName;

                // Load the HTML template from a file or from a string, 
                // then replace the placeholders with the actual values

                Console.WriteLine(remoteMachine);
                Console.WriteLine(users);

                if (obj.StatusMessage != null)
                {
                    Console.WriteLine("Status");
                    Console.WriteLine(obj.StatusMessage);

                    if (obj.StatusMessage == "Access is denied.")
                    {
                        subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
                        template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
                        template = template.Replace("$reason", "TS Gateway does not have access to your device.");
                        template = template.Replace("$raison", "TS Gateway n'a pas accès à votre appareil.");
                        uncompletedSync = true;
                    }
                    else if (obj.StatusMessage == "There is no such object on the server.")
                    {
                        subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
                        template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
                        template = template.Replace("$reason", $"The device {remoteMachine} cannot be found on the server.");
                        template = template.Replace("$raison", "L'appareil n'est pas trouvé sur le serveur.");
                        uncompletedSync = true;
                    }
                    else if (obj.StatusMessage.Contains("disabled"))
                    {
                        subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
                        template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
                        template = template.Replace("$reason", "Administrator user in Local User and Groups on your machine is disabled.");
                        template = template.Replace("$raison", "L'utilisateur administrateur est désactivé dans le groupe local d'utilisateurs et de groupes de votre machine.");
                        uncompletedSync = true;
                    }
                    else if (obj.StatusMessage == "Device is unreachable.")
                    {
                        subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
                        template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
                        template = template.Replace("$reason", "Your device is not reachable by TS Gateway.");
                        template = template.Replace("$raison", "Votre appareil n'est pas accessible par TS Gateway.");
                        uncompletedSync = true;
                    }
                    else if (obj.StatusMessage == "The network path was not found.")
                    {
                        subject = "no-reply Remote Desktop Service device synchronization Uncompleted";
                        template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestUncompleted.htm");  // Replace with actual path
                        template = template.Replace("$reason", "Your device was offline when TS Gateway tried to reach it.");
                        template = template.Replace("$raison", "Votre appareil était hors ligne lorsque TS Gateway a essayé de le joindre.");
                        uncompletedSync = true;
                    }
                    else
                    {
                        subject = "no-reply Remote Desktop Service device synchronization Failed";
                        template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestFailed.htm");  // Replace with actual path
                    }
                }
                else
                {
                    subject = "no-reply Remote Desktop Service device synchronization Failed";
                    Console.WriteLine("No Status");
                    template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestFailed.htm");
                }
                template = template.Replace("$firstName", firstName);
                template = template.Replace("$users", users);
                template = template.Replace("$RemoteMachine", remoteMachine);

                // Now use the template as the body of your email
                toAddress = obj.GroupName.Replace("LG-", "") + "@cern.ch";
                //string toAddressCC = resource.resourceOwner.Replace(@"CERN\", "") + "@cern.ch";
                toAddressCC = null;
                if (deviceInfo["ResponsiblePersonUsername"].Length != 0)
                {
                    toAddressCC = deviceInfo["ResponsiblePersonUsername"] + "@cern.ch";
                }
                body = template;

                
                if (uncompletedSync)
                {
                    rdpContent = RdpFile.CustomizeRdpFile(remoteMachine);
                }

                if (uncompletedSync)
                {
                    var matchingRapsUncompleted = db.raps
                        .Where(r => r.resourceGroupName == obj.GroupName)
                        .Include(r => r.rap_resource)  // Including related rap_resources
                        .ToList();

                    // Filtering unsynchronized raps and their resources
                    var unsynchronizedRapsUncompleted = matchingRapsUncompleted
                        .Where(r => r.rap_resource.Any(rr => !rr.synchronized && string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase) && !rr.toDelete))
                        .ToList();
                    foreach (var unsynchronizedRap in unsynchronizedRapsUncompleted)
                    {
                        foreach (var resource in unsynchronizedRap.rap_resource.Where(rr => string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase)))
                        {
                            resource.synchronized = true;
                            resource.exception = true;
                        }
                    }
                    SpamFailureHandler.CleanCache(remoteMachine, users);
                }
                else
                {
                    cacheData = new SpamFailureHandler(remoteMachine, users);
                }
            }
        }
    }

    public class SuccessfulEmail : EmailBuilder
    {
        public SuccessfulEmail(RAP_ResourceStatus? obj, RapContext db)
        {
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

                     // Prepare the email
                    string toAddress = resource.RAPName.Replace("RAP_", "") + "@cern.ch";
                    string toAddressCC = resource.resourceOwner.Replace(@"CERN\", "") + "@cern.ch";
                    string subject = "no-reply Remote Desktop Service device synchronization Success";
                    //string body = logMessage;
                    Dictionary<string, string> deviceInfo = Task.Run(() => SOAPMethods.ExecutePowerShellSOAPScript(obj.ComputerName, resource.RAPName.Replace("RAP_", ""), username, password)).Result;

                    string firstName = deviceInfo["UserGivenName"]; // Dodaj ime
                                                                    //firstName = firstName.ToLower(); // Convert the entire string to lowercase first
                                                                    //char firstLetter = char.ToUpper(firstName[0]); // Convert the first character to uppercase
                                                                    //firstName = firstLetter + firstName.Substring(1);
                    string users = obj.GroupName.Replace("LG-", "");
                    string remoteMachine = obj.ComputerName;
                    // Load the HTML template from a file or from a string, 
                    // then replace the placeholders with the actual values
                    string template = System.IO.File.ReadAllText(@".\DataBuffer\EmailTemplates\User_RequestSucceeded.htm");  // Replace with actual path
                    template = template.Replace("$firstName", firstName);
                    template = template.Replace("$users", users);
                    template = template.Replace("$RemoteMachine", remoteMachine);
                    string body = template;

                    Console.WriteLine(subject);
                    rdpContent = RdpFile.CustomizeRdpFile(remoteMachine);
                    SpamFailureHandler.CleanCache(remoteMachine, users);
                }
            }
        }
    }
}
