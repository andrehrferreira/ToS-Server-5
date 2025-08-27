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

        #region World Origin Rebasing Configuration

        [JsonPropertyName("worldOriginRebasing")]
        public WorldOriginRebasingConfig WorldOriginRebasing { get; set; } = new WorldOriginRebasingConfig();

        [JsonPropertyName("areaOfInterest")]
        public AreaOfInterestConfig AreaOfInterest { get; set; } = new AreaOfInterestConfig();

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

        #region Configuration Loading and Updates

        /// <summary>
        /// Atualiza as configurações de todos os mundos após carregar a configuração
        /// </summary>
        private static void UpdateWorldSettings()
        {
            try
            {
                // Verificar se há mundos para atualizar
                if (World.Worlds != null && World.Worlds.Count > 0)
                {
                    Console.WriteLine($"[CONFIG] 🔄 Atualizando configurações de {World.Worlds.Count} mundo(s)");

                    // Atualizar configurações de todos os mundos existentes
                    foreach (var world in World.Worlds.Values)
                    {
                        // Usar reflexão para chamar o método privado UpdateTickRate
                        var methodInfo = world.GetType().GetMethod("UpdateTickRate",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);

                        if (methodInfo != null)
                        {
                            methodInfo.Invoke(world, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] ⚠️ Não foi possível atualizar configurações dos mundos: {ex.Message}");
            }
        }

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

                        // Atualizar configurações do mundo após criar configuração padrão
                        UpdateWorldSettings();

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

                    // Atualizar configurações do mundo após carregar configuração
                    UpdateWorldSettings();

                    return config;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONFIG] ❌ Error loading configuration: {ex.Message}");
                    Console.WriteLine($"[CONFIG] 🔄 Using default configuration...");

                    var defaultConfig = CreateDefaultConfig();
                    _instance = defaultConfig;

                    // Atualizar configurações do mundo após carregar configuração padrão
                    UpdateWorldSettings();

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
                Server = new ServerGeneralConfig(),
                WorldOriginRebasing = new WorldOriginRebasingConfig()
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

        [JsonPropertyName("defaultMapName")]
        public string DefaultMapName { get; set; } = "SurvivalMap";
    }

    /// <summary>
    /// World Origin Rebasing configuration
    /// </summary>
    public class WorldOriginRebasingConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("enableYawOnlyRotation")]
        public bool EnableYawOnlyRotation { get; set; } = true;

        [JsonPropertyName("maps")]
        public Dictionary<string, MapSectionConfig> Maps { get; set; } = new Dictionary<string, MapSectionConfig>
        {
            {
                "SurvivalMap",
                new MapSectionConfig
                {
                    SectionSize = new FVector { X = 25600.0f, Y = 25600.0f, Z = 6400.0f },
                    SectionPerComponent = new FVector { X = 4, Y = 4, Z = 1 },
                    NumberOfComponents = new FVector { X = 8, Y = 8, Z = 4 },
                    Scale = new FVector { X = 100.0f, Y = 100.0f, Z = 100.0f }
                }
            }
        };
    }

    /// <summary>
    /// Map section configuration for World Origin Rebasing
    /// </summary>
    public class MapSectionConfig
    {
        [JsonPropertyName("sectionSize")]
        public FVector SectionSize { get; set; } = new FVector { X = 25600.0f, Y = 25600.0f, Z = 6400.0f };

        [JsonPropertyName("sectionPerComponent")]
        public FVector SectionPerComponent { get; set; } = new FVector { X = 4, Y = 4, Z = 1 };

        [JsonPropertyName("numberOfComponents")]
        public FVector NumberOfComponents { get; set; } = new FVector { X = 8, Y = 8, Z = 4 };

        [JsonPropertyName("scale")]
        public FVector Scale { get; set; } = new FVector { X = 100.0f, Y = 100.0f, Z = 100.0f };

        /// <summary>
        /// Calculate quadrant from world position
        /// </summary>
        public (int X, int Y) GetQuadrantFromPosition(FVector worldPosition)
        {
            float totalWorldSizeX = SectionSize.X * SectionPerComponent.X * NumberOfComponents.X;
            float totalWorldSizeY = SectionSize.Y * SectionPerComponent.Y * NumberOfComponents.Y;

            int quadrantX = (int)Math.Floor(worldPosition.X / totalWorldSizeX * NumberOfComponents.X);
            int quadrantY = (int)Math.Floor(worldPosition.Y / totalWorldSizeY * NumberOfComponents.Y);

            // Clamp to valid range
            quadrantX = Math.Max(0, Math.Min((int)NumberOfComponents.X - 1, quadrantX));
            quadrantY = Math.Max(0, Math.Min((int)NumberOfComponents.Y - 1, quadrantY));

            return (quadrantX, quadrantY);
        }

        /// <summary>
        /// Get quadrant origin position
        /// </summary>
        public FVector GetQuadrantOrigin(int quadrantX, int quadrantY)
        {
            // Simplificado para evitar problemas de NaN - quadrante * tamanho da seção
            float originX = quadrantX * SectionSize.X;
            float originY = quadrantY * SectionSize.Y;

            return new FVector { X = originX, Y = originY, Z = 0.0f };
        }

        /// <summary>
        /// Convert world position to quantized position relative to quadrant
        /// </summary>
        public (short X, short Y, short Z) QuantizePosition(FVector worldPosition, int quadrantX, int quadrantY)
        {
            FVector quadrantOrigin = GetQuadrantOrigin(quadrantX, quadrantY);
            FVector relativePosition = new FVector
            {
                X = worldPosition.X - quadrantOrigin.X,
                Y = worldPosition.Y - quadrantOrigin.Y,
                Z = worldPosition.Z
            };

            // Quantize to int16 range
            short quantX = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, relativePosition.X / Scale.X));
            short quantY = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, relativePosition.Y / Scale.Y));
            short quantZ = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, relativePosition.Z / Scale.Z));

            return (quantX, quantY, quantZ);
        }

        /// <summary>
        /// Convert quantized position back to world position
        /// </summary>
        public FVector DequantizePosition(short quantX, short quantY, short quantZ, int quadrantX, int quadrantY)
        {
            // Obter a origem do quadrante (simplificada)
            FVector quadrantOrigin = GetQuadrantOrigin(quadrantX, quadrantY);

            // Garantir que Scale nunca seja zero
            float scaleX = Scale.X > 0 ? Scale.X : 100.0f;
            float scaleY = Scale.Y > 0 ? Scale.Y : 100.0f;
            float scaleZ = Scale.Z > 0 ? Scale.Z : 100.0f;

            // Calcular posição mundial
            return new FVector
            {
                X = quadrantOrigin.X + (quantX * scaleX),
                Y = quadrantOrigin.Y + (quantY * scaleY),
                Z = quantZ * scaleZ
            };
        }
    }

    /// <summary>
    /// Area of Interest configuration for optimized entity replication
    /// </summary>
    public class AreaOfInterestConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("baseDistance")]
        public float BaseDistance { get; set; } = 100000.0f; // 1km in cm (Unreal units)

        [JsonPropertyName("entityDistances")]
        public Dictionary<string, float> EntityDistances { get; set; } = new Dictionary<string, float>
        {
            ["Player"] = 100000.0f,     // 1km
            ["NPC"] = 50000.0f,         // 500m
            ["StaticBuild"] = 25000.0f, // 250m
            ["Vehicle"] = 150000.0f,    // 1.5km
            ["Projectile"] = 200000.0f, // 2km
            ["Item"] = 10000.0f,        // 100m
            ["Effect"] = 50000.0f       // 500m
        };

        /// <summary>
        /// Configurações de sincronização adaptativa
        /// </summary>
        [JsonPropertyName("adaptiveSync")]
        public AdaptiveSyncConfig AdaptiveSync { get; set; } = new AdaptiveSyncConfig();

        public float GetDistanceForEntityType(string entityType)
        {
            return EntityDistances.TryGetValue(entityType, out var distance) ? distance : BaseDistance;
        }

        public bool IsWithinRange(FVector pos1, FVector pos2, string entityType1, string entityType2)
        {
            float distance1 = GetDistanceForEntityType(entityType1);
            float distance2 = GetDistanceForEntityType(entityType2);
            float maxDistance = Math.Max(distance1, distance2);
            float actualDistance = FVector.Distance(pos1, pos2);
            return actualDistance <= maxDistance;
        }
    }

    /// <summary>
    /// Configurações para sincronização adaptativa baseada em movimento e distância
    /// </summary>
    public class AdaptiveSyncConfig
    {
        /// <summary>
        /// Taxa de atualização do servidor em Hz (frames por segundo)
        /// </summary>
        [JsonPropertyName("serverTickRate")]
        public int ServerTickRate { get; set; } = 60;

        /// <summary>
        /// Intervalo em segundos para sincronização periódica de entidades paradas
        /// </summary>
        [JsonPropertyName("periodicSyncInterval")]
        public int PeriodicSyncInterval { get; set; } = 5;

        /// <summary>
        /// Taxa de sincronização para entidades paradas (percentual da taxa normal)
        /// </summary>
        [JsonPropertyName("stationarySyncRate")]
        public float StationarySyncRate { get; set; } = 0.1f;

        /// <summary>
        /// Taxa de sincronização para entidades distantes (percentual da taxa normal)
        /// </summary>
        [JsonPropertyName("distantSyncRate")]
        public float DistantSyncRate { get; set; } = 0.3f;

        /// <summary>
        /// Distância a partir da qual a entidade é considerada distante (em unidades)
        /// </summary>
        [JsonPropertyName("distanceThreshold")]
        public float DistanceThreshold { get; set; } = 50000.0f; // 500m

        /// <summary>
        /// Velocidade mínima para considerar que uma entidade está em movimento
        /// </summary>
        [JsonPropertyName("movementThreshold")]
        public float MovementThreshold { get; set; } = 0.1f;
    }

    #endregion
}
