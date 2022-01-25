using System.Net.Sockets;
using System.Net;

using MessengerPacket;

namespace MessengerProjectClient
{
    public class Client
    {
        #region Private Members
        // Client socket
        private Socket clientSocket;

        // Client name
        private string name = "";
        // Default message
        private string msg = "";

        // Server End Point
        private EndPoint epServer;

        // Data stream
        private byte[] dataStream = new byte[1024];
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
            Connect();

            ConsoleKeyInfo cki;

            Console.WriteLine("\nYou can now send messages! Press the 'Enter/Return' key to start typing a message to send.\nNote: You will not be able to receive messages while typing...\nPress 'esc' to quit at any time.");

            while (true) 
            {
                // As long as client is not typing - listen for incoming messages
                while (Console.KeyAvailable == false)
                {
                    // Begin listening (receive messages)
                    Listen();
                }
                cki = Console.ReadKey();
                if (cki.Key == ConsoleKey.Escape) 
                {
                    Disconnect();
                    break;
                } 
                else if (cki.Key == ConsoleKey.Enter) 
                {
                    msg += Console.ReadLine();
                    Send();
                }
                Console.WriteLine("\nPress 'Enter/Return' to send another message.");  
            }
        }

        private void Connect()
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

        private void Listen()
        {
            // listen for broadcasts
            clientSocket.BeginReceiveFrom(this.dataStream, 0, this.dataStream.Length, SocketFlags.None, ref epServer, new AsyncCallback(this.ReceiveData), null);
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

                // Close the socket
                this.clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Closing Error: " + ex.Message, "UDP Client");
            }
        }

        private void Send()
        {
            try
            {
                // Initialise a packet object to store the data to be sent
                Packet sendData = new Packet();
                sendData.ChatName = this.name;
                sendData.ChatMessage = this.msg;
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
        #endregion

        #region Send And Receive
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
            catch (Exception ex)
            {
                Console.WriteLine("Receive Data: " + ex.Message, "UDP Client");
            }
        }
        #endregion
    }
}
