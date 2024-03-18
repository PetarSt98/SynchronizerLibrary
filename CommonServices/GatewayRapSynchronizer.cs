﻿using System.DirectoryServices;
using System.Management;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System.Security;
using System.Text.Json;
using System.Text;
using SynchronizerLibrary.Loggers;
using SynchronizerLibrary.Caching;
using SynchronizerLibrary.DataBuffer;

namespace SynchronizerLibrary.CommonServices
{
    public class GatewayRapSynchronizer : IGatewayRapSynchronizer
    {
        private const string AdSearchGroupPath = "WinNT://{0}/{1},group";
        private const string NamespacePath = @"\root\CIMV2\TerminalServices";
        private readonly DirectoryEntry _rootDir = new DirectoryEntry("LDAP://DC=cern,DC=ch");

        public GatewayRapSynchronizer() { }

        public async Task<List<string>> GetGatewaysRapNamesAsync(string serverName, bool cacheFlag = false)
        {
            Console.WriteLine($"Getting RAP/Policy names from gateway '{serverName}'.");
            LoggerSingleton.General.Info($"Getting RAP/Policy names from gateway '{serverName}'.");
            LoggerSingleton.SynchronizedRaps.Info($"Getting RAP/Policy names from gateway '{serverName}'.");
            try
            {
                return await GetRapNamesAsync(serverName, cacheFlag);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed getting RAP/Policy names from gateway '{serverName}'.");
                LoggerSingleton.SynchronizedRaps.Error(ex, $"Failed getting RAP/Policy names from gateway '{serverName}'.");
                LoggerSingleton.General.Error(ex, $"Failed getting RAP/Policy names from gateway '{serverName}'.");
                throw;
            }
        }

