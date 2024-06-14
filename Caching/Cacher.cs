using System.Text.Json;
using SynchronizerLibrary.CommonServices.LocalGroups;

namespace SynchronizerLibrary.Caching
{
    public class Cacher
    {
        private const string CacheFilePolicyPrefix = "rapNamesCache";
        private const string CacheFileLocalGroupPrefix = "localGroupsCache";
        private const string CacheFileExtension = ".json";
        private const string CacheFilePath = "Cache";

        static public void SavePolicyCacheToFile(List<string> _rapNamesCache, string serverName)
        {
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string newCacheFilePath = $".\\{CacheFilePath}\\{CacheFilePolicyPrefix}{serverName}{dateTime}{CacheFileExtension}";

            File.WriteAllText(newCacheFilePath, JsonSerializer.Serialize(_rapNamesCache));
        }

        static public void SaveLocalGroupCacheToFile(List<LocalGroup> _policyNamesCache, string serverName)
        {
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string newCacheFilePath = $".\\{CacheFilePath}\\{CacheFileLocalGroupPrefix}{serverName}{dateTime}{CacheFileExtension}";

            File.WriteAllText(newCacheFilePath, JsonSerializer.Serialize(_policyNamesCache));
        }

        static public List<string> LoadPolicyCacheFromFile(string serverName)
        {
            var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var newestFile = directoryInfo.GetFiles($".\\{CacheFilePath}\\{CacheFilePolicyPrefix}{serverName}*{CacheFileExtension}")
                                            .OrderByDescending(f => f.LastWriteTime)
                                            .FirstOrDefault();

            if (newestFile != null)
            {
                var content = File.ReadAllText(newestFile.FullName);
                return JsonSerializer.Deserialize<List<string>>(content);
            }

            return null;
        }

        static public List<LocalGroup> LoadLocalGroupCacheFromFile(string serverName)
        {
            var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            var newestFile = directoryInfo.GetFiles($".\\{CacheFilePath}\\{CacheFileLocalGroupPrefix}{serverName}*{CacheFileExtension}")
                                            .OrderByDescending(f => f.LastWriteTime)
                                            .FirstOrDefault();

            if (newestFile != null)
            {
                try
                {
                    var content = File.ReadAllText(newestFile.FullName);
                    var serializedContent = JsonSerializer.Deserialize<List<LocalGroup>>(content);
                    return serializedContent;
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine("JSON Exception: " + jsonEx.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("General Exception: " + ex.Message);
                }
            }

            return null;
        }
    }
}
