using Google.Protobuf;
using Grpc.Core;
using Protos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Service
{
    internal class ClientServicer : IClientServicer
    {
        private readonly Guid userIdentifier;
        private readonly IAsyncStreamReader<Request> messageStream;
        private readonly IClientList clientList;

        /// <summary>
        /// Main constructor for client servicer, doesn't start listening yet
        /// </summary>
        /// <param name="userIdentifier">The unique identifier for this client</param>
        /// <param name="messageStream">The stream to read messages from the client on</param>
        /// <param name="clientList">The client list to send messages to other clients with</param>
        public ClientServicer(
            Guid userIdentifier,
            IAsyncStreamReader<Request> messageStream,
            IClientList clientList)
        {
            this.userIdentifier = userIdentifier;
            this.messageStream = messageStream;
            this.clientList = clientList;
        }

        /// <inheritdoc/>
        public void Start()
        {
            // kick it off in the background, don't await
            _ = this.HandleClientSocket();
        }

        /// <summary>
        /// Handles receiving messages from the client on the tcp socker, runs forever until cancelled
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task HandleClientSocket()
        {
            await foreach (Protos.Request message in this.messageStream.ReadAllAsync())
            {
                switch (message.RequestCase)
                {
                    case Request.RequestOneofCase.DirectMessage:
                        await this.HandleDirectMessage(message.DirectMessage);
                        break;

                    case Request.RequestOneofCase.GlobalMessage:
                        await this.HandleGlobalMessage(message.GlobalMessage);
                        break;

                    default:
                        Debug.WriteLine($"Received bad request of type {message.RequestCase}", category: "Error");
                        break;
                }
            }

            // only gets here when connection to client has been closed
            await this.clientList.Unregister(this.userIdentifier);
        }

        private async Task HandleGlobalMessage(Protos.GlobalMessageRequest request)
        {
            Response responseMessage = new Response
            {
                Message = new MessageResponse
                {
                    Message = request.Message,
                },
            };

            IReadOnlyCollection<IAsyncStreamWriter<Response>> writers = await this.clientList.GetGlobalMessageList(this.userIdentifier);

            await SendAllMessages(writers, responseMessage);
        }

        private async Task HandleDirectMessage(Protos.DirectMessageRequest request)
        {
            Response responseMessage = new Response
            {
                Message = new MessageResponse
                {
                    Message = request.Message,
                },
            };

            IReadOnlyCollection<IAsyncStreamWriter<Response>> writers = await this.clientList.GetDirectMessageList(request.TargetUser, this.userIdentifier);

            await SendAllMessages(writers, responseMessage);
        }

        private static async Task SendAllMessages(
            IReadOnlyCollection<IAsyncStreamWriter<Response>> writers,
            Response message)
        {
            foreach (IAsyncStreamWriter<Response> writer in writers)
            {
                await writer.WriteAsync(message);
            }
        }
    }
}