        public async Task<List<string>> GetRapNamesAsync(string serverName, bool cacheFlag = false)
        {
            if (cacheFlag)
            {
                Console.WriteLine($"Using cached Gateway Policies");
                return Cacher.LoadPolicyCacheFromFile(serverName);
            }
            else
            {
                try
                {
                    Console.WriteLine($"Preparing to downloaod gateway policies");
                    return QueryGatewayRapNames(serverName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to downloaod gateway policies");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message);
                    throw ex;
                }
            }
        }

        private List<string> QueryGatewayRapNames(string serverName)
        {
            Console.WriteLine($"Start Querying '{serverName}'.");
            //var username = "";
            //var password = "";
            //var securepassword = new SecureString();
            //foreach (char c in password)
            //    securepassword.AppendChar(c);
            const string AdSearchGroupPath = "WinNT://{0}/{1},group";
            const string NamespacePath = @"\root\CIMV2\TerminalServices";
            string _oldGatewayServerHost = $@"\\{serverName}.cern.ch";
            int fatalCounter = 0;
            while (true)
            {
                try
                {
                    const string osQuery = "SELECT * FROM Win32_TSGatewayResourceAuthorizationPolicy";
                    //CimCredential Credentials = new CimCredential(PasswordAuthenticationMechanism.Default, "cern.ch", username, securepassword);

                    //WSManSessionOptions SessionOptions = new WSManSessionOptions();
                    //SessionOptions.AddDestinationCredentials(Credentials);
                    //CimSession mySession = CimSession.Create(serverName, SessionOptions);
                    CimSession mySession = CimSession.Create(serverName);

                    Console.WriteLine($"Connecting to gateway policies on '{serverName}'.");
                    var queryInstanceTask = mySession.QueryInstances(_oldGatewayServerHost + NamespacePath, "WQL", osQuery);
                    IEnumerable<CimInstance> queryInstance = queryInstanceTask;

                    var rapNames = new List<string>();
                    Console.WriteLine($"Querying '{serverName}'.");
                    LoggerSingleton.SynchronizedRaps.Info($"Started querying RAP/Policy names from gateway '{serverName}'.");
                    var i = 1;
                    foreach (CimInstance x in queryInstance)
                    {
                        var policyName = x.CimInstanceProperties["Name"].Value.ToString();
                        LoggerSingleton.SynchronizedRaps.Debug($"{i} - Querying RAP/Policy {policyName} from gateway '{serverName}'.");
                        rapNames.Add(policyName);
                        i++;
                    }
                    LoggerSingleton.SynchronizedRaps.Info($"Finished querying RAP/Policy names from gateway '{serverName}'.");

                    Cacher.SavePolicyCacheToFile(rapNames, serverName);
                    return rapNames;
                }
                catch (Exception ex)
                {
                    fatalCounter++;
                    LoggerSingleton.General.Error(ex, $"Error while getting rap names from gateway: '{serverName}'. Ex: {ex}");
                    LoggerSingleton.SynchronizedRaps.Error(ex, $"Error while getting rap names from gateway: '{serverName}'. Ex: {ex}");
                    LoggerSingleton.SynchronizedRaps.Info($"Retry querying NO {fatalCounter}");
                    LoggerSingleton.General.Info($"Retry querying NO {fatalCounter}");
                    Console.WriteLine($"Error while getting rap names from gateway: '{serverName}'. Ex: {ex}");
                    Console.WriteLine($"Retry querying NO {fatalCounter}");
                    if (fatalCounter == 10) break;
                }
            }
            LoggerSingleton.General.Fatal($"Failed to query policies from gateway {serverName}");
            LoggerSingleton.SynchronizedRaps.Fatal($"Failed to query policies from gateway {serverName}");
            LoggerSingleton.SynchronizedRaps.Error($"Failed to query policies from gateway {serverName}");
            Console.WriteLine($"Failed to query policies from gateway {serverName}");
            return new List<string>();
        }

        public async Task SynchronizeRaps(string serverName, List<string> allGatewayGroups, List<string> toDeleteGatweayGroups, List<string> gatewayRaps)
        {
            LoggerSingleton.General.Info($"Starting Policy synchronisation of server: '{serverName}'.");
            LoggerSingleton.SynchronizedRaps.Info($"Starting Policy synchronisation of server: '{serverName}'.");
            var modelRapNamesAdd = allGatewayGroups.Select(LgNameToRapName).ToList();
            var modelRapNamesDelete = toDeleteGatweayGroups.Select(LgNameToRapName).ToList();
            await AddMissingRaps(serverName, modelRapNamesAdd, gatewayRaps);
            await DeleteObsoleteRaps(serverName, modelRapNamesDelete, gatewayRaps);
            LoggerSingleton.General.Info($"Finished Policy synchronisation of server: '{serverName}'.");
            LoggerSingleton.SynchronizedRaps.Info($"Finished Policy synchronisation of server: '{serverName}'.");
        }
        private static string LgNameToRapName(string lgName)
        {
            return lgName.Replace("LG-", "RAP_");
        }
        private async Task DeleteObsoleteRaps(string serverName, List<string> modelRapNames, List<string> gatewayRaps)
        {
            var obsoleteRapNames = gatewayRaps.Intersect(modelRapNames).ToList();
            //_reporter.SetShouldDeleteRaps(serverName, obsoleteRapNames.Count);
            LoggerSingleton.General.Info($"Server:{serverName} Deleting {obsoleteRapNames.Count} RAPs from the gateway.");
            LoggerSingleton.SynchronizedRaps.Info($"Deleting {obsoleteRapNames.Count} RAPs from the gateway '{serverName}'.");
            if (obsoleteRapNames.Count > 0)
                await TryDeletingRaps(serverName, obsoleteRapNames);
            //_reporter.Info(serverName, "Finished deleting RAPs.");
            LoggerSingleton.General.Info($"Finished deleting RAPs from the gateway '{serverName}'.");
            LoggerSingleton.SynchronizedRaps.Info($"Finished deleting RAPs from the gateway '{serverName}'.");
        }

        private async Task AddMissingRaps(string serverName, List<string> modelRapNames, List<string> gatewayRapNames)
        {
            var missingRapNames = modelRapNames.Except(gatewayRapNames).ToList();
            //_reporter.SetShouldAddRaps(serverName, missingRapNames.Count);
            //_reporter.Info(serverName, $"Adding {missingRapNames.Count} RAPs to the gateway.");
            LoggerSingleton.General.Info($"Adding {missingRapNames.Count} RAPs to the gateway '{serverName}'.");
            LoggerSingleton.SynchronizedRaps.Info($"Adding {missingRapNames.Count} RAPs to the gateway '{serverName}'.");
            await AddMissingRaps(serverName, missingRapNames);
            //_reporter.Info(serverName, "Finished adding RAPs.");
            LoggerSingleton.General.Info($"Finished adding {missingRapNames.Count} RAPs to the gateway '{serverName}'.");
            LoggerSingleton.SynchronizedRaps.Info($"Finished adding {missingRapNames.Count} RAPs to the gateway '{serverName}'.");
        }
        private async Task TryDeletingRaps(string serverName, List<string> obsoleteRapNames)
        {
            bool finished = false;
            int counter = 0;
            var toDelete = new List<string>(obsoleteRapNames);
            while (!(counter == 30 || finished))
            {
                if (toDelete.Count == 0) break;
                
                var response = await DeleteRapsFromGateway(serverName, toDelete);
                Console.WriteLine($"Deleting raps, try #{counter + 1}"); //TODO delete
                LoggerSingleton.SynchronizedRaps.Debug($"Deleting raps, try #{counter + 1}");
                foreach (var res in response)
                {
                    if (res.Deleted)
                    {
                        //_reporter.IncrementDeletedRaps(serverName);
                        Console.WriteLine($"Deleted '{res.RapName}'.");
                        LoggerSingleton.SynchronizedRaps.Debug($"Deleted '{res.RapName}'.");
                        if (toDelete.Count == 0) finished = true;
                    }
                    else
                    {
                        Console.WriteLine($"Failed deleting '{res.RapName}'."); //TODO delete
                        LoggerSingleton.SynchronizedRaps.Error($"Failed deleting '{res.RapName}'.");
                    }
                }
                toDelete = toDelete.Except(response.Where(r => r.Deleted).Select(r => r.RapName)).ToList();
                counter++;
            }
        }
        public async Task<bool> AddMissingRaps(string serverName, List<string> missingRapNames)
        {
            var sHost = $@"\\{serverName}";
            try
            {
                var oConn = new ConnectionOptions();
                oConn.Impersonation = ImpersonationLevel.Impersonate;
                oConn.Authentication = AuthenticationLevel.PacketPrivacy;
                //oConn.Username = "";
                //oConn.Password = "";
                var oMScope = new ManagementScope(sHost + NamespacePath, oConn);
                oMScope.Options.Authentication = AuthenticationLevel.PacketPrivacy;
                oMScope.Options.Impersonation = ImpersonationLevel.Impersonate;

                var oMPath = new ManagementPath();
                oMPath.ClassName = "Win32_TSGatewayResourceAuthorizationPolicy";
                oMPath.NamespacePath = NamespacePath;

                oMScope.Connect();

                ManagementClass processClass = new ManagementClass(oMScope, oMPath, null);

                ManagementBaseObject inParameters = processClass.GetMethodParameters("Create");
                var mnvc = new ManagementNamedValueCollection();
                var imo = new InvokeMethodOptions();
                imo.Context = mnvc;
                processClass.Get();
                var i = 0;
                inParameters["Description"] = "";
                inParameters["Enabled"] = true;
                inParameters["ResourceGroupType"] = "CG";
                inParameters["ProtocolNames"] = "RDP";
                inParameters["PortNumbers"] = "3389";
                foreach (var rapName in missingRapNames)
                {
                    for (int retryCnt = 0; retryCnt < 10; retryCnt++)
                    {
                        Console.WriteLine($"Adding RAP {rapName}");
                        //_reporter.Info(serverName, $"Adding '{rapName}'.");
                        LoggerSingleton.SynchronizedRaps.Info($"Adding new RAP '{rapName}' to the gateway '{serverName}'.");
                        var groupName = ConvertToLgName(rapName);
                        inParameters["Name"] = "" + rapName;
                        inParameters["ResourceGroupName"] = groupName;
                        inParameters["UserGroupNames"] = groupName;

                        ManagementBaseObject outParameters = processClass.InvokeMethod("Create", inParameters, imo);

                        if ((uint)outParameters["ReturnValue"] == 0)
                        {
                            Console.WriteLine($"{rapName} created. {++i}/{missingRapNames.Count}"); //TODO delete
                            LoggerSingleton.SynchronizedRaps.Info($"RAP '{rapName}' added to the gateway '{serverName}'.");
                            break;
                            //GlobalInstance.Instance.AddToObjectsList(serverName, computer.Name.Replace("$", ""), "LG-" + rapName, true);
                            //_reporter.IncrementAddedRaps(serverName);
                        }
                        else
                        {
                            if ((uint)outParameters["ReturnValue"] == 2147749913)
                            {
                                LoggerSingleton.SynchronizedRaps.Warn($"Error creating RAP: '{rapName}'. Reason: Already exists.");
                                //GlobalInstance.Instance.AddToObjectsList(serverName, computer.Name.Replace("$", ""), "LG-" + rapName, true);
                                break;
                            }
                            else
                            {
                                Console.WriteLine($"Error creating RAP: '{rapName}'. Reason: {(uint)outParameters["ReturnValue"]}.");
                                LoggerSingleton.SynchronizedRaps.Error($"Error creating RAP: '{rapName}'. Reason: {(uint)outParameters["ReturnValue"]}.");
                                Console.WriteLine($"Retry number {retryCnt}");
                            }
                        }
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                LoggerSingleton.SynchronizedRaps.Error(ex, $"Error when adding new RAPs to the gateway '{serverName}'.");
                //_reporter.Error(serverName, $"Exception when adding missing RAPs to the gateway. Details: {ex.Message}");
                return false;
            }
        }

        private string ConvertToLgName(string rapName)
        {
            return rapName.Replace("RAP_", "LG-");
        }

        public async Task<List<RapsDeletionResponse>> DeleteRapsFromGateway(string serverName, List<string> rapNamesToDelete)
        {
            var result = new List<RapsDeletionResponse>();
            //var username = ""; 
            //var password = ""; 
            //var securepassword = new SecureString();
            //foreach (char c in password)
            //    securepassword.AppendChar(c);
            const string AdSearchGroupPath = "WinNT://{0}/{1},group";
            const string NamespacePath = @"\root\CIMV2\TerminalServices";
            string _oldGatewayServerHost = $@"\\{serverName}.cern.ch";
            try
            {
                string where = CreateWhereClause(rapNamesToDelete);
                //string osQuery =
                //    "SELECT * FROM Win32_TSGatewayResourceAuthorizationPolicy " + where;
                string osQuery = "SELECT * FROM Win32_TSGatewayResourceAuthorizationPolicy";

                //CimCredential Credentials = new CimCredential(PasswordAuthenticationMechanism.Default, "cern.ch", username, securepassword);

                //WSManSessionOptions SessionOptions = new WSManSessionOptions();
                //SessionOptions.AddDestinationCredentials(Credentials);
                //CimSession mySession = CimSession.Create(serverName, SessionOptions);
                CimSession mySession = CimSession.Create(serverName);
                IEnumerable<CimInstance> queryInstance = mySession.QueryInstances(_oldGatewayServerHost + NamespacePath, "WQL", osQuery);
                IEnumerable<CimInstance> filteredInstances = queryInstance.Where(instance => rapNamesToDelete.Contains(instance.CimInstanceProperties["Name"].Value.ToString()));
                //foreach (CimInstance rapInstance in filteredInstances)
                //{
                //    var rapName = rapInstance.CimInstanceProperties["Name"].Value.ToString();
                //    if (!rapNamesToDelete.Contains(rapName)) continue;
                //    var rapDeletion = await DeleteRap(mySession, rapInstance, rapName);
                //    result.Add(rapDeletion);
                //}
                foreach (CimInstance rapInstance in filteredInstances)
                {
                    var rapName = rapInstance.CimInstanceProperties["Name"].Value.ToString();
                    if (!rapNamesToDelete.Contains(rapName)) continue;

                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(20));
                    var deleteTask = DeleteRap(mySession, rapInstance, rapName, cancellationTokenSource.Token);

                    if (await Task.WhenAny(deleteTask, Task.Delay(Timeout.Infinite, cancellationTokenSource.Token)) == deleteTask)
                    {
                        // Task completed within timeout
                        result.Add(await deleteTask);
                    }
                    else
                    {
                        // Task timed out
                        LoggerSingleton.SynchronizedRaps.Warn($"Timed out while deleting RAP '{rapName}'. Skipping to next RAP.");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerSingleton.SynchronizedRaps.Error($"Error while getting rap names from gateway: '{serverName}'.Ex: {ex}");
            }

            return result;
        }

        private string CreateWhereClause(IEnumerable<string> names)
        {
            var enumerable = names.ToList();
            if (enumerable.Count == 0)
                return "";
            var sb = new StringBuilder();
            sb.Append("WHERE");
            for (var i = 0; i < enumerable.Count; i++)
            {
                var name = enumerable[i];
                sb.Append(" (Name=\"" + name + "\")");
                if (i != enumerable.Count - 1)
                    sb.Append(" or");
            }
            return sb.ToString();
        }
        //private async Task<RapsDeletionResponse> DeleteRap(CimSession mySession, CimInstance rapInstance, string rapName)
        //{
        //    var rapDeletion = new RapsDeletionResponse(rapName);
        //    try
        //    {
        //        var result = mySession.InvokeMethod(rapInstance, "Delete", null);
        //        if (int.Parse(result.ReturnValue.Value.ToString()) == 0)
        //        {
        //            rapDeletion.Deleted = true;
        //            LoggerSingleton.SynchronizedRaps.Info($"Deleted RAP '{rapName}'.");
        //        }
        //    }
        //    catch (CimException ex) // catch only CIM exceptions
        //    {
        //        if (ex.Message.Contains("NotFound")) // check if the error message contains "NotFound"
        //        {
        //            LoggerSingleton.SynchronizedRaps.Warn($"Could not find RAP '{rapName}' to delete."); // log a warning
        //            rapDeletion.Deleted = false;
        //        }
        //        else // handle all other CIM exceptions
        //        {
        //            LoggerSingleton.SynchronizedRaps.Error(ex, $"Error deleting RAP '{rapName}'.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerSingleton.SynchronizedRaps.Error(ex, $"Error deleting rap '{rapName}'.");
        //    }

        //    return rapDeletion;
        //}
        private async Task<RapsDeletionResponse> DeleteRap(CimSession mySession, CimInstance rapInstance, string rapName, CancellationToken cancellationToken)
        {
            var rapDeletion = new RapsDeletionResponse(rapName);
            try
            {
                // Run the deletion process in a task to support cancellation
                await Task.Run(() =>
                {
                    var result = mySession.InvokeMethod(rapInstance, "Delete", null);
                    if (int.Parse(result.ReturnValue.Value.ToString()) == 0)
                    {
                        rapDeletion.Deleted = true;
                        LoggerSingleton.SynchronizedRaps.Info($"Deleted RAP '{rapName}'.");
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                LoggerSingleton.SynchronizedRaps.Warn($"Deletion of RAP '{rapName}' was cancelled due to timeout.");
                rapDeletion.Deleted = false;
            }
            catch (Exception ex)
            {
                LoggerSingleton.SynchronizedRaps.Error(ex, $"Error deleting RAP '{rapName}'.");
            }

            return rapDeletion;
        }
    }

    public class RapsDeletionResponse
    {
        public string RapName { get; set; }
        public bool Deleted { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public RapsDeletionResponse(string name)
        {
            RapName = name;
        }
    }
}

