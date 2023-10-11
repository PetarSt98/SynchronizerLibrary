using SynchronizerLibrary.CommonServices.LocalGroups;

namespace SynchronizerLibrary.CommonServices
{
    public interface IGatewayLocalGroupSynchronizer
    {
        bool DownloadGatewayConfig(string serverName, bool cacheFlag = false);
        List<string>  SyncLocalGroups(LocalGroupsChanges changedLocalGroups, string serverName);
    }
}
