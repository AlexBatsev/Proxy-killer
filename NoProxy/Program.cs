using System;
using System.Management;
using System.Threading;
using Microsoft.Win32;

namespace NoProxy
{
    internal static class Program
    {
        private const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const string RegVal = @"AutoConfigURL";

        private static void Main(string[] args)
        {
            try
            {
                var user = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
                var key = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(RegKey, false);
                var query = new WqlEventQuery(
                    $@"SELECT * FROM RegistryValueChangeEvent 
WHERE Hive = 'HKEY_USERS' 
AND KeyPath = '{user}\\{key}' 
AND ValueName='{RegVal}'");
                //var query = new WqlEventQuery("SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_USERS'");

                var watcher = new ManagementEventWatcher(query);
                Console.WriteLine("Proxy-killer launched.\n");
                watcher.EventArrived += (sender, e) => DelRegVal();
                watcher.Start();
                DelRegVal();
                Thread.Sleep(100000000);
                watcher.Stop();
            }
            catch (ManagementException managementException)
            {
                Console.WriteLine("An error occurred: " + managementException.Message);
            }
        }

        private static void DelRegVal()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegKey, true);
                var val = key?.GetValue(RegVal);
                if (val == null)
                    return;
                Console.WriteLine($"Deleting '{RegVal}={val}' at {DateTime.Now:T}.");
                key.DeleteValue(RegVal, false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}