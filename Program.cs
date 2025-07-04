using Networking;

class Program
{
    public static async Task Main(string[] args)
    {
        int portArg = int.Parse(args.Length > 1 ? args[1] : "3565");
        int port = portArg;

        UDPServer.Init(port);

        while (true)
        {
            Thread.Sleep(1000);
        }
    }
}