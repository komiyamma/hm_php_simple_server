﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HmPHPSimpleServer
{
    [Guid("F62022BE-69BA-40FF-8EA9-EEABEF0041E0")]
    public class HmUsedPortChecker
    {
        List<int> portsInUse;
        public int GetAvailablePort(int beginPort, int endPort)
        {
            var ipGP = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEPs = ipGP.GetActiveTcpListeners();
            var udpEPs = ipGP.GetActiveUdpListeners();
            portsInUse = tcpEPs.Concat(udpEPs).Select(p => p.Port).ToList();

            for (int port = beginPort; port <= endPort; ++port)
            {
                if (!portsInUse.Contains(port))
                {
                    return port;
                }
            }

            return 0; // 空きポートが見つからない場合
        }
    }
}
