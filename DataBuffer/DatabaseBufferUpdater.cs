using Microsoft.EntityFrameworkCore;
using SynchronizerLibrary.Data;
using SynchronizerLibrary.Loggers;
using SynchronizerLibrary.Loggers;

namespace SynchronizerLibrary.DataBuffer
{
    public class DatabaseSynchronizator
    {
        private GlobalInstance globalInstance;
        private Dictionary<string, RAP_ResourceStatus> databaseStatusUpdater;

        public DatabaseSynchronizator()
        {
            globalInstance = GlobalInstance.Instance;
        }

        public void AverageGatewayReults()
        {
            databaseStatusUpdater = new Dictionary<string, RAP_ResourceStatus>();
            foreach (var name in globalInstance.Names)
            {
                foreach (var obj in globalInstance.ObjectLists[name])
                {
                    var key = $"{obj.ComputerName}-{obj.GroupName}";
                    if (!databaseStatusUpdater.ContainsKey(key))
                    {
                        databaseStatusUpdater[key] = obj;
                    }
                    else
                    {
                        databaseStatusUpdater[key].Status &= obj.Status;
                    }
                }
            }
        }

        public void UpdateDatabase()
        {
            using (var db = new RapContext())
            {
                foreach (KeyValuePair<string, RAP_ResourceStatus> item in databaseStatusUpdater)
                {
                    var obj = item.Value;
                    if (!item.Value.Status) continue;

                    Console.WriteLine($"  ComputerName: {obj.ComputerName}, GroupName: {obj.GroupName}");

                    // Selecting matching raps
                    var matchingRaps = db.raps
                        .Where(r => r.resourceGroupName == obj.GroupName)
                        .Include(r => r.rap_resource)  // Including related rap_resources
                        .ToList();

                    // Filtering unsynchronized raps and their resources
                    var unsynchronizedRaps = matchingRaps
                        .Where(r => r.rap_resource.Any(rr => rr.synchronized == false && rr.resourceName == obj.ComputerName))
                        .ToList();

                    foreach (var unsynchronizedRap in unsynchronizedRaps)
                    {
                        foreach (var resource in unsynchronizedRap.rap_resource.Where(rr => rr.resourceName == obj.ComputerName))
                        {
                            resource.synchronized = true;
                            LoggerSingleton.Raps.Info($"Rap_Resource successfuly synchronized RAP_Name: {resource.RAPName}, Device name: {resource.resourceName}");
                        }
                    }
                }
                db.SaveChanges();
            }
        }

    }

}