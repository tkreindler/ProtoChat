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

        const string dmPrefix = "dm ";

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
            Request request = new Request
            {
            };
            if (inputLine.StartsWith(dmPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                int space = inputLine.IndexOf(' ', dmPrefix.Length);

                if (space == -1)
                {
                    throw new InvalidOperationException("Badly formatted dm message");
                }

                string name = inputLine.Substring(dmPrefix.Length, space - dmPrefix.Length + 1);
                string message = inputLine.Substring(space);


                request.DirectMessage = new DirectMessageRequest
                {
                    TargetUser = name,
                    Message = message,
                };
            }
            else
            {
                // just do global message for now
                request.GlobalMessage = new GlobalMessageRequest
                {
                    Message = inputLine,
                };
            }

            await this.requestWriter.WriteAsync(request);
        }
    }
}
