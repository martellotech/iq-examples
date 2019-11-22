using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;


namespace iQOpenApiExample
{
    public static class Helper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void ColoredConsoleWriteLine(string text, ConsoleColor color)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine("{0:HH:mm:ss:ffff} {1}", DateTime.Now, text);

            Console.ForegroundColor = originalColor;
            Logger.Debug(text);
        }

        public static string GetDescription(this Enum value)
        {
            var name = value.ToString();
            var fieldInfo = value.GetType().GetField(name);
            if (fieldInfo == null)
                return name;
            var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length == 0)
                return name;

            return ((DescriptionAttribute)attributes[0]).Description;
        }

        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception($"No network adapters with an IPv4 address in the system!");
        }
        public static string GetFqdn()
        {
            string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string hostName = Dns.GetHostName();

            domainName = "." + domainName;
            if (!hostName.EndsWith(domainName))  // if hostname does not already include domain name
            {
                hostName += domainName;   // add the domain name part
            }

            return hostName;                    // return the fully qualified name
        }
        public static List<string> GetSourceNamesFromLog(string logName)
        {
            List<string> sourceNameList = new List<string>();
            RegistryKey keyLog = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Services\EventLog\" + logName);

            if (keyLog != null && keyLog.SubKeyCount > 0)
            {
                string[] sourceNames = keyLog.GetSubKeyNames();

                sourceNameList.Capacity = keyLog.SubKeyCount;

                sourceNameList.AddRange(sourceNames);
            }

            return sourceNameList;
        }

    }
}
