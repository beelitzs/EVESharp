using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using EVESharp.Common.Configuration;
using EVESharp.Common.Network;

namespace EVESharp.Common.Logging.Streams
{
    public class LogLiteStream : ILogStream
    {
        enum ConnectionMessage
        {
            CONNECTION_MESSAGE,
            SIMPLE_MESSAGE,
            LARGE_MESSAGE,
            CONTINUATION_MESSAGE,
            CONTINUATION_END_MESSAGE,
        }

        enum Severity
        {
            SEVERITY_INFO,
            SEVERITY_NOTICE,
            SEVERITY_WARN,
            SEVERITY_ERR,
        }

        private static readonly Dictionary<MessageType, Severity> MessageTypeToSeverity = new Dictionary<MessageType, Severity>()
        {
            {MessageType.Info, Severity.SEVERITY_INFO},
            {MessageType.Debug, Severity.SEVERITY_INFO},
            {MessageType.Error, Severity.SEVERITY_ERR},
            {MessageType.Fatal, Severity.SEVERITY_ERR},
            {MessageType.Trace, Severity.SEVERITY_NOTICE},
            {MessageType.Warning, Severity.SEVERITY_WARN}
        };
        
        private const int PROTOCOL_VERSION = 2;

        private Queue<StreamMessage> mQueue = new Queue<StreamMessage>();

        private readonly EVEClientSocket mSocket = null;
        private Channel Log { get; }
        private bool Enabled { get; set; }
        private string Name { get; }
        private string ExecutablePath { get; }
        private long PID { get; }

        public LogLiteStream(string name, Logger logger, LogLite configuration)
        {
            this.Log = logger.CreateLogChannel("LogLiteBridge");

            this.Enabled = true;
            this.PID = Process.GetCurrentProcess().Id;
            this.ExecutablePath = Process.GetCurrentProcess().ProcessName;
            this.Name = name;
            this.mSocket = new EVEClientSocket(this.Log);
            this.mSocket.SetOnConnectionLostHandler(OnConnectionLost);
            this.mSocket.SetExceptionHandler(OnException);
            this.mSocket.Connect(configuration.Hostname, int.Parse(configuration.Port));

            // send the connection message
            this.SendConnectionMessage();
        }

        void OnException(Exception ex)
        {
            Log.Error($"Unhandled exception: {ex.Message}");
        }

        void OnConnectionLost()
        {
            this.Enabled = false;
            Log.Fatal("LogLite connection lost. The LogLiteStream is disabled");
        }

        private void SendConnectionMessage()
        {
            // prepare the machineName and executablePath
            byte[] machineName = new byte[32];
            Encoding.ASCII.GetBytes(this.Name, 0, Math.Min(31, this.Name.Length), machineName, 0);

            byte[] executablePath = new byte[260];
            Encoding.ASCII.GetBytes(this.ExecutablePath, 0, Math.Min(259, this.ExecutablePath.Length), executablePath, 0);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            using (stream)
            using (writer)
            {
                writer.Write((int) ConnectionMessage.CONNECTION_MESSAGE);
                writer.Write((int) 0);
                writer.Write((uint) PROTOCOL_VERSION);
                writer.Write((int) 0);
                writer.Write((long) this.PID);
                writer.Write(machineName);
                writer.Write(executablePath);
                // fill the packet with empty data to fill the 344 size in packets
                writer.Write(new byte[344 - (4 + 4 + 4 + 4 + 8 + 32 + 260)]);

                this.mSocket.Send(stream.ToArray());
            }
        }

        private void SendTextMessage(StreamMessage message)
        {
            byte[] module = new byte[32];
            byte[] channel = new byte[32];
            byte[] byteMessage = new byte[256];

            Encoding.ASCII.GetBytes(message.Channel.Name, 0, Math.Min(31, message.Channel.Name.Length), channel, 0);
            Encoding.ASCII.GetBytes(message.Message, 0, Math.Min(255, message.Message.Length), byteMessage, 0);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            using (stream)
            using (writer)
            {
                if (message.Message.Length > 255)
                {
                    int offset = 255;

                    writer.Write((uint) ConnectionMessage.LARGE_MESSAGE);
                    writer.Write((int) 0);
                    writer.Write((ulong) message.Time.ToUnixTimeMilliseconds());
                    writer.Write((uint) MessageTypeToSeverity[message.Type]);
                    writer.Write(module);
                    writer.Write(channel);
                    writer.Write(byteMessage);
                    // fill the packet with empty data to fill the 344 size in packets
                    writer.Write(new byte[344 - (4 + 4 + 8 + 4 + 32 + 32 + 256)]);

                    while (offset < message.Message.Length)
                    {
                        byteMessage = new byte[256];
                        Encoding.ASCII.GetBytes(message.Message, offset, Math.Min(255, message.Message.Length - offset), byteMessage, 0);
                        
                        if ((message.Message.Length - offset) > 255)
                            writer.Write((uint) ConnectionMessage.CONTINUATION_MESSAGE);
                        else
                            writer.Write((uint) ConnectionMessage.CONTINUATION_END_MESSAGE);

                        writer.Write((int) 0);
                        writer.Write((ulong) message.Time.ToUnixTimeMilliseconds());
                        writer.Write((uint) MessageTypeToSeverity[message.Type]);
                        writer.Write(module);
                        writer.Write(channel);
                        writer.Write(byteMessage);
                        // fill the packet with empty data to fill the 344 size in packets
                        writer.Write(new byte[344 - (4 + 4 + 8 + 4 + 32 + 32 + 256)]);

                        offset += 255;
                    }
                }
                else
                {
                    writer.Write((uint) ConnectionMessage.SIMPLE_MESSAGE);
                    writer.Write((int) 0);
                    writer.Write((ulong) message.Time.ToUnixTimeMilliseconds());
                    writer.Write((uint) MessageTypeToSeverity[message.Type]);
                    writer.Write(module);
                    writer.Write(channel);
                    writer.Write(byteMessage);
                    // fill the packet with empty data to fill the 344 size in packets
                    writer.Write(new byte[344 - (4 + 4 + 8 + 4 + 32 + 32 + 256)]);
                }

                this.mSocket.Send(stream.ToArray());
            }
        }

        public void Write(MessageType messageType, string message, Channel channel)
        {
            // prevent queueing messages if the logger is stopped
            if (this.Enabled == false)
                return;

            // ensure that long messages are not sent as is, they could crash the LogLite
            if (message.Length >= 6000)
            {
                // build a new message that only has parts of the original
                message = message.Substring(0, 2048) + "\n[...]\n" + message.Substring(message.Length - 2048);
            }
            
            StreamMessage entry = new StreamMessage(messageType, message, channel);

            lock (this.mQueue)
                this.mQueue.Enqueue(entry);
        }

        public void Flush()
        {
            // prevent queueing messages if the logger is stopped
            if (this.Enabled == false)
                return;

            Queue<StreamMessage> queue;

            lock (this.mQueue)
            {
                // if there is no message pending there is not an actual reason to flush the stream
                // so just release the semaphore and return
                if (this.mQueue.Count == 0)
                {
                    return;
                }

                // clone the queue so the services that need to write messages do not have to wait for the write to finish
                queue = this.mQueue;
                this.mQueue = new Queue<StreamMessage>();
            }

            while (queue.Count > 0)
            {
                StreamMessage entry = queue.Dequeue();

                // send text message
                this.SendTextMessage(entry);
            }
        }
    }
}