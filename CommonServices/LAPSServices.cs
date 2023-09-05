using System.DirectoryServices;
using SynchronizerLibrary.CommonServices.Exceptions;

namespace SynchronizerLibrary.CommonServices
{
    class LAPSService
    {
        private static (DirectoryEntry, string) GetRemoteDesktopUsersGroup(string machineName, string adminUsername, string adminPassword)
        {
            try
            {
                var ad = new DirectoryEntry($"WinNT://{machineName},computer", adminUsername, adminPassword);
                foreach (DirectoryEntry childEntry in ad.Children)
                {
                    if (childEntry.SchemaClassName == "Group" && childEntry.Name.Equals("Remote Desktop Users", StringComparison.OrdinalIgnoreCase))
                    {
                        return (childEntry, "Device is unreachable.");
                    }
                }
                throw new Exception($"Local group 'Remote Desktop Users' not found on server '{machineName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (null, ex.Message);
            }
        }

        private static bool MemberExists(DirectoryEntry groupEntry, string domain, string userName)
        {
            foreach (object member in (System.Collections.IEnumerable)groupEntry.Invoke("Members"))
            {
                using (DirectoryEntry memberEntry = new DirectoryEntry(member))
                {
                    if (memberEntry.Name.Equals(userName, StringComparison.OrdinalIgnoreCase)
                        && memberEntry.Path.Contains($"WinNT://{domain}/"))
                    {
                        return true;  // Member exists
                    }
                }
            }

            return false;  // Member does not exist
        }

        private static void AddorRemoveUserToRemoteDesktopUsersGroup(DirectoryEntry groupEntry, string domain, string userName, string lapsOperator)
        {
            try
            {
                if (lapsOperator == "Add")
                {
                    if (!MemberExists(groupEntry, domain, userName))
                    {
                        groupEntry.Invoke("Add", $"WinNT://{domain}/{userName}");
                        groupEntry.CommitChanges();
                    }
                }
                if (lapsOperator == "Remove")
                {
                    if (MemberExists(groupEntry, domain, userName))
                    {
                        groupEntry.Invoke("Remove", $"WinNT://{domain}/{userName}");
                        groupEntry.CommitChanges();
                    }
                }
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                //if (ex.HResult == -2146232828)
                //{
                //    Loggers.LoggerSingleton.SynchronizedLocalGroups.Debug("User already deleted in LAPS");
                //}
                throw ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Loggers.LoggerSingleton.SynchronizedLocalGroups.Error(ex.Message);
                throw ex;
            }
            Console.WriteLine("Updated with LAPS");
        }
        public static (bool, string) UpdateLaps(string machineName, string newUser, string lapsOperator)
        {
            if (lapsOperator != "Add" && lapsOperator != "Remove")
            {
                throw new Exception($"Uknown LAPS operator: {lapsOperator} !");
            }

            string lapsAttribute = "ms-Mcs-AdmPwd";
            string ldapPath = $"LDAP://CN={machineName},OU=Computers,OU=Organic Units,DC=cern,DC=ch";
            string username = "svcgtw";
            string password = "7KJuswxQnLXwWM3znp";
            string domain = "CERN";
            string statusMessage = "";

            try
            {
                using (DirectoryEntry directoryEntry = new DirectoryEntry(ldapPath, username, password))
                {
                    if (directoryEntry.Properties.Contains(lapsAttribute))
                    {
                        var pass = directoryEntry.Properties[lapsAttribute].Value.ToString();

                        if (string.IsNullOrEmpty(pass))
                        {
                            throw new RemoteDesktopUserGroupNotFound($"Device: {machineName} is unreachable!");
                        }
                        (DirectoryEntry remoteDesktopGroup, statusMessage) = GetRemoteDesktopUsersGroup(machineName, "Administrator", pass);
                        using (remoteDesktopGroup)
                        {
                            if (remoteDesktopGroup == null)
                            {
                                return (false, statusMessage);
                            }
                            AddorRemoveUserToRemoteDesktopUsersGroup(remoteDesktopGroup, domain, newUser, lapsOperator);
                        }
                    }
                }
                return (true, "");
            }
            catch (RemoteDesktopUserGroupNotFound ex)
            {
                return (false, "Unreachable password");
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                    return (false, ex.InnerException.Message);
                }
                else
                {
                    return (false, ex.Message);
                }
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException ex)
            {
                //if (ex.ErrorCode == -2147016656)
                //{
                //    return (false, "");
                //}

                //throw ex;
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                    return (false, ex.InnerException.Message);
                }
                else
                {
                    return (false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                    return (false, ex.InnerException.Message);
                }
                else
                {
                    return (false, ex.Message);
                }
            }
        }

    }
}

namespace SynchronizerLibrary.CommonServices.Exceptions
{
    class RemoteDesktopUserGroupNotFound : Exception
    {
        public RemoteDesktopUserGroupNotFound() : base() { }
        public RemoteDesktopUserGroupNotFound(string message) : base(message) { }
    }
}

