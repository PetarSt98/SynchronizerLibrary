using System;
using System.Collections.Generic;

namespace SynchronizerLibrary.DataBuffer
{ 
    public class GlobalInstance
    {
        private static GlobalInstance _instance;
        private static readonly object _lock = new object();

        public List<string> Names { get; private set; }
        public Dictionary<string, List<RAP_ResourceStatus>> ObjectLists { get; private set; }

        private GlobalInstance()
        {
            Names = new List<string>();
            ObjectLists = new Dictionary<string, List<RAP_ResourceStatus>>();
        }

        public static GlobalInstance Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GlobalInstance();
                    }
                    return _instance;
                }
            }
        }
    }

    public class RAP_ResourceKey
    {
        public string ComputerName { get; set; }
        public string GroupName { get; set; }
    }
    public class RAP_ResourceStatus
    {
        public string ComputerName { get; set; }
        public string GroupName { get; set; }
        public bool Status { get; set; }
    }
}