using Grpc.Core;
using Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    internal class ChatAppQuicService : Protos.ChatAppQuic.ChatAppQuicBase
    {

        private readonly IClientList clientList;

        public ChatAppQuicService(IClientList clientList)
        {
            ArgumentNullException.ThrowIfNull(clientList, nameof(clientList));

            this.clientList = clientList;
        }

        public override Task SessionExecute(
            IAsyncStreamReader<Request> requestStream,
            IServerStreamWriter<Response> responseStream,
            ServerCallContext context)
        {
            return base.SessionExecute(requestStream, responseStream, context);
        }

        public override async Task (IAsyncStreamReader<Protos.Request> requestStream, IServerStreamWriter<Protos.Response> responseStream, ServerCallContext context)
        {
            await foreach (var requestMessage in requestStream.ReadAllAsync())
            {
                await _chatHub.HandleIncomingMessage(requestMessage, responseStream);
            }
        }
    }
}
