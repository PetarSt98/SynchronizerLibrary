using SynchronizerLibrary.CommonServices.LocalGroups;

namespace SynchronizerLibrary.CommonServices
{
    public interface IGatewayLocalGroupSynchronizer
    {
        bool DownloadGatewayConfig(string serverName, bool cacheFlag = false);
        Task<List<string>>  SyncLocalGroups(LocalGroupsChanges changedLocalGroups, string serverName);
    }
}
