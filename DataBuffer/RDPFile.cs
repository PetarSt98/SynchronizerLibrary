using System.Text;

namespace SynchronizerLibrary.RDPFile
{
    public struct RdpFile
    {
        public string FullAddress { get; set; }
        public int DesktopWidth { get; set; }
        public int DesktopHeight { get; set; }
        // Add other properties as needed

        public override string ToString()
        {
            StringBuilder rdpContent = new StringBuilder();
            rdpContent.AppendLine($"full address:s:{FullAddress}.cern.ch");
            rdpContent.AppendLine($"desktopwidth:i:{DesktopWidth}");
            rdpContent.AppendLine($"desktopheight:i:{DesktopHeight}");
            // Add other properties as needed

            rdpContent.AppendLine("session bpp:i:16");
            rdpContent.AppendLine("compression:i:1");
            rdpContent.AppendLine("keyboardhook:i:2");
            rdpContent.AppendLine("displayconnectionbar:i:1");
            rdpContent.AppendLine("disable wallpaper:i:1");
            rdpContent.AppendLine("disable wallpaper:i:1");
            rdpContent.AppendLine("disable full window drag:i:1");
            rdpContent.AppendLine("allow desktop composition:i:0");
            rdpContent.AppendLine("allow font smoothing:i:0");
            rdpContent.AppendLine("disable menu anims:i:1");
            rdpContent.AppendLine("disable themes:i:0");
            rdpContent.AppendLine("disable cursor setting:i:0");
            rdpContent.AppendLine("bitmapcachepersistenable:i:1");
            rdpContent.AppendLine("audiomode:i:0");
            rdpContent.AppendLine("redirectprinters:i:1");
            rdpContent.AppendLine("redirectcomports:i:0");
            rdpContent.AppendLine("redirectsmartcards:i:1");
            rdpContent.AppendLine("redirectclipboard:i:1");
            rdpContent.AppendLine("redirectposdevices:i:0");
            rdpContent.AppendLine("autoreconnection enabled:i:1");
            rdpContent.AppendLine("authentication level:i:0");
            rdpContent.AppendLine("prompt for credentials:i:0");
            rdpContent.AppendLine("negotiate security layer:i:1");
            rdpContent.AppendLine("remoteapplicationmode:i:0");
            rdpContent.AppendLine("alternate shell:s:");
            rdpContent.AppendLine("shell working directory:s:");
            rdpContent.AppendLine("gatewayhostname:s:cerngt01.cern.ch");
            rdpContent.AppendLine("gatewayusagemethod:i:1");
            rdpContent.AppendLine("gatewaycredentialssource:i:4");
            rdpContent.AppendLine("gatewayprofileusagemethod:i:1");
            rdpContent.AppendLine("omptcredentialonce:i:1");
            rdpContent.AppendLine("drivestoredirect:s:");
            return rdpContent.ToString();
        }
        public static string CustomizeRdpFile(string resourceName)
        {
            RdpFile rdpFile = new RdpFile
            {
                FullAddress = resourceName,
                DesktopWidth = 1280,
                DesktopHeight = 1024
                // Initialize other properties as needed
            };

            return rdpFile.ToString();
        }
    }
}
