using Grpc.Core;
using Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class RequestHandler : IRequestHandler
    {
        private readonly IAsyncStreamWriter<Request> requestWriter;

        /// <summary>
        /// Initializes an instance of RequestHandler, doesn't start any background threads
        /// </summary>
        /// <param name="requestWriter"></param>
        public RequestHandler(
            IAsyncStreamWriter<Request> requestWriter)
        {
            this.requestWriter = requestWriter;
        }

        /// <inheritdoc/>
        public async Task HandleInput(string inputLine)
        {
            // just do global message for now
            Request request = new Request
            {
                GlobalMessage = new GlobalMessageRequest
                {
                    Message = inputLine,
                },
            };

            await this.requestWriter.WriteAsync(request);
        }
    }
}
