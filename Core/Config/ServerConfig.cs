using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Config
{
    /// <summary>
    /// Server configuration loaded from server-config.json
    /// Controls all aspects of server behavior and networking
    /// </summary>
    public class ServerConfig
    {
        #region Network Configuration

        [JsonPropertyName("network")]
        public NetworkConfig Network { get; set; } = new NetworkConfig();

        #endregion

        #region Security Configuration

        [JsonPropertyName("security")]
        public SecurityConfig Security { get; set; } = new SecurityConfig();

        #endregion

        #region Performance Configuration

        [JsonPropertyName("performance")]
        public PerformanceConfig Performance { get; set; } = new PerformanceConfig();

        #endregion

        #region Server Configuration

        [JsonPropertyName("server")]
        public ServerGeneralConfig Server { get; set; } = new ServerGeneralConfig();

        #endregion

        #region Static Instance Management

        private static ServerConfig? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Get the current server configuration instance
        /// </summary>
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

        /// <summary>
        /// Check if configuration is loaded
        /// </summary>
        public static bool IsLoaded => _instance != null;

        #endregion

        #region Configuration Loading

        /// <summary>
        /// Load configuration from JSON file
        /// </summary>
        /// <param name="configPath">Path to the configuration file</param>
        /// <returns>Loaded server configuration</returns>
        public static ServerConfig LoadConfig(string configPath = "server-config.json")
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(configPath))
                    {
                        Console.WriteLine($"[CONFIG] Configuration file not found at '{configPath}'. Creating default configuration...");
                        var defaultConfig = CreateDefaultConfig();
                        SaveConfig(defaultConfig, configPath);
                        _instance = defaultConfig;
                        return defaultConfig;
                    }

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
                    {
                        throw new InvalidOperationException("Failed to deserialize configuration file");
                    }

                    // Validate configuration
                    ValidateConfig(config);

                    _instance = config;
                    Console.WriteLine($"[CONFIG] ✅ Configuration loaded successfully from '{configPath}'");
                    LogConfigurationSummary(config);

                    return config;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONFIG] ❌ Error loading configuration: {ex.Message}");
                    Console.WriteLine($"[CONFIG] 🔄 Using default configuration...");

                    var defaultConfig = CreateDefaultConfig();
                    _instance = defaultConfig;
                    return defaultConfig;
                }
            }
        }

        /// <summary>
        /// Save configuration to JSON file
        /// </summary>
        public static void SaveConfig(ServerConfig config, string configPath = "server-config.json")
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonContent = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, jsonContent);
                Console.WriteLine($"[CONFIG] ✅ Configuration saved to '{configPath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] ❌ Error saving configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static ServerConfig CreateDefaultConfig()
        {
            return new ServerConfig
            {
                Network = new NetworkConfig(),
                Security = new SecurityConfig(),
                Performance = new PerformanceConfig(),
                Server = new ServerGeneralConfig()
            };
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private static void ValidateConfig(ServerConfig config)
        {
            // Network validation
            if (config.Network.Port <= 0 || config.Network.Port > 65535)
                throw new ArgumentException($"Invalid port number: {config.Network.Port}");

            if (config.Network.MaxPacketSize < 512 || config.Network.MaxPacketSize > 8192)
                throw new ArgumentException($"Invalid max packet size: {config.Network.MaxPacketSize}");

            // Performance validation
            if (config.Performance.TickRate <= 0 || config.Performance.TickRate > 120)
                throw new ArgumentException($"Invalid tick rate: {config.Performance.TickRate}");

            if (config.Performance.BundleTimeoutMs < 10 || config.Performance.BundleTimeoutMs > 1000)
                throw new ArgumentException($"Invalid bundle timeout: {config.Performance.BundleTimeoutMs}");

            // Server validation
            if (config.Server.MaxPlayers <= 0 || config.Server.MaxPlayers > 100000)
                throw new ArgumentException($"Invalid max players: {config.Server.MaxPlayers}");
        }

        /// <summary>
        /// Log configuration summary
        /// </summary>
        private static void LogConfigurationSummary(ServerConfig config)
        {
            Console.WriteLine("[CONFIG] === SERVER CONFIGURATION SUMMARY ===");
            Console.WriteLine($"[CONFIG] 🌐 Network: Port {config.Network.Port}, MTU {config.Network.MaxPacketSize} bytes");
            Console.WriteLine($"[CONFIG] 🔐 Security: Encryption={config.Security.EnableEndToEndEncryption}, Password={!string.IsNullOrEmpty(config.Server.Password)}");
            Console.WriteLine($"[CONFIG] 📦 Compression: LZ4={config.Network.EnableLZ4Compression}");
            Console.WriteLine($"[CONFIG] 💓 Heartbeat: Enabled={config.Security.EnableHeartbeat}, Interval={config.Security.HeartbeatIntervalMs}ms");
            Console.WriteLine($"[CONFIG] ⚡ Performance: TickRate={config.Performance.TickRate}Hz, BundleTimeout={config.Performance.BundleTimeoutMs}ms");
            Console.WriteLine($"[CONFIG] 👥 Server: MaxPlayers={config.Server.MaxPlayers}, Name='{config.Server.Name}'");
            Console.WriteLine("[CONFIG] ============================================");
        }

        #endregion
    }

    #region Configuration Sections

    /// <summary>
    /// Network-related configuration
    /// </summary>
    public class NetworkConfig
    {
        [JsonPropertyName("port")]
        public int Port { get; set; } = 3565;

        [JsonPropertyName("maxPacketSize")]
        public int MaxPacketSize { get; set; } = 1200;

        [JsonPropertyName("enableLZ4Compression")]
        public bool EnableLZ4Compression { get; set; } = true;

        [JsonPropertyName("compressionThreshold")]
        public int CompressionThreshold { get; set; } = 512;

        [JsonPropertyName("receiveBufferSize")]
        public int ReceiveBufferSize { get; set; } = 524288; // 512KB

        [JsonPropertyName("sendBufferSize")]
        public int SendBufferSize { get; set; } = 524288; // 512KB

        [JsonPropertyName("sendThreadCount")]
        public int SendThreadCount { get; set; } = 1;
    }

    /// <summary>
    /// Security-related configuration
    /// </summary>
    public class SecurityConfig
    {
        [JsonPropertyName("enableEndToEndEncryption")]
        public bool EnableEndToEndEncryption { get; set; } = true;

        [JsonPropertyName("enableIntegrityCheck")]
        public bool EnableIntegrityCheck { get; set; } = true;

        [JsonPropertyName("enableWAF")]
        public bool EnableWAF { get; set; } = true;

        [JsonPropertyName("enableHeartbeat")]
        public bool EnableHeartbeat { get; set; } = true;

        [JsonPropertyName("heartbeatIntervalMs")]
        public int HeartbeatIntervalMs { get; set; } = 5000;

        [JsonPropertyName("heartbeatTimeoutMs")]
        public int HeartbeatTimeoutMs { get; set; } = 15000;

        [JsonPropertyName("enableAntiCheat")]
        public bool EnableAntiCheat { get; set; } = false;

        [JsonPropertyName("maxConnectionsPerIP")]
        public int MaxConnectionsPerIP { get; set; } = 3;
    }

    /// <summary>
    /// Performance-related configuration
    /// </summary>
    public class PerformanceConfig
    {
        [JsonPropertyName("tickRate")]
        public int TickRate { get; set; } = 32;

        [JsonPropertyName("bundleTimeoutMs")]
        public int BundleTimeoutMs { get; set; } = 50;

        [JsonPropertyName("reliableTimeoutMs")]
        public int ReliableTimeoutMs { get; set; } = 250;

        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 10;

        [JsonPropertyName("enablePerformanceMetrics")]
        public bool EnablePerformanceMetrics { get; set; } = true;

        [JsonPropertyName("metricsLogIntervalMs")]
        public int MetricsLogIntervalMs { get; set; } = 30000;
    }

    /// <summary>
    /// General server configuration
    /// </summary>
    public class ServerGeneralConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "ToS Game Server";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("maxPlayers")]
        public int MaxPlayers { get; set; } = 1000;

        [JsonPropertyName("password")]
        public string Password { get; set; } = "";

        [JsonPropertyName("enablePasswordProtection")]
        public bool EnablePasswordProtection { get; set; } = false;

        [JsonPropertyName("motd")]
        public string MOTD { get; set; } = "Welcome to ToS Game Server!";

        [JsonPropertyName("enableDebugLogs")]
        public bool EnableDebugLogs { get; set; } = false;

        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; } = "Info"; // Debug, Info, Warning, Error

        [JsonPropertyName("enableFileLogging")]
        public bool EnableFileLogging { get; set; } = true;
    }

    #endregion
}
