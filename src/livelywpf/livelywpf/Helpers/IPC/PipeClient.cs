using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace livelywpf.Helpers
{
    class PipeClient
    {
        public static void SendMessage(string channelName, string[] msg)
        {
            using (var pipeClient = new NamedPipeClientStream(".", channelName, PipeDirection.Out))
            {
                pipeClient.Connect(0);
                
                var sb = new StringBuilder();
                foreach (var item in msg)
                {
                    sb.Append(item);
                    sb.Append(PipeServer.MsgDelimiter);
                }
                sb.Remove((sb.Length - PipeServer.MsgDelimiter.Length), PipeServer.MsgDelimiter.Length);

                var writer = new StreamWriter(pipeClient) { AutoFlush = true };
                writer.Write(sb.ToString());
                writer.Flush();
                writer.Close();
                pipeClient.Dispose();
            }
        }
    }
}
