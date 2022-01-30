using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

using MessengerPacket;

namespace MessengerProjectClient
{
    /// <summary>
    /// Client class is a console application that implements the behavior of a client for client-server communication.
    /// </summary>
    public class Client
    {
        #region Private Members
        // Server IP
        private string serverIPAddress = "";

        // Client sockets
        private Socket clientSocket;
        private Socket clientSocketFiles;

        // Client name
        private string name = "";

        // Server End Points
        private EndPoint epServer;
        private EndPoint epServerFiles;

        // Data streams
        private byte[] dataStream = new byte[1024];
        private byte[] fileDataStream = new byte[4096];
        #endregion

        #region Methods

        /// <summary>
        /// Constructor that starts up the client connections and interface
        /// </summary>
        public Client()
        {
            Client_Load();
        }
        
        /// <summary>
        /// Receiving IP Address to connect to and name from the client, opening connections to the server, and providing options for the client to use the app
        /// </summary>
        private void Client_Load()
        {
            // Welcome and connect
            Console.WriteLine("Welcome to messenger!");
            Console.WriteLine("Enter IP Address of the server you would like to connect to:");
            this.serverIPAddress += Console.ReadLine();
            Console.WriteLine("To log on, please enter your name:");
            this.name += Console.ReadLine();
            ConnectForMessages();
            ConnectForFiles();

            ConsoleKeyInfo cki;
            string msg = "";
            string file_path = "../../../../../";

            Console.WriteLine("You can now communicate!");

            bool connected = true;

            while (connected) 
            {
                Console.WriteLine("Press 'M' to send a message or 'F' to send a file. (esc to quit)"); 
                // As long as client is not typing - listen for incoming data
                while (Console.KeyAvailable == false)
                {
                    Thread.Sleep(1000); // Wait a second so we don't call it so many times
                    // Begin listening (receive messages and files)
                    Listen();
                }
                cki = Console.ReadKey();
                switch (cki.Key)
                {
                    case ConsoleKey.Escape:
                        Disconnect();
                        connected = false;
                        break;
                    case ConsoleKey.M:
                        Console.WriteLine("\nType your message:");
                        msg += Console.ReadLine();
                        Send(msg);
                        msg = "";
                        break;
                    case ConsoleKey.F:
                        Console.WriteLine("\nWhich file would you like to send? (must be .json or .xml)\nInput full path relative to the directory containing this application:");
                        file_path += Console.ReadLine();
                        while (!file_path.EndsWith(".json") && !file_path.EndsWith(".xml"))
                        {
                            file_path = "";
                            Console.WriteLine("You must choose a .json or .xml! Re-enter path:");
                            file_path += Console.ReadLine();
                        }
                        SendAFile(file_path);
                        file_path = "";
                        break;
                    default:
                        break;

                }
            }
        }

