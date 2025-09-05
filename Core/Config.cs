using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wormhole
{
    public class ServerConfig
    {
        #region Network Configuration

        [JsonPropertyName("network")]
        public NetworkConfig Network { get; set; } = new NetworkConfig();

        #endregion

        private static ServerConfig? _instance;
        private static readonly object _lock = new object();

        public static ServerConfig Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ?? throw new InvalidOperationException("ServerConfig not loaded. Call LoadConfig() first.");
                }
            }
        }

        public static ServerConfig LoadConfig(string configPath = "server-config.json")
        {
            lock (_lock)
            {
                try
                {
                    string jsonContent = File.ReadAllText(configPath);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        WriteIndented = true
                    };

                    var config = JsonSerializer.Deserialize<ServerConfig>(jsonContent, options);

                    if (config == null)
                        throw new InvalidOperationException("Failed to deserialize configuration file");

                    ValidateConfig(config);

                    _instance = config;
                    Debug.Log($"[CONFIG] Configuration loaded successfully from '{configPath}'");

                    return config;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONFIG] Error loading configuration: {ex.Message}");
                    Console.WriteLine($"[CONFIG] Using default configuration...");

                    var defaultConfig = CreateDefaultConfig();
                    _instance = defaultConfig;

                    return defaultConfig;
                }
            }
        }

        public static ServerConfig CreateDefaultConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkConfig()
            };
        }

        private static void ValidateConfig(ServerConfig config)
        {
            if (config.Network.Port <= 0 || config.Network.Port > 65535)
                throw new ArgumentException($"Invalid port number: {config.Network.Port}");
        }
    }

    public class NetworkConfig
    {
        [JsonPropertyName("port")]
        public int Port { get; set; } = 3565;

        [JsonPropertyName("maxClients")]
        public int MaxClients { get; set; } = 100;

        [JsonPropertyName("tickRate")]
        public int TickRate { get; set; } = 30;

        [JsonPropertyName("pingInterval")]
        public int PingInterval { get; set; } = 5000;
    }
}