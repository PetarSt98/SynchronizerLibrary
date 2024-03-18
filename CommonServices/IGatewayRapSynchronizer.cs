﻿namespace SynchronizerLibrary.CommonServices
{
    public interface IGatewayRapSynchronizer
    {
        Task<List<string>> GetGatewaysRapNamesAsync(string serverName, bool cacheFlag = false);
        Task SynchronizeRaps(string serverName, List<string> allGatewayGroups, List<string> toDeleteGatweayGroups, List<string> gatewayRaps);
    }
}
