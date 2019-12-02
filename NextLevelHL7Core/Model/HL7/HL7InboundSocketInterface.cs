using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NextLevelHL7
{
    public class HL7InboundSocketInterface : BaseHL7Interface
    {
        private bool _Cancelled = false;
        private Socket _Listener;

        public int Port { get; private set; }
        public bool PersistConnection { get; set; } = true;

        private ConcurrentBag<Socket> _Connections = new ConcurrentBag<Socket>();

        public HL7InboundSocketInterface(string name, int port = 2575)
        {
            Name = name;
            Port = port;
        }

        protected override bool OnStart()
        {
            _Cancelled = false;

            IPAddress address = null;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    address = ip;
                    break;
                }

            if (address == null)
                throw new Exception("Local IP address not found");

            IPEndPoint endPoint = new IPEndPoint(address, Port);

            string history = string.Empty;

            _Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _Listener.Bind(endPoint);
                _Listener.Listen(32);

                // Reference for supporting multiple connections on one listening socket
                // http://stackoverflow.com/questions/19387086/how-to-set-up-tcplistener-to-always-listen-and-accept-multiple-connections

                while (!_Cancelled)
                {
                    WriteStatus("Listening on port {0}", endPoint);

                    byte[] buffer = new byte[4096];
                    string data = string.Empty;

                    // handle incoming connection ...
                    Socket handler = _Listener.Accept();
                    _Connections.Add(handler);

                    handler.ReceiveTimeout = TimeSpan.FromMinutes(1).Milliseconds;
                    handler.SendTimeout = TimeSpan.FromMinutes(1).Milliseconds;

                    WriteStatus("Connection established");

                    bool errored = false;
                    while (!_Cancelled && !errored)
                    {
                        try
                        {
                            if (!IsSocketConnected(handler))
                            {
                                WriteStatus("Socket Error - connection recycling");
                                errored = true;
                                continue;
                            }

                            int count = handler.Receive(buffer);
                            data += Encoding.UTF8.GetString(buffer, 0, count);

                            if (string.IsNullOrEmpty(data))
                            {
                                // if we aren't receiving data and getting nulls from the interface engine 
                                // let's recycle the connection
                                if (Statistics.HasReceivedMessage(TimeSpan.FromMinutes(1)))
                                {
                                    Thread.Sleep(TimeSpan.FromSeconds(1));
                                    continue;
                                }
                                else
                                {
                                    WriteStatus("Not receiving - connection recycling");
                                    Start();
                                }
                            }

                            // Find start of MLLP frame, a VT character ...
                            int start = data.IndexOf((char)0x0B);
                            if (start >= 0)
                            {
                                // Now look for the end of the frame, a FS character
                                int end = data.IndexOf((char)0x1C);
                                if (end > start)
                                {
                                    string temp = data.Substring(start + 1, end - start);

                                    // process message
                                    Message message = null;
                                    message = ParseMessage(temp);

                                    // Send response
                                    string acknowledgement = CreateAcknowledgement(message.MessageControlId());
                                    handler.Send(Encoding.UTF8.GetBytes(acknowledgement));

                                    // process message
                                    WriteMessage(message);

                                    if (!PersistConnection)
                                        break;

                                    if (LogMessages)
                                        WriteStatus(temp);

                                    int numberCharacters = data.Length - end - 2;
                                    if (numberCharacters == 0)
                                        data = string.Empty;
                                    else
                                        data = data.Substring(end + 1, numberCharacters + 1);
                                }

                            }
                        }
                        catch (SocketException socketException1)
                        {
                            switch (socketException1.SocketErrorCode)
                            {
                                #region hide 
                                case SocketError.TimedOut:
                                case SocketError.SocketError:
                                case SocketError.Interrupted:
                                case SocketError.AccessDenied:
                                case SocketError.Fault:
                                case SocketError.InvalidArgument:
                                case SocketError.TooManyOpenSockets:
                                case SocketError.WouldBlock:
                                case SocketError.InProgress:
                                case SocketError.AlreadyInProgress:
                                case SocketError.NotSocket:
                                case SocketError.DestinationAddressRequired:
                                case SocketError.MessageSize:
                                case SocketError.ProtocolType:
                                case SocketError.ProtocolOption:
                                case SocketError.ProtocolNotSupported:
                                case SocketError.SocketNotSupported:
                                case SocketError.OperationNotSupported:
                                case SocketError.ProtocolFamilyNotSupported:
                                case SocketError.AddressFamilyNotSupported:
                                case SocketError.AddressAlreadyInUse:
                                case SocketError.AddressNotAvailable:
                                case SocketError.NetworkDown:
                                case SocketError.NetworkUnreachable:
                                case SocketError.NetworkReset:
                                case SocketError.ConnectionAborted:
                                case SocketError.ConnectionReset:
                                case SocketError.NoBufferSpaceAvailable:
                                case SocketError.IsConnected:
                                case SocketError.NotConnected:
                                case SocketError.Shutdown:
                                case SocketError.ConnectionRefused:
                                case SocketError.HostDown:
                                case SocketError.HostUnreachable:
                                case SocketError.ProcessLimit:
                                case SocketError.SystemNotReady:
                                case SocketError.VersionNotSupported:
                                case SocketError.NotInitialized:
                                case SocketError.Disconnecting:
                                case SocketError.TypeNotFound:
                                case SocketError.HostNotFound:
                                case SocketError.TryAgain:
                                case SocketError.NoRecovery:
                                case SocketError.NoData:
                                case SocketError.IOPending:
                                case SocketError.OperationAborted:
                                #endregion
                                default:
                                    WriteStatus(socketException1.Message);
                                    WriteStatus("Connection status: " + socketException1.SocketErrorCode.ToString());
                                    WriteStatus("Connection recycling. ...");
                                    errored = true;
                                    continue;
                            }
                        }
                        catch (Exception ex1)
                        {
                            Statistics.AddFailure();
                            WriteStatus(ex1.Message);
                            WriteStatus("Exception caught parsing frame: {0}", ex1.Message);
                        }
                    } // end socket handler loop

                    // close connection
                    _Connections.TryTake(out handler);
                    if (handler != null)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                    WriteStatus("Connection closed");
                }
            }
            catch (SocketException socketException2)
            {
                switch (socketException2.SocketErrorCode)
                {
                    #region hide
                    case SocketError.TimedOut:
                    case SocketError.SocketError:
                    case SocketError.Interrupted:
                        WriteStatus("Connection aborted", endPoint);
                        break;
                    case SocketError.AccessDenied:
                    case SocketError.Fault:
                    case SocketError.InvalidArgument:
                    case SocketError.TooManyOpenSockets:
                    case SocketError.WouldBlock:
                    case SocketError.InProgress:
                    case SocketError.AlreadyInProgress:
                    case SocketError.NotSocket:
                    case SocketError.DestinationAddressRequired:
                    case SocketError.MessageSize:
                    case SocketError.ProtocolType:
                    case SocketError.ProtocolOption:
                    case SocketError.ProtocolNotSupported:
                    case SocketError.SocketNotSupported:
                    case SocketError.OperationNotSupported:
                    case SocketError.ProtocolFamilyNotSupported:
                    case SocketError.AddressFamilyNotSupported:
                    case SocketError.AddressAlreadyInUse:
                        WriteStatus("Socket address and port already in use.  Recycling.", endPoint);
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        OnStart();
                        break;
                    case SocketError.AddressNotAvailable:
                    case SocketError.NetworkDown:
                    case SocketError.NetworkUnreachable:
                    case SocketError.NetworkReset:
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset:
                    case SocketError.NoBufferSpaceAvailable:
                    case SocketError.IsConnected:
                    case SocketError.NotConnected:
                    case SocketError.Shutdown:
                    case SocketError.ConnectionRefused:
                    case SocketError.HostDown:
                    case SocketError.HostUnreachable:
                    case SocketError.ProcessLimit:
                    case SocketError.SystemNotReady:
                    case SocketError.VersionNotSupported:
                    case SocketError.NotInitialized:
                    case SocketError.Disconnecting:
                    case SocketError.TypeNotFound:
                    case SocketError.HostNotFound:
                    case SocketError.TryAgain:
                    case SocketError.NoRecovery:
                    case SocketError.NoData:
                    case SocketError.IOPending:
                    case SocketError.OperationAborted:
                    #endregion
                    default:
                        WriteStatus(socketException2.Message);
                        WriteStatus("Connection status: " + socketException2.SocketErrorCode.ToString());
                        break;
                }
            }
            catch (Exception ex2)
            {
                WriteStatus("Exception caught establishing connection: {0}", ex2.Message);
            }

            return true;
        }

        protected override bool OnStop()
        {
            _Cancelled = true;

            // shut down open connections
            foreach (Socket socket in _Connections)
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch
                {
                }
            _Connections = new ConcurrentBag<Socket>();

            // shut down socket listener
            if (_Listener != null)
            {
                try
                {
                    _Listener.Close();
                    _Listener.Dispose();
                }
                catch
                {
                }
                _Listener = null;
            }
            return true;
        }

        private static bool IsSocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected)
                return false;
            else
                return true;
        }
    }
}
