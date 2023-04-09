namespace Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddGrpc();
            builder.Services.AddSingleton<IClientList>(new ClientList());

            WebApplication app = builder.Build();

            app.MapGrpcService<ChatAppQuicService>();

            app.MapGet("/", () => "Are you trying to reach ChatAppQuic? You must use our gRPC client.");

            app.Run();
        }
    }
}