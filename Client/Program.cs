using Grpc.Core;
using Grpc.Net.Client;
using Protos;
using System.Diagnostics;
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

            string address = args[0];

            // keep alive the connection, don't immediately close
            GrpcChannelOptions channelOptions = new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                }
            };

            // establish bidirectional channel and wrap it in the grpc client
            using GrpcChannel channel = GrpcChannel.ForAddress(address, channelOptions);
            try
            {
                ChatAppQuicClient client = new ChatAppQuicClient(channel);

                using AsyncDuplexStreamingCall<Request, Response> session = client.SessionExecute();

                IResponseHandler responseHandler = new ResponseHandler(session.ResponseStream, Console.WriteLine);

                // start in the background
                Task background = responseHandler.Start();

                string? userName = null;
                while (userName is null)
                {
                    Console.Write("Enter username: ");

                    string? input = Console.ReadLine();

                    // they entered no username, let it be
                    if (string.IsNullOrEmpty(input))
                    {
                        break;
                    }

                    if (input.Contains(' '))
                    {
                        Debug.WriteLine("Spaces aren't allowed in usernames", category: "Error");
                        continue;
                    }

                    userName = input;
                }

                if (userName is not null)
                {
                    await session.RequestStream.WriteAsync(new Request
                    {
                        ChangeName = new ChangeNameRequest
                        {
                            NewName = userName,
                        },
                    });

                    Console.WriteLine($"Successfully logged in as '{userName}'");
                }

                IRequestHandler requestHandler = new RequestHandler(session.RequestStream);

                // read console in the foreground, having the request handler handle the output
                while (true)
                {
                    string? line = Console.ReadLine();

                    if (line is null || channel.State != ConnectivityState.Ready)
                    {
                        break;
                    }

                    try
                    {
                        await requestHandler.HandleInput(line);
                    }
                    catch (Exception e)
                    {
                        // log exception and eat it
                        Debug.WriteLine(e.ToString(), category: "Error");
                    }
                }

                // await background now that we're done
                await background;

                return 0;
            }
            finally
            {
                try
                {
                    await channel.ShutdownAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error shutting down the connection: {e}", category: "Error");
                }
            }
        }
    }
}