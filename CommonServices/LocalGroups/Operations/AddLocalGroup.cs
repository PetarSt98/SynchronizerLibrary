using System.DirectoryServices;
using SynchronizerLibrary.Loggers;
using SynchronizerLibrary.Data;
using SynchronizerLibrary.CommonServices.LocalGroups.Components;
using SynchronizerLibrary.CommonServices.LAPS;
using System.Data.Entity;


namespace SynchronizerLibrary.CommonServices.LocalGroups.Operations
{
    public class AddLocalGroup
    {
        public AddLocalGroup()
        {

        }

        public async Task<List<string>> AddNewGroups(string serverName, ICollection<LocalGroup> groupsToAdd, string specialFlag="")
        {
            LoggerSingleton.General.Info($"Adding {groupsToAdd.Count} new groups to the gateway '{serverName}'.");
            var addedGroups = new List<string>();
            using (var db = new RapContext())
            {
                foreach (var lg in groupsToAdd)
                {
                    LoggerSingleton.SynchronizedLocalGroups.Info(serverName, $"Adding group '{lg.Name}'.");
                    Console.WriteLine($"Adding group '{lg.Name}'.");
                    if (await AddNewGroupWithContent(serverName, lg, specialFlag))
                    {
                        addedGroups.Add(lg.Name);

                        LoggerSingleton.SynchronizedLocalGroups.Info($"Synchronized Local Group: {lg.Name} successfuly!");

                        if (specialFlag == "serverInit") continue;

                        foreach (var member in lg.MembersObj.Names.Zip(lg.MembersObj.Flags, (name, flag) => new { Name = name, Flag = flag }))
                        {
                            if (member.Flag == LocalGroupFlag.Delete) continue;

                            var matchingRaps = await db.raps
                                .Where(r => (r.login == member.Name && r.resourceGroupName == lg.Name))
                                .ToListAsync();

                            foreach (var rap in matchingRaps)
                            {
                                rap.synchronized = true;
                            }

                        }
                    }

                }
                db.SaveChanges();
            }
            LoggerSingleton.General.Info(serverName, $"Finished adding {addedGroups.Count} new groups.");
            LoggerSingleton.SynchronizedLocalGroups.Info(serverName, $"Finished adding {addedGroups.Count} new groups.");
            return addedGroups;
        }

        private async Task<bool> AddNewGroupWithContent(string server, LocalGroup lg, string specialFlag="")
        {
            //string groupName = FormatModifiedValue(lg.Name);
            var success = true;
            LoggerSingleton.SynchronizedLocalGroups.Info($"Adding empty group {lg.Name}");
            var newGroup = AddEmptyGroup(lg.Name, server);
            if (newGroup is not null)
            {
                LocalGroupMembers localGroupMembers = new LocalGroupMembers();
                LocalGroupComputers localGroupComputers = new LocalGroupComputers();
                LAPSService lapsService = new LAPSService();

                if (!localGroupMembers.SyncMember(newGroup, lg, server))
                    success = false;
                if (!localGroupComputers.SyncComputers(newGroup, lg, server))
                    success = false;

                if (specialFlag != "serverInit")
                {
                    if (!await lapsService.SyncLAPS(server, lg))
                        success = false;
                }
            }
            else
            {
                success = false;
                LoggerSingleton.SynchronizedLocalGroups.Error(server, $"Failed adding new group: '{lg.Name}' and its contents.");
            }
            newGroup.CommitChanges();
            return success;
        }

        public DirectoryEntry AddEmptyGroup(string groupName, string server)
        {
            var success = true;
            DirectoryEntry newGroup = null;
            //string username = "";
            //string password = "";
            bool groupExists = false;

            LoggerSingleton.SynchronizedLocalGroups.Debug($"Adding new group: '{groupName}' on gateway: '{server}'.");
            try
            {
                //var ad = new DirectoryEntry($"WinNT://{server},computer", username, password);
                var ad = new DirectoryEntry($"WinNT://{server},computer");
                try
                {
                    newGroup = ad.Children.Find(groupName, "group");
                    groupExists = true;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    if (ex.ErrorCode == -2147022675 || ex.ErrorCode == -2147022676) // Group not found.
                    {
                        LoggerSingleton.SynchronizedLocalGroups.Debug($"Group already exists: '{groupName}' on gateway: '{server}'.");
                        groupExists = true;

                        if (newGroup is null) return null;
                    }
                    else
                    {
                        LoggerSingleton.SynchronizedLocalGroups.Warn($"Different exception message than Already existing group: '{groupName}' on gateway: '{server}'.");
                        groupExists = false;
                    }
                }
                if (!groupExists)
                {
                    newGroup = ad.Children.Add(groupName, "group");
                    newGroup.CommitChanges();
                }
            }
            catch (Exception ex)
            {
                LoggerSingleton.SynchronizedLocalGroups.Error(ex, $"Error adding empty group: '{groupName}' on gateway: '{server}'.");
                newGroup = null;
            }

            return newGroup;
        }




    }
}
