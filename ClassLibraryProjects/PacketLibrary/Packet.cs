using System;
using System.Collections.Generic;
using System.Text;

namespace MessengerPacket
{
    // Packet Structure:
    // Description   -> |dataIdentifier|name length|message length|    name   |    message   |
    // Size in bytes -> |       4      |     4     |       4      |name length|message length|
    
    /// <summary>
    /// Packet class is a format for sending messages between client and server
    /// </summary>
    public class Packet
    {
        public enum DataIdentifier
        {
            Message,
            LogIn,
            LogOut,
            Null    
        }
        #region Private Members
        private DataIdentifier dataIdentifier; 
        private string name; 
        private string message;
        #endregion

        #region Public Properties
        /// <value> The type of message, namely- login, logout, or a text message... </value>
        public DataIdentifier ChatDataIdentifier
        {
            get { return dataIdentifier; }
            set { dataIdentifier = value; }
        }

        /// <value> Name of the client sending the message </value>
        public string ChatName
        {
            get { return name; }
            set { name = value; }
        }

        /// <value> The text of the message itself </value>
        public string ChatMessage
        {
            get { return message; }
            set { message = value; }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Packet()
        {
            this.dataIdentifier = DataIdentifier.Null;
            this.message = "";
            this.name = "";
        }

        /// <summary>
        /// Construct a Packet from input
        /// </summary>
        /// <param name="dataStream"> the input data to construct the packet out of </param>
        public Packet(byte[] dataStream)
        {
            // Read the data identifier from the beginning of the stream (4 bytes)
            this.dataIdentifier = (DataIdentifier)BitConverter.ToInt32(dataStream, 0);

            // Read the length of the name (4 bytes)
            int nameLength = BitConverter.ToInt32(dataStream, 4);

            // Read the length of the message (4 bytes)
            int msgLength = BitConverter.ToInt32(dataStream, 8);

            // Read the name field
            this.name = Encoding.UTF8.GetString(dataStream, 12, nameLength);

            // Read the message field
            this.message = Encoding.UTF8.GetString(dataStream, 12 + nameLength, msgLength);
        }

        /// <summary>
        /// Converts the packet into a byte array for sending/receiving
        /// </summary> 
        /// <return> The byte array we made </return>
        public byte[] GetDataStream()
        {
            List<byte> dataStream = new List<byte>();

            // Add the dataIdentifier
            dataStream.AddRange(BitConverter.GetBytes((int)this.dataIdentifier));

            // Add the name length
            dataStream.AddRange(BitConverter.GetBytes(this.name.Length));

            // Add the message length
            dataStream.AddRange(BitConverter.GetBytes(this.message.Length));

            // Add the name
            dataStream.AddRange(Encoding.UTF8.GetBytes(this.name));

            // Add the message
            dataStream.AddRange(Encoding.UTF8.GetBytes(this.message));

            return dataStream.ToArray();
        }

        #endregion
    }
}