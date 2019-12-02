using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextLevelHL7
{
    public class HL7OutboundSocketInterface : BaseHL7Interface
    {
        private CancellationTokenSource _CancellationTokenSource;
        private ConcurrentQueue<Message> _OutboundMessageQueue = new ConcurrentQueue<Message>();
        private Socket _Socket = null;

        public string IPAddress { get; private set; }
        public int Port { get; private set; }

        public HL7OutboundSocketInterface(string name, string ipAddress, int port)
        {
            Name = name;
            IPAddress = ipAddress;
            Port = port;
        }

        public void EnqueueMessage(string hl7Text)
        {
            Message message = new Message();
            message.Parse(hl7Text);
            EnqueueMessage(message);
        }

        public void EnqueueMessage(Message message)
        {
            _OutboundMessageQueue.Enqueue(message);
        }

        protected override bool OnStart()
        {
            _CancellationTokenSource = new CancellationTokenSource();
            Task task = ProcessMessages();
            return true;
        }
        protected override bool OnStop()
        {
            _CancellationTokenSource.Cancel(false);
            return true;
        }

        private async Task ProcessMessages()
        {
            bool connectionErrorShown = false;
            while (!_CancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    Message message;
                    if (_OutboundMessageQueue.TryPeek(out message))
                    {
                        if (SendHL7(IPAddress, Port, message.Text))
                        {
                            _OutboundMessageQueue.TryDequeue(out message);
                            WriteMessage(message);
                        }
                        else
                        {
                            WriteError(new Exception("Failure Sending HL7 Message"));
                            if (!connectionErrorShown)
                            {
                                connectionErrorShown = true;
                                WriteError(new Exception("Failure connecting to HL7 Endpoint " + IPAddress + ":" + Port));
                            }

                            await Task.Delay(TimeSpan.FromSeconds(10), _CancellationTokenSource.Token);
                        }
                    }
                }
                catch(Exception e)
                {
                    WriteError(e);
                }

                if (_OutboundMessageQueue.Count == 0)
                    await Task.Delay(TimeSpan.FromSeconds(1), _CancellationTokenSource.Token);
            }
        }


        private bool SendHL7(string server, int port, string hl7message)
        {
            bool success = false;
            try
            {
                // add leading and trailing characters for MLLP complaince
                string hl7Request = Convert.ToChar(11).ToString() + hl7message + Convert.ToChar(28).ToString() + Convert.ToChar(13).ToString();

                // get the size of the message that we have to send.
                byte[] bytesSent = Encoding.ASCII.GetBytes(hl7Request);
                byte[] bytesReceived = new byte[256];

                // create a socket connection with the specified server and port.
                if (_Socket == null)
                    _Socket = ConnectSocket(server, port);

                // exit gracefully if socket connection cannot be established
                if (_Socket == null)
                    success = false;
                else
                {
                    // send message to the server.
                    _Socket.SendTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
                    _Socket.Send(bytesSent, bytesSent.Length, 0);

                    // receive response from server
                    int bytes = 0;
                    _Socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
                    bytes = _Socket.Receive(bytesReceived, bytesReceived.Length, 0);
                    string response = Encoding.ASCII.GetString(bytesReceived, 0, bytes);

                    // check for success
                    success = response.Contains("MSA|AA");
                }
            }
            catch (Exception)
            {
                if (_Socket != null)
                    try
                    {
                        _Socket.Close();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _Socket = null;
                    }
                success = false;
            }

            return success;
        }

        private static Socket ConnectSocket(string server, int port)
        {
            Socket socket = null;
            IPHostEntry hostEntry = null;

            // Get host related information.
            hostEntry = Dns.GetHostEntry(server);

            foreach (IPAddress address in hostEntry.AddressList)
            {
                try
                {
                    IPEndPoint endpoint = new IPEndPoint(address, port);
                    Socket temporarySocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    temporarySocket.Connect(endpoint);

                    if (temporarySocket.Connected)
                    {
                        socket = temporarySocket;
                        break;
                    }
                }
                catch
                {
                    // intentionally empty, hide socket connection failures
                }
            }
            return socket;
        }
    }
}
