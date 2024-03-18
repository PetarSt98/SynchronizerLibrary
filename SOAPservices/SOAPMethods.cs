using System.Diagnostics;
using SynchronizerLibrary.Loggers;

namespace SynchronizerLibrary.SOAPservices
{
    public class SOAPMethods
    {
        public static async Task<Dictionary<string, string>> ExecutePowerShellSOAPScript(string computerName, string otherUser)
        {
            Console.WriteLine($"Calling SOAP Service: {computerName}");
            LoggerSingleton.Raps.Debug($"Calling SOAP Service: {computerName}");

            try
            {

                string pathToScript = @".\SOAPServiceScripts\SOAPNetworkService.ps1";
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    //Arguments = $"-ExecutionPolicy Bypass -File \"{pathToScript}\" -SetName1 \"{computerName}\" -SetUserName1 \"{otherUser}\" -UserName1 \"{userName}\" -Password1 \"{password}\"",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{pathToScript}\" -SetName1 \"{computerName}\" -SetUserName1 \"{otherUser}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using Process process = new Process { StartInfo = startInfo };
                process.Start();
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                string output = await outputTask;
                string errors = await errorTask;
                if (output.Length == 0 || errors.Length > 0) throw new ComputerNotFoundInActiveDirectoryException(errors);
                LoggerSingleton.Raps.Debug($"Successful call of SOAP Service: {computerName}");
                Dictionary<string, string> result = ConvertStringToDictionary(output);
                process.WaitForExit();

                return result;
            }
            catch (ComputerNotFoundInActiveDirectoryException ex)
            {
                Console.WriteLine($"{ex.Message} Unable to use SOAP operations for device: {computerName}");
                LoggerSingleton.Raps.Error($"{ex.Message} Unable to use SOAP operations for device: {computerName}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                LoggerSingleton.Raps.Error($"{ex.Message}");
                return null;
            }
        }

        public static async Task<Dictionary<string, string>> ExecutePowerShellLAPScript(string computerName)
        {
            Console.WriteLine($"Calling SOAP Service: {computerName}");
            LoggerSingleton.Raps.Debug($"Calling SOAP Service: {computerName}");

            try
            {
                string pathToScript = @".\SOAPServiceScripts\LAPS.ps1";
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{pathToScript}\" -SetName1 \"{computerName}\"",
                    //Arguments = $"-ExecutionPolicy Bypass -File \"{pathToScript}\" -SetName1 \"{computerName}\"  -UserName1 \"{userName}\" -Password1 \"{password}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Dictionary<string, string> result = new Dictionary<string, string>();

                await Task.Run(async () => // Note the 'async' here
                {
                    using Process process = new Process { StartInfo = startInfo };
                    process.Start();

                    // Use await instead of .Result
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string errors = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();

                    if (output.Length == 0 || errors.Length > 0)
                        throw new ComputerNotFoundInActiveDirectoryException(errors);

                    result = ConvertStringToDictionary(output);
                });

                LoggerSingleton.Raps.Debug($"Successful call of SOAP Service: {computerName}");
                return result;
            }
            catch (ComputerNotFoundInActiveDirectoryException ex)
            {
                Console.WriteLine($"{ex.Message} Unable to use LAPS operations for device: {computerName}");
                LoggerSingleton.Raps.Error($"{ex.Message} Unable to use SOAP operations for device: {computerName}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                LoggerSingleton.Raps.Error($"{ex.Message}");
                return null;
            }
        }

        public static Dictionary<string, string> ConvertStringToDictionary(string input)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                int separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();
                    result[key] = value;
                }
            }

            return result;
        }
    }
    class ComputerNotFoundInActiveDirectoryException : Exception
    {
        public ComputerNotFoundInActiveDirectoryException() : base() { }
        public ComputerNotFoundInActiveDirectoryException(string message) : base(message) { }
    }
    
}
