//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PipesProvider.Networking
{
    /// <summary>
    /// Class that provide API for network information.
    /// </summary>
    public static class Info
    {
        /// <summary>
        /// Conver ip adress of server to host name.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static void TryGetHostName(string ipAddress, ref string output)
        {
            // Set request as output for case of fail.
            output = ipAddress;

            // Change pipe local domain to valid.
            if (ipAddress.Equals("."))
            {
                ipAddress = "localhost";
            }

            try
            {
                // Try to get entry.
                // Can be failed if host name not available.
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    output = entry.HostName;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("HOST NAME NOT FOUND: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Ping server by URI via requested port.
        /// In case if cooecntion established will call callback where:
        /// </summary>
        /// <param name="hostUri">Server uri.</param>
        /// <param name="portNumber">Port for connection. Must be oppend.</param>
        /// <param name="callback">Method that will be called as callback when cooection will be established. 
        /// string: uri
        /// int: port</param>
        public async static void PingHost(string hostUri, int portNumber, System.Action<string, int> callback)
        {
            // Change pipe local domain to valid.
            if (hostUri.Equals("."))
            {
                hostUri = "localhost";
            }

            try
            {
                using (var client = new TcpClient())
                {
                    // Try to reach the server and port.
                    await client.ConnectAsync(hostUri, portNumber);

                    // Call callback.
                    callback?.Invoke(hostUri, portNumber);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error pinging host:'" + hostUri + ":" + portNumber.ToString() + "' Reasons:\n" + ex.Message);
            }
        }

        public static string[] ServerName()
        {
            string[] strIP = DisplayIPAddresses();
            Console.WriteLine("strIP : {0}", strIP.Length);
            int CountIP = 0;
            for (int i = 0; i < strIP.Length; i++)
            {
                if (strIP[i] != null)
                    CountIP++;
            }
            string[] name = new string[CountIP];
            for (int i = 0; i < strIP.Length; i++)
            {
                if (strIP[i] != null)
                {
                    try
                    {
                        Console.WriteLine("IP : {0} | {1}", strIP[i], System.Net.Dns.GetHostEntry(strIP[i]).HostName);
                        name[i] = System.Net.Dns.GetHostEntry(strIP[i]).HostName;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return name;
        }

        /// <summary>
        /// Provide IP addresses for every relevant network interface.
        /// </summary>
        /// <returns></returns>
        public static string[] DisplayIPAddresses()
        {
            // Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)     
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            int i = -1;
            string[] s = new string[networkInterfaces.Length];

            foreach (NetworkInterface network in networkInterfaces)
            {
                i++;
                if (network.OperationalStatus == OperationalStatus.Up)
                {
                    if (network.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    if (network.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    //GatewayIPAddressInformationCollection GATE = network.GetIPProperties().GatewayAddresses;
                    // Read the IP configuration for each network   

                    IPInterfaceProperties properties = network.GetIPProperties();
                    //discard those who do not have a real gateaway 
                    if (properties.GatewayAddresses.Count > 0)
                    {
                        bool good = false;
                        foreach (GatewayIPAddressInformation gInfo in properties.GatewayAddresses)
                        {
                            //not a true gateaway (VmWare Lan)
                            if (!gInfo.Address.ToString().Equals("0.0.0.0"))
                            {
                                s[i] = gInfo.Address.ToString();
                                good = true;
                                break;
                            }
                        }
                        if (!good)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return s;
        }

        /// <summary>
        /// Mac adress of current device.
        /// </summary>
        public static string MacAdsress
        {
            get
            {
                // Get new address if not found.
                if (string.IsNullOrEmpty(macAddress))
                {
                    macAddress =
                    (
                        from nic in NetworkInterface.GetAllNetworkInterfaces()
                        where nic.OperationalStatus == OperationalStatus.Up
                        select nic.GetPhysicalAddress().ToString()
                    ).FirstOrDefault();
                }
                return macAddress;
            }
        }
        private static string macAddress;

    }
}
