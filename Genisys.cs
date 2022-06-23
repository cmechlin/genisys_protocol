using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using PCLCP.Util;

namespace Genisys
{
    public class Genisys
    {
        private Timer _pollTimer;
        private Timer _recallTimer;

        public enum Modes
        {
            Slave, Master
        }

        public enum ByteTypes
        {
            Escape = 0xf0,
            Ack_Master = 0xf1,
            Indicaiton = 0xf2,
            Control_Checkback = 0xf3,
            // 0xf4, 0xf5, 0xf7 undefined
            End_Of_Message = 0xf6,
            Date_Time_Update = 0xf8,
            Common_Control = 0xf9,
            Ack_Slave = 0xfa,
            Poll = 0xfb,
            Control = 0xfc,
            Recall = 0xfd,
            Execute = 0xfe,
            Illegal = 0xff
        }

        public Genisys()
        {
            // Genisys protocol supports up to 256 bytes of controls and indications, or 2048 bits
            // Bytes 224 - 255 (0xE0 - 0xFF) are reserved for status type information.
            BitVector controls = new BitVector(2048);
            BitVector indications = new BitVector(2048);

            _pollTimer = new Timer();
            _pollTimer.Interval = PollTime;
            _pollTimer.Elapsed += onPoll;

            _recallTimer = new Timer();
            _recallTimer.Interval = RecallTime;
            _recallTimer.Elapsed += onRecall;
        }

        public void Start()
        {
            _pollTimer.Enabled = true;
            _recallTimer.Enabled = true;
        }

        public void Stop()
        {
            _pollTimer.Enabled = false;
            _recallTimer.Enabled = false;
        }

        public uint PollTime
        {
            get;
            set;
        }

        public uint RecallTime
        {
            get;
            set;
        }

        private void onPoll(Object sender, ElapsedEventArgs e)
        {

        }

        private void onRecall(Object sender, ElapsedEventArgs e)
        {

        }

        private void processMessage()
        {

        }
    }

    public class Connection
    {
        private IPEndPoint address;
        
        private UDPSocket socket;

        public Connection(string address, int port)
        {
            this.address = new IPEndPoint(IPAddress.Parse(address), port);


            socket = new UDPSocket();
            socket.Client(this.address);
        }

        private void onPoll(object sender, ElapsedEventArgs e)
        {
            socket.Send("0xFA");
            //throw new NotImplementedException();
        }


    }

    public class UDPSocket
    {
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public UDPSocket()
        {

        }

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(IPEndPoint address)
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socket.Bind(address);
            Receive();
        }

        public void Client(IPEndPoint address)
        {
            _socket.Connect(address);
            Receive();
        }

        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
                Console.WriteLine("TX: {0}, {1}", bytes, text);
            }, state);
        }

        private void Receive()
        {
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                Console.WriteLine("RX: {0}: {1}, {2}", epFrom.ToString(), bytes, Encoding.ASCII.GetString(so.buffer, 0, bytes));
            }, state);

        }
    }
}
