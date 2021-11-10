using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace livelywpf.Helpers.IPC
{
    class PipeServer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<string[]> MessageReceived;
        public static string MsgDelimiter { get; } = "^_^";

        public PipeServer(string channelName)//, bool biDirection = false)
        {
            CreateRemoteService(channelName);
        }

        private async void CreateRemoteService(string channelName)
        {
            using (var pipeServer = new NamedPipeServerStream(channelName, PipeDirection.In))
            {
                while (true)
                {
                    await pipeServer.WaitForConnectionAsync().ConfigureAwait(false);
                    var reader = new StreamReader(pipeServer);
                    var rawArgs = await reader.ReadToEndAsync();
                    Logger.Info(rawArgs);
                    MessageReceived?.Invoke(this, rawArgs.Split(MsgDelimiter));
                    pipeServer.Disconnect();
                }
            }
        }
    }
}
