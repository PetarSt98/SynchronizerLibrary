using SynchronizerLibrary.Data;
using System.Text.Json;
using SynchronizerLibrary.EmailService;


namespace SynchronizerLibrary.DataBuffer
{
    public class DatabaseSynchronizator
    {
        private GlobalInstance globalInstance;
        private Dictionary<string, RAP_ResourceStatus> databaseStatusUpdater;
        private Dictionary<string, bool> partialStatus;
        private string username = "pstojkov";
        private string password = "GeForce9800GT.";
        private EmailBuilder emailParser;
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
                    var key = $"{obj.Value.ComputerName}-{obj.Value.GroupName}";
                    if (!databaseStatusUpdater.ContainsKey(key))
                    {
                        databaseStatusUpdater[key] = obj.Value;
                        partialStatus[key] = obj.Value.Status;
                    }
                    else
                    {
                        databaseStatusUpdater[key].Status &= obj.Value.Status;
                        partialStatus[key] |= obj.Value.Status;
                    }
                }
            }
        }

        public void UpdateDatabase()
        {
            bool sendEmail = false;
            using (var db = new RapContext())
            {
                foreach (var pair in databaseStatusUpdater.Zip(partialStatus, (item, partial) => (item, partial)))
                {
                    var obj = pair.item.Value;
                    if (!pair.item.Value.Status)
                    {
                        emailParser = new UnsuccessfulEmail(obj, db);
                        if (sendEmail) emailParser.Send();

                        //if (!pair.partial.Value)
                        //{
                        //    var rapResourcesToDelete = db.rap_resource.Where(rr => (rr.RAPName == obj.GroupName.Replace("LG-", "RAP_") && string.Equals(rr.resourceName, obj.ComputerName, StringComparison.OrdinalIgnoreCase))).ToList();
                        //    db.rap_resource.RemoveRange(rapResourcesToDelete);

                        //    LoggerSingleton.General.Warn($"Deleting unsynchronized RAP_Resource RAP_Name: {obj.GroupName.Replace("LG-", "RAP_")} resourceName: {obj.ComputerName} from MySQL database");
                        //    LoggerSingleton.Raps.Warn($"Deleting unsynchronized RAP_Resource RAP_Name: {obj.GroupName.Replace("LG-", "RAP_")} resourceName: {obj.ComputerName} from MySQL database");
                        //    Console.WriteLine($"Deleting unsynchronized RAP_Resource RAP_Name: {obj.GroupName.Replace("LG-", "RAP_")} resourceName: {obj.ComputerName} from MySQL database");

                        //    db.SaveChanges();
                        //}
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        Console.WriteLine("No Status");
                        //        Console.WriteLine(ex.Message);
                        //    }
                        //}
                    }
                    else
                    {
                        Console.WriteLine($"  ComputerName: {obj.ComputerName}, GroupName: {obj.GroupName}");
                        emailParser = new SuccessfulEmail(obj, db);
                        if (sendEmail) emailParser.Send();
                        
                    }
                }
                db.SaveChanges();
            }
        }
    }

    public class SpamFailureHandler
    {
        public string deviceName { get; set; }
        public string userName { get; set; }
        public string cacheKey { get; set; }
        public int counter { get; set; }

        private static string path = "./cached_spam_failures.json";

        public SpamFailureHandler(string deviceName, string userName)
        {
            this.deviceName = deviceName;
            this.userName = userName;
            this.cacheKey = $"{userName}-{deviceName}";
            this.counter = 1;
        }

        public static void CleanCache(string deviceName, string userName)
        {
            deviceName = GlobalInstance.ModifyComputerName(deviceName);

            string cacheKey = $"{userName}-{deviceName}";

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                var temp = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, SpamFailureHandler>>(content);

                if (temp.ContainsKey(cacheKey)) // kada sacuva json sjebe ga i nema imena itd
                {
                    temp.Remove(cacheKey);
                    File.WriteAllText(path, JsonSerializer.Serialize(temp));
                }

            }

        }

        public static bool CheckStatus(string deviceName, string userName, bool email = false)
        {
            deviceName = GlobalInstance.ModifyComputerName(deviceName);
            string cacheKey = $"{userName}-{deviceName}";

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                var temp = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, SpamFailureHandler>>(content);

                if (temp.ContainsKey(cacheKey)) // kada sacuva json sjebe ga i nema imena itd
                {
                    if (email) return true;

                    if (temp[cacheKey].counter == 255)
                        return false;
                    else
                        return true;
                }
                else
                    return false;
            }
            else
            {
                return false;
            }

        }

        public void CacheSpam()
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                var temp = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, SpamFailureHandler>>(content);
                if (temp.ContainsKey(this.cacheKey))
                {
                    this.counter = temp[cacheKey].counter;
                    this.counter++;
                    if (this.counter > 255)
                    {
                        this.counter = 1;
                    }
                }
                else
                {
                    temp[cacheKey] = this;
                }    

                temp[cacheKey].counter = this.counter;
                File.WriteAllText(path, JsonSerializer.Serialize(temp));
            }
            else
            {
                var temp = new Dictionary<string, SpamFailureHandler>();
                temp[this.cacheKey] = this;
                File.WriteAllText(path, JsonSerializer.Serialize(temp));
            }
        }

    }

}