        /// <summary>
        /// Starts connection for sending/receiving text. Sends a Login packet to notify were online...
        /// </summary>
        private void ConnectForMessages()
        {
            try
            {
                // Initialise a packet object to store the data to be sent (Login)
                Packet sendData = new Packet();
                sendData.ChatName = this.name;
                sendData.ChatDataIdentifier = Packet.DataIdentifier.LogIn;

                // Initialise socket
                this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Initialise server IP
                IPAddress serverIP = IPAddress.Parse(serverIPAddress);

                // Initialise the IPEndPoint for the server and use port 8080
                IPEndPoint server = new IPEndPoint(serverIP, 8080);

                // Initialise the EndPoint for the server
                epServer = (EndPoint)server;

                // Make the packet a byte array
                byte[] data = sendData.GetDataStream();

                // Send data to server
                clientSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epServer, new AsyncCallback(this.SendData), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectForMessages Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Starts connection for sending/receiving files
        /// </summary>
        private void ConnectForFiles()
        {
            try
            {
                // Intitialize Socket
                clientSocketFiles = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Initialise server IP
                IPAddress serverIP = IPAddress.Parse(serverIPAddress);

                // Initialise the IPEndPoint for the server and use port 8081
                IPEndPoint server = new IPEndPoint(serverIP, 8081);

                // Initialise the EndPoint for the server
                epServerFiles = (EndPoint)server;

                clientSocketFiles.BeginConnect(serverIP, 8081, Connect, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConnectForFiles Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Listen for incoming messages or files
        /// </summary>
        private void Listen()
        {
            try
            {
                // listen for message broadcasts
                clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epServer, new AsyncCallback(this.ReceiveData), null);

                // listen for file broadcasts
                clientSocketFiles.BeginReceive(this.fileDataStream, 0, this.fileDataStream.Length, SocketFlags.None, new AsyncCallback(this.ReceiveFile), clientSocketFiles);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("Listen Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Disconnect from the server. Send a Logout message to notify other clients...
        /// </summary>
        private void Disconnect()
        {
            try
            {
                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();
                sendData.ChatDataIdentifier = Packet.DataIdentifier.LogOut;
                sendData.ChatName = this.name;
                sendData.ChatMessage = "";

                // Make packet a byte array
                byte[] byteData = sendData.GetDataStream();

                // Send packet to the server
                this.clientSocket.SendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer);

                // Close the sockets
                this.clientSocket.Close();
                this.clientSocketFiles.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Closing Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Send a message
        /// </summary>
        private void Send(string msg)
        {
            try
            {
                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();
                sendData.ChatName = this.name;
                sendData.ChatMessage = msg;
                sendData.ChatDataIdentifier = Packet.DataIdentifier.Message;

                // Make packet a byte array
                byte[] byteData = sendData.GetDataStream();

                // Send packet to the server
                clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(this.SendData), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Send a file
        /// </summary>
        private void SendAFile(string file_path)
        {
            try
            {
                // Read file into byte array
                byte[] byteData = File.ReadAllBytes(file_path);

                // Add byte to the front of the array that says if its json or xml (true=json, false=xml)
                bool type = false; // default filetype is xml
                if (file_path.EndsWith(".json"))
                {
                    type = true;
                }
                byte file_type = Convert.ToByte(type);
                byte[] byteDataFile = new byte[byteData.Length+1];
                byteDataFile[0] = file_type;
                Array.Copy(byteData, 0, byteDataFile, 1, byteData.Length); 

                // Send data to the server
                clientSocketFiles.BeginSend(byteDataFile, 0, byteDataFile.Length, SocketFlags.None, new AsyncCallback(this.SendFile), clientSocketFiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendFile Error: " + ex.Message);
            }
        }
        #endregion

        #region Callbacks

        /// <summary>
        /// Asyncronous callback function to handle the sending of data for messages
        /// </summary>
        /// <param name="ar"> the status of our asynchronous operation </param>
        private void SendData(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Data: " + ex.Message);
            }
        }

        /// <summary>
        /// Asyncronous callback function to handle the sending of files
        /// </summary>
        /// <param name="ar"> the status of our asynchronous operation </param>
        private void SendFile(IAsyncResult ar)
        {
            try
            {
                clientSocketFiles.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send File: " + ex.Message);
            }
        }

        /// <summary>
        /// Asyncronous callback function to handle the receiving of messages. Prints to the console and continues
        /// </summary>
        /// <param name="ar"> the status of our asynchronous operation </param>
        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(this.dataStream);
                
                // Initialise the IPEndPoint for the server
                IPEndPoint serverIPEP = new IPEndPoint(IPAddress.Parse(serverIPAddress), 8080);

                // Initialise the EndPoint for the clients
                EndPoint epServer = (EndPoint)serverIPEP;

                // Receive all data
                clientSocket.EndReceiveFrom(ar, ref epServer);

                // Print data received to the console
                Console.WriteLine("Incoming Message:");
                Console.WriteLine(receivedData.ChatMessage);
                Console.WriteLine("Press 'M' to send a message or 'F' to send a file. (esc to quit)"); 
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("Receive Data: " + ex.Message);
            }
        }

        /// <summary>
        /// Asyncronous callback function to handle the receiving of files. Ask if user wants it and then takes details for where to save it.
        /// </summary>
        /// <param name="ar"> the status of our asynchronous operation </param>
        private void ReceiveFile(IAsyncResult ar)
        {
            try
            {
                // Receive all data
                this.clientSocket.EndReceive(ar);

                

                // Notify user that a file was received and ask if they would like to accept
                Console.WriteLine("\nFile received! Would you like to accept it? (y/n)");
                ConsoleKeyInfo cki;
                cki = Console.ReadKey();
                while (cki.Key != ConsoleKey.Y && cki.Key != ConsoleKey.N)
                {
                    Console.WriteLine("Only press Y or N!");
                    cki = Console.ReadKey();
                }

                // If user doesn't want file - stop receiving and continue program
                if (cki.Key == ConsoleKey.N)
                {
                    return;
                }

                // Otherwise ask where he wants to save it and what he wants to name it...
                string path = "../../../../../";
                Console.WriteLine("\nWhere would you like to save it?\n(input path relative to the directory containing this application and end with a slash...)");
                path += Console.ReadLine();
                Console.WriteLine("What would you like to name it? (do not include extension)");
                path += Console.ReadLine();

                // Get file type, add to the path, and save file
                bool type = Convert.ToBoolean(fileDataStream[0]); // If true - json. If false - xml.
                byte[] fileStream = fileDataStream.Skip(1).Take(4095).ToArray();
                path += (type) ? ".json" : ".xml";
                File.WriteAllBytes(path, fileStream);

                Console.WriteLine("File saved!");
                Console.WriteLine("Press 'M' to send a message or 'F' to send a file. (esc to quit)"); 
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("Receive File: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Asyncronous callback function to handle the TCP connection
        /// </summary>
        /// <param name="ar"> the status of our asynchronous operation </param>
        private void Connect(IAsyncResult ar) {
            try {
                clientSocketFiles.EndConnect(ar);
            } catch (Exception ex) {
                Console.WriteLine("Connection attempt is unsuccessful: " + ex.Message);
            }
        }
        #endregion
    }
}
