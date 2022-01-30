using System.Net.Sockets;
using System.Net;
using System.Collections;

using MessengerPacket;

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

        // List of clients
        private ArrayList clientList;
        // List of client sockets (files)
        private List<Socket> clientSockets = new List<Socket>();

        // Server sockets
        private Socket serverSocket;
        private Socket serverSocketFiles;

        // Data streams
        private byte[] dataStream = new byte[1024];
        private byte[] fileDataStream = new byte[4096];
        #endregion

        #region Methods
        public Server()
        {
            Server_Load();
            // Add something to block the console thread because everything is running in the background!
            Console.WriteLine("Press Enter to close the server...");
            Console.ReadLine();
            CloseAllSockets();
        }
        private void Server_Load()
        {
            // Initialise the ArrayList of connected clients
            this.clientList = new ArrayList();

            // Begin connecting
            ConnectForMessages();
            ConnectForFiles();
            
        }
        private void ConnectForMessages() 
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectForMessages Error: " + ex.Message, "UDP Server");
            }
        }

        private void ConnectForFiles()
        {
            try
            {
                // Initialise the socket
                serverSocketFiles = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Initialise the IPEndPoint for the server and listen on port 8081
                IPEndPoint server = new IPEndPoint(IPAddress.Any , 8081);

                // Associate the socket with this IP address and port
                serverSocketFiles.Bind(server);
                serverSocketFiles.Listen(10); // Allow up to ten pending connections

                // Accept new connections
                serverSocketFiles.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (Exception ex) 
            { 
                Console.WriteLine("ConnectForFiles Error: " + ex.Message, "TCP Server");
            }
        }

        private void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Close();
            }
            serverSocket.Close();
            serverSocketFiles.Close();
        }
        #endregion

        #region Callbacks
        private void SendData(IAsyncResult asyncResult)
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

        private void SendFile(IAsyncResult asyncResult)
        {
            try
            {
                // Get the sender
                Socket socket = (Socket)asyncResult.AsyncState;
                socket.EndSend(asyncResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendFile Error: " + ex.Message, "TCP Server");
            }
        }


        private void ReceiveData(IAsyncResult asyncResult)
        {
            try
            {
                byte[] data;

                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(this.dataStream);

                // Initialise the IPEndPoint for the clients
                IPEndPoint clients = new IPEndPoint(IPAddress.Any, 0);

                // Initialise the EndPoint for the clients
                EndPoint epSender = (EndPoint)clients;

                // Receive all data
                serverSocket.EndReceiveFrom(asyncResult, ref epSender);

                switch (receivedData.ChatDataIdentifier)
                {
                    case Packet.DataIdentifier.Message:
                        receivedData.ChatMessage = string.Format("{0}: {1}", receivedData.ChatName, receivedData.ChatMessage);
                        break;

                    case Packet.DataIdentifier.LogIn:
                        // Populate client object
                        Client client = new Client();
                        client.endPoint = epSender;
                        client.name = receivedData.ChatName;

                        // Add client to list
                        this.clientList.Add(client);

                        receivedData.ChatMessage = string.Format("--- {0} is online ---", receivedData.ChatName);
                        break;

                    case Packet.DataIdentifier.LogOut:
                        // Remove current client from list
                        foreach (Client c in this.clientList)
                        {
                            if (c.endPoint.Equals(epSender))
                            {
                                IPEndPoint ep = (IPEndPoint)epSender;
                                // Remove and close socket opened for files from this client
                                foreach (Socket socket in clientSockets)
                                {
                                    IPEndPoint sep = (IPEndPoint)socket.RemoteEndPoint;
                                    if (ep.Address.Equals(sep.Address)) // compare ip address to find which client to remove
                                    {
                                        socket.Close();
                                        clientSockets.Remove(socket);
                                        break;
                                    }
                                }
                                this.clientList.Remove(c);
                                break;
                            }
                        }

                        receivedData.ChatMessage = string.Format("--- {0} is offline ---", receivedData.ChatName);
                        break;
                }

                // Make the packet a byte array
                data = receivedData.GetDataStream();

                foreach (Client client in this.clientList)
                {
                    //if (client.endPoint != epSender) // Don't send to client we received from
                    //{
                    // Broadcast to all logged on users
                    serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, client.endPoint, new AsyncCallback(this.SendData), client.endPoint);
                    //}
                }

                // Print message we sent out to server console
                Console.WriteLine(receivedData.ChatMessage);

                // Listen for more connections again...
                serverSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epSender, new AsyncCallback(this.ReceiveData), epSender);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveData Error: " + ex.Message, "UDP Server");
            }
        }

        private void ReceiveFile(IAsyncResult asyncResult)
        {
            try
            {
                // Get the sender
                Socket socket = (Socket)asyncResult.AsyncState;
                int received;
                received = socket.EndReceive(asyncResult);

                // Make byte array the length of data sent and store the data in the array
                byte[] data = new byte[received];
                Buffer.BlockCopy(fileDataStream, 0, data, 0, data.Length);

                // Send file received out to clients
                foreach (Socket clientSocket in clientSockets)
                {
                    // put condition to not send to sender
                    clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendFile), clientSocket);
                }

                // Listen for more potential files from this client
                socket.BeginReceive(fileDataStream, 0, fileDataStream.Length, SocketFlags.None, new AsyncCallback(ReceiveFile), socket);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveFile Error: " + ex.Message, "TCP Server");
            }

        }

        private void AcceptCallback(IAsyncResult asyncResult) 
        {
            try 
            {
                // Accept client connection
                Socket socket = serverSocketFiles.EndAccept(asyncResult); 

                // Add to list of sockets so we can use later
                clientSockets.Add(socket);

                // Start receiving potential files from client
                socket.BeginReceive(fileDataStream, 0, fileDataStream.Length, SocketFlags.None, new AsyncCallback(ReceiveFile), socket);

                // Call BeginAccept again for new potential client connections
                serverSocketFiles.BeginAccept(new AsyncCallback(AcceptCallback), null);
            } 
            catch (ObjectDisposedException)
            { }
            catch (Exception ex) 
            { 
                Console.WriteLine("AcceptCallback Error: " + ex.Message, "TCP Server");
            }
}
        #endregion
    }
}