using NanoSockets;

namespace Tests
{
    public class WAFRateLimiterTests : AbstractTest
    {
        public WAFRateLimiterTests()
        {
            Describe("WAFRateLimiter Operations", () =>
            {
                It("should allow initial packets within rate limit", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.100", 8080);

                    // Should allow first few packets (burst + rate)
                    for (int i = 0; i < 5; i++)
                    {
                        bool allowed = WAFRateLimiter.AllowPacket(address);
                        Expect(allowed).ToBe(true);
                    }
                });

                It("should block packets when rate limit exceeded", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.101", 8080);

                    // Consume all available tokens (burst + initial rate)
                    int allowedCount = 0;
                    for (int i = 0; i < 100; i++)
                    {
                        if (WAFRateLimiter.AllowPacket(address))
                            allowedCount++;
                    }

                    // Should have allowed some packets but not all
                    Expect(allowedCount).ToBe(65); // burst(5) + rate(60) = 65

                    // Next packet should be blocked
                    bool blocked = WAFRateLimiter.AllowPacket(address);
                    Expect(blocked).ToBe(false);
                });

                It("should handle different IP addresses independently", () =>
                {
                    var address1 = Address.CreateFromIpPort("192.168.1.102", 8080);
                    var address2 = Address.CreateFromIpPort("192.168.1.103", 8080);

                    // Exhaust tokens for first address
                    for (int i = 0; i < 70; i++)
                    {
                        WAFRateLimiter.AllowPacket(address1);
                    }

                    // First address should be blocked
                    bool blocked1 = WAFRateLimiter.AllowPacket(address1);
                    Expect(blocked1).ToBe(false);

                    // Second address should still be allowed
                    bool allowed2 = WAFRateLimiter.AllowPacket(address2);
                    Expect(allowed2).ToBe(true);
                });

                It("should handle same IP with different ports as same client", () =>
                {
                    var address1 = Address.CreateFromIpPort("192.168.1.104", 8080);
                    var address2 = Address.CreateFromIpPort("192.168.1.104", 9090);

                    // Exhaust tokens for first address
                    for (int i = 0; i < 70; i++)
                    {
                        WAFRateLimiter.AllowPacket(address1);
                    }

                    // Same IP different port should also be blocked
                    bool blocked = WAFRateLimiter.AllowPacket(address2);
                    Expect(blocked).ToBe(false);
                });

                It("should refill tokens over time", async () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.105", 8080);

                    // Consume all tokens
                    for (int i = 0; i < 70; i++)
                    {
                        WAFRateLimiter.AllowPacket(address);
                    }

                    // Should be blocked now
                    bool blocked = WAFRateLimiter.AllowPacket(address);
                    Expect(blocked).ToBe(false);

                    // Wait for token refill (at 60 packets per second = ~16.67ms per token)
                    await Task.Delay(100); // Wait 100ms for multiple tokens to refill

                    // Should allow some packets again
                    bool allowed = WAFRateLimiter.AllowPacket(address);
                    Expect(allowed).ToBe(true);
                });

                It("should handle burst capacity correctly", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.106", 8080);

                    // Should allow burst size (5) + rate (60) = 65 packets initially
                    int allowedCount = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        if (WAFRateLimiter.AllowPacket(address))
                            allowedCount++;
                    }

                    // Should allow all 10 packets (within burst + rate limit)
                    Expect(allowedCount).ToBe(10);
                });

                It("should handle localhost addresses", () =>
                {
                    var localhost1 = Address.CreateFromIpPort("127.0.0.1", 8080);
                    var localhost2 = Address.CreateFromIpPort("localhost", 8080);

                    bool allowed1 = WAFRateLimiter.AllowPacket(localhost1);
                    bool allowed2 = WAFRateLimiter.AllowPacket(localhost2);

                    Expect(allowed1).ToBe(true);
                    Expect(allowed2).ToBe(true);
                });

                It("should handle concurrent access safely", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.107", 8080);
                    int allowedCount = 0;
                    int totalRequests = 200;

                    var tasks = new Task[10];
                    for (int i = 0; i < 10; i++)
                    {
                        tasks[i] = Task.Run(() =>
                        {
                            for (int j = 0; j < totalRequests / 10; j++)
                            {
                                if (WAFRateLimiter.AllowPacket(address))
                                {
                                    Interlocked.Increment(ref allowedCount);
                                }
                            }
                        });
                    }

                    Task.WaitAll(tasks);

                    // Should have allowed approximately the rate limit
                    // Allow some variance due to timing and concurrent access
                    Expect(allowedCount).ToBeLessThan(75); // Some buffer for concurrency
                    Expect(allowedCount).ToBeGreaterThan(60); // At least the base rate
                });

                It("should handle multiple different addresses concurrently", () =>
                {
                    int totalAllowed = 0;
                    int addressCount = 5;
                    int requestsPerAddress = 20;

                    var tasks = new Task[addressCount];
                    for (int i = 0; i < addressCount; i++)
                    {
                        int addressIndex = i;
                        tasks[i] = Task.Run(() =>
                        {
                            var address = Address.CreateFromIpPort($"192.168.1.{200 + addressIndex}", 8080);
                            int localAllowed = 0;

                            for (int j = 0; j < requestsPerAddress; j++)
                            {
                                if (WAFRateLimiter.AllowPacket(address))
                                {
                                    localAllowed++;
                                }
                            }

                            Interlocked.Add(ref totalAllowed, localAllowed);
                        });
                    }

                    Task.WaitAll(tasks);

                    // Each address should have its own bucket, so more packets should be allowed
                    Expect(totalAllowed).ToBeGreaterThan(50); // Much higher than single address limit
                });

                It("should handle rapid successive calls", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.108", 8080);

                    int allowedCount = 0;
                    for (int i = 0; i < 1000; i++)
                    {
                        if (WAFRateLimiter.AllowPacket(address))
                            allowedCount++;
                    }

                    // Should respect rate limits even under rapid calls
                    Expect(allowedCount).ToBeLessThan(70);
                    Expect(allowedCount).ToBeGreaterThan(0);
                });

                It("should handle zero port addresses", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.109", 0);

                    bool allowed = WAFRateLimiter.AllowPacket(address);
                    Expect(allowed).ToBe(true);
                });

                It("should maintain state between calls", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.110", 8080);

                    // First call should be allowed
                    bool first = WAFRateLimiter.AllowPacket(address);
                    Expect(first).ToBe(true);

                    // Subsequent calls should maintain the bucket state
                    bool second = WAFRateLimiter.AllowPacket(address);
                    Expect(second).ToBe(true);
                });

                It("should handle edge case with maximum rate consumption", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.111", 8080);

                    // Consume exactly the limit
                    for (int i = 0; i < 65; i++) // burst(5) + rate(60)
                    {
                        bool allowed = WAFRateLimiter.AllowPacket(address);
                        Expect(allowed).ToBe(true);
                    }

                    // Next packet should be blocked
                    bool blocked = WAFRateLimiter.AllowPacket(address);
                    Expect(blocked).ToBe(false);
                });
            });
        }
    }
}
