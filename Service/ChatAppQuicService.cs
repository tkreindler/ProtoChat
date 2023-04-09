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

        /// <summary>
        /// Main entrypoint of the service. Called when a new client establishes a new bidirectional session
        /// </summary>
        /// <param name="requestStream">Incoming stream with async messages from the client</param>
        /// <param name="responseStream">Outgoing stream to send messages to the client</param>
        /// <returns></returns>
        public override async Task SessionExecute(
            IAsyncStreamReader<Request> requestStream,
            IServerStreamWriter<Response> responseStream,
            ServerCallContext _)
        {
            // assign a new user for this session
            Guid userIdentifier = Guid.NewGuid();

            Response response = new Response
            {
                Message = new MessageResponse
                {
                    Message = "Someone joined the chat",
                },
            };

            IReadOnlyCollection<IServerStreamWriter<Response>> list = await this.clientList.GetGlobalMessageList(userIdentifier);
            foreach (var item in list)
            {
                await item.WriteAsync(response);
            }

            // create a new object to handle
            IClientServicer clientServicer = new ClientServicer(userIdentifier, requestStream, this.clientList);

            await this.clientList.Register(userIdentifier, clientServicer, responseStream);

            await clientServicer.Start();
        }
    }
}
