using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EVESharp.Common.Logging.Streams
{
    public class FileLogStream : ILogStream
    {
        private Queue<StreamMessage> mQueue = new Queue<StreamMessage>();
        private readonly FileStream mFile = null;
        private readonly StreamWriter mWriter = null;

        public FileLogStream(Configuration.FileLog configuration)
        {
            // create directory and log file
            if (Directory.Exists(configuration.Directory) == false)
                Directory.CreateDirectory(configuration.Directory);

            this.mFile = File.OpenWrite(Path.Join(configuration.Directory, configuration.LogFile));
            this.mWriter = new StreamWriter(this.mFile);
        }

        public void Write(MessageType messageType, string message, Channel channel)
        {
            StreamMessage entry = new StreamMessage(messageType, message, channel);

            lock (this.mQueue)
                this.mQueue.Enqueue(entry);
        }

        public void Flush()
        {
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

                this.mWriter.Write(
                    $"{entry.Time.DateTime.ToShortDateString()} {entry.Time.DateTime.ToShortTimeString()} {entry.Type.ToString()[0]} " +
                    $"{entry.Channel.Name}: {entry.Message}{Environment.NewLine}"
                );
            }

            this.mFile.Flush();
        }
    }
}