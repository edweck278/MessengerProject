using System.Net.Sockets;
using System.Net;
using System.Collections;

using MessengerProjectPacket;

namespace MessengerProject
{
    public class Server
    {
        #region Private Members

        // Structure to store the client information
        private struct Client
        {
            public EndPoint endPoint;
            public string name;
        }

        // Listing of clients
        private ArrayList clientList;

        // Server socket
        private Socket serverSocket;

        // Data stream
        private byte[] dataStream = new byte[1024];
        #endregion

        #region Methods
        public Server()
        {
            Server_Load();
        }
        private void Server_Load()
        {
            try
            {
                // Initialise the ArrayList of connected clients
                this.clientList = new ArrayList();

                // Initialise the socket
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Initialise the IPEndPoint for the server and listen on port 8080
                IPEndPoint server = new IPEndPoint(IPAddress.Any, 8080);

                // Associate the socket with this IP address and port
                serverSocket.Bind(server);

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = (EndPoint)clients;

                // Start listening for incoming data
                serverSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);

                Console.WriteLine("Listening...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load Error: " + ex.Message, "UDP Server");
            }
        }
        public void SendData(IAsyncResult asyncResult)
        {
            try
            {
                serverSocket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendData Error: " + ex.Message, "UDP Server");
            }
        }

        private void ReceiveData(IAsyncResult asyncResult)
        {
            try
            {
                byte[] data;

                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(this.dataStream);

                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = (EndPoint)clients;

                // Receive all data
                serverSocket.EndReceiveFrom(asyncResult, ref epSender);

                // Fill the packet with the data to be sent
                sendData.ChatDataIdentifier = receivedData.ChatDataIdentifier;
                sendData.ChatName = receivedData.ChatName;

                switch (receivedData.ChatDataIdentifier)
                {
                    case DataIdentifier.Message:
                        sendData.ChatMessage = string.Format("{0}: {1}", receivedData.ChatName, receivedData.ChatMessage);
                        break;

                    case DataIdentifier.LogIn:
                        // Populate client object
                        Client client = new Client();
                        client.endPoint = epSender;
                        client.name = receivedData.ChatName;

                        // Add client to list
                        this.clientList.Add(client);

                        sendData.ChatMessage = string.Format("--- {0} is online ---", receivedData.ChatName);
                        break;

                    case DataIdentifier.LogOut:
                        // Remove current client from list
                        foreach (Client c in this.clientList)
                        {
                            if (c.endPoint.Equals(epSender))
                            {
                                this.clientList.Remove(c);
                                break;
                            }
                        }

                        sendData.ChatMessage = string.Format("--- {0} is offline ---", receivedData.ChatName);
                        break;
                }

                // Get packet as byte array
                data = sendData.GetDataStream();

                foreach (Client client in this.clientList)
                {
                    // may want to remove conditions
                    if (client.endPoint != epSender || sendData.ChatDataIdentifier != DataIdentifier.LogIn)
                    {
                        // Broadcast to all logged on users
                        serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                    }
                }

                // Listen for more connections again...
                serverSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epSender, new AsyncCallback(this.ReceiveData), epSender);
                Console.WriteLine("Continuing to listen...");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveData Error: " + ex.Message, "UDP Server");
            }
        }
        #endregion
    }
}