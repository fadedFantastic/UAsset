using System;
using System.Collections;

namespace UAsset
{
    public class UAssets
    {
        public static bool SimulationMode => Versions.SimulationMode;

        public static bool OfflineMode => Versions.OfflineMode;
        
        public static IEnumerator Initialize()
        {
            return Versions.InitializeAsync();
        }
        
    }
}