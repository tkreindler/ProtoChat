using Grpc.Core;
using Protos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class ResponseHandler : IResponseHandler
    {
        private readonly IAsyncStreamReader<Response> responseStream;
        private readonly Action<string> printAction;

        /// <summary>
        /// Initializes an instance of <see cref="ResponseHandler"/>, doesn't start any threads, use Start for that
        /// </summary>
        /// <param name="responseStream">The stream we receive messages to</param>
        /// <param name="printAction">Where to write text, ie the console</param>
        public ResponseHandler(
            IAsyncStreamReader<Response> responseStream,
            Action<string> printAction)
        {
            this.responseStream = responseStream;
            this.printAction = printAction;
        }

        private void HandleMessage(Protos.MessageResponse response)
        {
            this.printAction(response.Message);
        }

        private async Task ReadAllResponses()
        {
            await foreach (Response response in this.responseStream.ReadAllAsync())
            {
                switch (response.ResponseCase)
                {
                    case Response.ResponseOneofCase.Message:
                        this.HandleMessage(response.Message);
                        break;

                    default:
                        Debug.WriteLine($"Received bad request of type {response.ResponseCase}", category: "Error");
                        break;
                }
            }
        }

        public async Task Start()
        {
            await this.ReadAllResponses();
        }
    }
}
