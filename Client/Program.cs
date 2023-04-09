using Grpc.Core;
using Grpc.Net.Client;
using Protos;
using static Protos.ChatAppQuic;

namespace Client
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: ChatClient.exe <url>");
                return 1;
            }

            Uri address = new Uri(args[0]);

            // establish bidirectional channel and wrap it in the grpc client
            using GrpcChannel channel = GrpcChannel.ForAddress(address);
            ChatAppQuicClient client = new ChatAppQuicClient(channel);

            using AsyncDuplexStreamingCall<Request, Response> session = client.SessionExecute();

            IResponseHandler responseHandler = new ResponseHandler(session.ResponseStream, Console.WriteLine);

            // start in the background
            _ = responseHandler.Start();

            IRequestHandler requestHandler = new RequestHandler(session.RequestStream);

            // read console in the foreground, having the request handler handle the output
            while (true)
            {
                string? line = Console.ReadLine();

                if (line is null)
                {
                    return 0;
                }

                await requestHandler.HandleInput(line);
            }
        }
    }
}