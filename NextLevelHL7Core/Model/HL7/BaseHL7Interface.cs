using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextLevelHL7
{
    public abstract class BaseHL7Interface : IEHRInterface
    {
        /// <summary>
        /// Occurs when an HL7 message is received.
        /// </summary>
        public event EventHandler<Message> MessageEvent;

        /// <summary>
        /// Occurs when an interface changes status.
        /// </summary>
        public event EventHandler<InterfaceStatusEvent> StatusEvent;

        /// <summary>
        /// Occurs when an error is encountered during connection, transport, or message parsing.
        /// </summary>
        public event EventHandler<Exception> ErrorEvent;

        /// <summary>
        /// Gets or sets a unique identifier which represents this interface instance.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the interface.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Configures whether this interface should send acknowledgements for guaranteed message delivery.
        /// </summary>
        public bool SendAcknowledgements { get; set; } = true;

        /// <summary>
        /// Configures whether this interface should log messages received
        /// </summary>
        public bool LogMessages { get; set; }

        /// <summary>
        /// Gets a value which indicates whether this interface is currently running.
        /// </summary>
        public bool IsRunning { get; protected set; } = false;

        /// <summary>
        /// Gets interface statistics which track up-time, delivery statistics, and message failures.
        /// </summary>
        public InterfaceStatistics Statistics { get; private set; } = new InterfaceStatistics();

        /// <summary>
        /// Configures the default character used to indicate the start of an HL7 message carried within an MLLP frame.
        /// </summary>
        public byte HL7MessageStartCharacter { get; set; } = 0x0B;

        /// <summary>
        /// Configures the default character used to indicate the end of an HL7 message carried within an MLLP frame.
        /// </summary
        public byte HL7MessageEndCharacter { get; set; } = 0x1C;

        /// <summary>
        /// Configures the default character used to denote the end of an MLLP frame.
        /// </summary>
        public byte HL7FrameEndCharacter { get; set; } = 0x0D;

        public string HL7DateTimeFormat { get; set; } = "yyyyMMddhhmmss";

        public string HL7AcknowledgementVersion { get; set; } = "2.3";

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseHL7Interface" /> class.
        /// </summary>
        public BaseHL7Interface()
        {
            Id = Guid.NewGuid().ToString();
        }

        protected abstract bool OnStart();
        protected abstract bool OnStop();

        /// <summary>
        /// Starts the interface.
        /// </summary>
        /// <param name="quiet">If true, starts an interface without logging a change in status</param>
        /// <returns></retur
        public bool Start(bool quiet = false)
        {
            if (!quiet)
            {
                WriteStatus("Starting");
                WriteStatus("LogMessages=" + LogMessages.ToString());
                WriteStatus("SendAcknowledgements=" + SendAcknowledgements.ToString());
            }
            Stop(quiet);
            IsRunning = true;
            return OnStart();
        }

        /// <summary>
        /// Stops an interface.
        /// </summary>
        /// <param name="quiet">If true, stops an interface without logging a change in status</param>
        /// <returns></returns>
        public bool Stop(bool quiet = false)
        {
            if (IsRunning)
            {
                if (!quiet)
                    WriteStatus("Stopping");
                bool result = OnStop();
                IsRunning = false;
                return result;
            }
            return false;
        }

        /// <summary>
        /// Starts the interface.
        /// </summary>
        /// <param name="quiet">If true, starts an interface without logging a change in status</param>
        /// <returns></returns>
        public Task<bool> StartAsync(bool quiet = false)
        {
            return StartAsync(CancellationToken.None, quiet);
        }

        /// <summary>
        /// Starts the interface.
        /// </summary>
        /// <param name="quiet">If true, starts an interface without logging a change in status</param>
        /// <returns></returns>
        public Task<bool> StartAsync(CancellationToken cancellationToken, bool quiet = false)
        {
            return Task.Run(() =>
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                bool result = Start(quiet);
                taskCompletionSource.SetResult(result);
                return taskCompletionSource.Task;
            }, cancellationToken);
        }

        /// <summary>
        /// Stops an interface.
        /// </summary>
        /// <param name="quiet">If true, stops an interface without logging a change in status</param>
        /// <returns></returns>
        public Task<bool> StopAsync(bool quiet = false)
        {
            return StopAsync(CancellationToken.None, quiet);
        }

        /// <summary>
        /// Stops an interface.
        /// </summary>
        /// <param name="quiet">If true, stops an interface without logging a change in status</param>
        /// <returns></returns>
        public Task<bool> StopAsync(CancellationToken cancellationToken, bool quiet = false)
        {
            return Task.Run(() =>
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                bool result = Stop(quiet);
                taskCompletionSource.SetResult(result);
                return taskCompletionSource.Task;
            }, cancellationToken);
        }

        protected string CreateAcknowledgement(string messageControlID)
        {
            Message response = new Message();

            Segment msh = new Segment("MSH");
            msh.SetField(2, "^~\\&");
            msh.SetField(7, DateTime.Now.ToString(HL7DateTimeFormat));
            msh.SetField(9, "ACK");
            msh.SetField(10, Guid.NewGuid().ToString());
            msh.SetField(11, "P");
            msh.SetField(12, HL7AcknowledgementVersion);
            response.Add(msh);

            Segment msa = new Segment("MSA");
            msa.SetField(1, "AA");
            msa.SetField(2, messageControlID);
            response.Add(msa);

            StringBuilder frame = new StringBuilder();
            frame.Append((char)HL7MessageStartCharacter);
            frame.Append(response.Serialize());
            frame.Append((char)HL7MessageEndCharacter);
            frame.Append((char)HL7FrameEndCharacter);

            return frame.ToString();
        }

        protected Message ParseMessage(string inputHL7Message)
        {
            try
            {
                Message message = new Message();
                message.Parse(inputHL7Message);
                return message;

            }
            catch
            {
                WriteError(new Exception("Exception caught parsing message."));
                throw;
            }
        }

        protected void WriteMessage(Message message)
        {
            Statistics.AddSuccess(message.MessageType());
            MessageEvent?.Invoke(this, message);
        }

        protected void WriteStatus(string text, params object[] args)
        {
            text = string.Format(text, args);
            StatusEvent?.Invoke(this, new InterfaceStatusEvent(text));
        }

        protected void WriteError(Exception exception)
        {
            ErrorEvent?.Invoke(this, exception);
        }
    }
}
