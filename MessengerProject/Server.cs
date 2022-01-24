using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MessengerProject 
{
    class Server
    {
        /// Property to store the Port
        public static int Port { get; private set; }
        public static string? IPAddress { get; private set; }

        public static void Start(int _port, string _ipAddress) 
        {
            Port = _port;
            IPAddress = _ipAddress;

            Socket 
        }
    }

}