using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Genisys
{
    public class Genisys
    {
        public enum Mode { Slave = 0, Master = 1 };

        public Genisys()
        {


        }

        private void processMessage()
        {

        }
    }

    public class Connection
    {
        /*
         * Eventually support both network and serial connections 
         */
        public Controls controls;
        public Indications indications;
        private IPEndPoint address;
        private Timer pollTimer;
        private UDPSocket socket;

        public Connection(string address, int port)
        {
            this.address = new IPEndPoint(IPAddress.Parse(address), port);
            pollTimer = new Timer();
            pollTimer.Interval = 1000;
            pollTimer.Elapsed += onPoll;
            controls = new Controls(this);
            indications = new Indications(this);

            socket = new UDPSocket();
            socket.Client(this.address);
        }

        private void onPoll(object sender, ElapsedEventArgs e)
        {
            socket.Send("0xFA");
            //throw new NotImplementedException();
        }

        public void Start()
        {
            pollTimer.Enabled = true;
        }

        public void Stop()
        {
            pollTimer.Enabled = false;
        }
    }

    public class Controls
    {
        private bool[] localArray = new bool[65535];
        Connection conn;

        public Controls(Connection conn)
        {
            this.conn = conn;
        }

        public bool this[int x]
        {
            get { return this.localArray[x]; }
            set
            {
                this.localArray[x] = value;
            }
        }
    }

    public class Indications
    {
        private bool[] localArray = new bool[65535];
        Connection conn;

        public Indications(Connection conn)
        {
            this.conn = conn;
        }

        public bool this[int x]
        {
            get { return this.localArray[x]; }
            set
            {
                this.localArray[x] = value;
            }
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
