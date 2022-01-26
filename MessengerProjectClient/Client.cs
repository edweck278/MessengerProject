using System.Net.Sockets;
using System.Net;
using System.IO;

using MessengerPacket;

namespace MessengerProjectClient
{
    public class Client
    {
        #region Private Members
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

        public Client()
        {
            Client_Load();
        }
        private void Client_Load()
        {
            // Welcome and connect
            Console.WriteLine("Welcome to messenger!");
            Console.WriteLine("To log on, please enter your name:");
            this.name += Console.ReadLine();
            ConnectForMessages();
            ConnectForFiles();

            ConsoleKeyInfo cki;
            string msg = "";
            string file_path = "";

            Console.WriteLine("You can now communicate!");
            Console.WriteLine("Press the 'M' key to start typing a message to send.");
            Console.WriteLine("Press the 'F' key to send a file.");
            Console.WriteLine("Note: You will not be able to receive while sending...");
            Console.WriteLine("Press 'esc' to quit at any time.");

            while (true) 
            {
                // As long as client is not typing - listen for incoming data
                while (Console.KeyAvailable == false)
                {
                    //TODO wait a second?
                    // Begin listening (receive messages and files)
                    Listen();
                }
                cki = Console.ReadKey();
                switch (cki.Key)
                {
                    case ConsoleKey.Escape:
                        Disconnect();
                        break;
                    case ConsoleKey.M:
                        Console.WriteLine("Type your message:");
                        msg += Console.ReadLine();
                        Send(msg);
                        msg = "";
                        break;
                    case ConsoleKey.F:
                        Console.WriteLine("Which file would you like to send? (must be .json or .xml)\nInput full path:");
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
                Console.WriteLine("Press 'M' to send a message or 'F' to send a file.");  
            }
        }

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
                IPAddress serverIP = IPAddress.Parse("127.0.0.1");

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
                Console.WriteLine("Connection Error: " + ex.Message, "UDP Client");
            }
        }

        private void ConnectForFiles()
        {
            // Intitialize Socket
            clientSocketFiles = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Initialise server IP
            IPAddress serverIP = IPAddress.Parse("127.0.0.1");

            // Initialise the IPEndPoint for the server and use port 8081
            IPEndPoint server = new IPEndPoint(serverIP, 8081);

            // Initialise the EndPoint for the server
            epServerFiles = (EndPoint)server;

            clientSocketFiles.BeginConnect(serverIP, 8081, endConnect, null);
        }

        private void Listen()
        {
            // listen for message broadcasts
            clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epServer, new AsyncCallback(this.ReceiveData), null);

            // listen for file broadcasts
            clientSocketFiles.BeginReceiveFrom(this.fileDataStream, 0, this.fileDataStream.Length, SocketFlags.None, ref epServerFiles, new AsyncCallback(this.ReceiveFile), null);
        }

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
                Console.WriteLine("Closing Error: " + ex.Message, "UDP Client");
            }
        }

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
                Console.WriteLine("Send Error: " + ex.Message, "UDP Client");
            }
        }

        private void SendAFile(string file_path)
        {
            try
            {
                // TODO
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendFile Error: " + ex.Message, "TCP Client");
            }
        }
        #endregion

        #region Callbacks
        private void SendData(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send Data: " + ex.Message, "UDP Client");
            }
        }

        private void SendFile(IAsyncResult ar)
        {
            // TODO
        }

        private void ReceiveData(IAsyncResult ar)
        {
            try
            {
                // Receive all data
                this.clientSocket.EndReceive(ar);

                // Initialise a packet object to store the received data
                Packet receivedData = new Packet(this.dataStream);

                // Print data received to the console
                Console.WriteLine("Incoming Message:");
                Console.WriteLine(receivedData.ChatMessage);
                
                // Reset data stream
                this.dataStream = new byte[1024];
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("Receive Data: " + ex.Message, "UDP Client");
            }
        }

        private void ReceiveFile(IAsyncResult ar)
        {
            try
            {
                // Receive all data
                this.clientSocket.EndReceive(ar);

                // Notify user that a file was received and ask if they would like to accept
                Console.WriteLine("File received! Would you like to accept it? (y/n)");
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
                string path = "";
                Console.WriteLine("Where would you like to save it? (input path ending with a slash...)");
                path += Console.ReadLine();
                Console.WriteLine("What would you like to name it? (do not include extension)");
                path += Console.ReadLine();

                // Get file type, add to the path, and save file
                bool type = Convert.ToBoolean(fileDataStream[0]); // If true - json. If false - xml.
                byte[] fileStream = fileDataStream.Skip(1).Take(4095).ToArray();
                path += (type) ? ".json" : ".xml";
                File.WriteAllBytes(path, fileStream);
                
                // Reset data stream
                this.fileDataStream = new byte[4096];
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                Console.WriteLine("Receive File: " + ex.Message, "TCP Client");
            }
        }
        
        private void endConnect(IAsyncResult ar) {
            try {
                clientSocketFiles.EndConnect(ar);
            } catch (Exception ex) {
                Console.WriteLine("Connection attempt is unsuccessful: " + ex.Message, "TCP Client");
            }
        }

        #endregion
    }
}
