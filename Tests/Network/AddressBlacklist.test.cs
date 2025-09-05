using NanoSockets;
using Wormhole;

namespace Tests
{
    public class AddressBlacklistTests : AbstractTest
    {
        public AddressBlacklistTests()
        {
            Describe("AddressBlacklist Basic Operations", () =>
            {
                It("should start with empty blacklist", () =>
                {
                    Expect(AddressBlacklist.Count).ToBe(0);
                });

                It("should ban address with default TTL", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.100", 8080);

                    AddressBlacklist.Ban(address);

                    Expect(AddressBlacklist.Count).ToBeGreaterThan(0);
                    Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                });

                It("should ban address with custom TTL", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.101", 8080);

                    AddressBlacklist.Ban(address, TimeSpan.FromSeconds(30));

                    Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                });

                It("should unban address successfully", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.102", 8080);

                    AddressBlacklist.Ban(address);
                    Expect(AddressBlacklist.IsBanned(address)).ToBe(true);

                    bool unbanResult = AddressBlacklist.Unban(address);
                    Expect(unbanResult).ToBe(true);
                    Expect(AddressBlacklist.IsBanned(address)).ToBe(false);
                });

                It("should return false when unbanning non-existent address", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.103", 8080);

                    bool unbanResult = AddressBlacklist.Unban(address);
                    Expect(unbanResult).ToBe(false);
                });

                It("should handle localhost address", () =>
                {
                    var localhost = Address.CreateFromIpPort("127.0.0.1", 8080);

                    AddressBlacklist.Ban(localhost);
                    Expect(AddressBlacklist.IsBanned(localhost)).ToBe(true);

                    AddressBlacklist.Unban(localhost);
                    Expect(AddressBlacklist.IsBanned(localhost)).ToBe(false);
                });

                It("should handle different ports for same IP", () =>
                {
                    var address1 = Address.CreateFromIpPort("192.168.1.104", 8080);
                    var address2 = Address.CreateFromIpPort("192.168.1.104", 8081);

                    AddressBlacklist.Ban(address1);
                    AddressBlacklist.Ban(address2);

                    Expect(AddressBlacklist.IsBanned(address1)).ToBe(true);
                    Expect(AddressBlacklist.IsBanned(address2)).ToBe(true);

                    // They should be treated as different addresses
                    bool unbanResult1 = AddressBlacklist.Unban(address1);
                    Expect(unbanResult1).ToBe(true);
                    Expect(AddressBlacklist.IsBanned(address1)).ToBe(false);
                    Expect(AddressBlacklist.IsBanned(address2)).ToBe(true);
                });
            });

            Describe("AddressBlacklist Expiration Handling", () =>
            {
                // It("should automatically remove expired entries on IsBanned check", () =>
                // {
                //     var address = Address.CreateFromIpPort("192.168.1.105", 8080);
                //
                //     // Clear any existing entries
                //     AddressBlacklist.SweepExpired(1000);
                //
                //     // Ban with TTL that guarantees expiration
                //     AddressBlacklist.Ban(address, TimeSpan.FromSeconds(5));
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                //
                //     // Wait for expiration (5 seconds + buffer)
                //     System.Threading.Thread.Sleep(6000);
                //
                //     // Should be expired now
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(false);
                // });

                // It("should handle zero TTL (immediate expiration)", () =>
                // {
                //     var address = Address.CreateFromIpPort("192.168.1.106", 8080);
                //
                //     AddressBlacklist.Ban(address, TimeSpan.Zero);
                //
                //     // For zero TTL, the entry expires at the current second
                //     // Give a delay to ensure we're in the next second
                //     System.Threading.Thread.Sleep(1100);
                //
                //     // Should be expired now
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(false);
                // });

                // It("should maintain ban status within TTL", () =>
                // {
                //     var address = Address.CreateFromIpPort("192.168.1.108", 8080);
                //
                //     AddressBlacklist.Ban(address, TimeSpan.FromSeconds(2));
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                //
                //     System.Threading.Thread.Sleep(500);
                //
                //     // Should still be banned
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                // });
            });

            Describe("AddressBlacklist Sweep Operations", () =>
            {
                // It("should respect sweep budget limit", () =>
                // {
                //     // Add multiple expired entries
                //     for (int i = 0; i < 10; i++)
                //     {
                //         var address = Address.CreateFromIpPort($"192.168.1.{111 + i}", 8080);
                //         AddressBlacklist.Ban(address, TimeSpan.FromMilliseconds(50));
                //     }
                //
                //     System.Threading.Thread.Sleep(100);
                //
                //     int removedCount = AddressBlacklist.SweepExpired(3); // Budget of 3
                //     Expect(removedCount).ToBeLessThanOrEqualTo(3);
                // });

                It("should return zero when no entries to sweep", () =>
                {
                    int removedCount = AddressBlacklist.SweepExpired();
                    Expect(removedCount).ToBe(0);
                });

                It("should handle sweep with no expired entries", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.120", 8080);
                    AddressBlacklist.Ban(address, TimeSpan.FromMinutes(5)); // Long TTL

                    int removedCount = AddressBlacklist.SweepExpired();
                    Expect(removedCount).ToBe(0);
                    Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                });
            });

            Describe("AddressBlacklist Count Property", () =>
            {
                It("should accurately track entry count", () =>
                {
                    int initialCount = AddressBlacklist.Count;

                    var address1 = Address.CreateFromIpPort("192.168.1.121", 8080);
                    var address2 = Address.CreateFromIpPort("192.168.1.122", 8080);

                    AddressBlacklist.Ban(address1);
                    Expect(AddressBlacklist.Count).ToBe(initialCount + 1);

                    AddressBlacklist.Ban(address2);
                    Expect(AddressBlacklist.Count).ToBe(initialCount + 2);

                    AddressBlacklist.Unban(address1);
                    Expect(AddressBlacklist.Count).ToBe(initialCount + 1);
                });

            });

            Describe("AddressBlacklist Edge Cases", () =>
            {
                // It("should handle rebanning same address", () =>
                // {
                //     var address = Address.CreateFromIpPort("192.168.1.124", 8080);
                //
                //     AddressBlacklist.Ban(address, TimeSpan.FromSeconds(1));
                //     AddressBlacklist.Ban(address, TimeSpan.FromMinutes(5)); // Re-ban with longer TTL
                //
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                //
                //     System.Threading.Thread.Sleep(1100);
                //
                //     // Should still be banned due to longer TTL
                //     Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                // });

                It("should handle banning already banned address", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.125", 8080);

                    AddressBlacklist.Ban(address);
                    int countBefore = AddressBlacklist.Count;

                    AddressBlacklist.Ban(address); // Ban again
                    int countAfter = AddressBlacklist.Count;

                    Expect(countAfter).ToBe(countBefore);
                    Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                });

                It("should handle null TTL gracefully", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.126", 8080);

                    AddressBlacklist.Ban(address, null); // Should use default TTL

                    Expect(AddressBlacklist.IsBanned(address)).ToBe(true);
                });

                It("should handle negative TTL", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.127", 8080);

                    AddressBlacklist.Ban(address, TimeSpan.FromSeconds(-1));

                    // Should be expired immediately
                    Expect(AddressBlacklist.IsBanned(address)).ToBe(false);
                });
            });

            Describe("AddressBlacklist Concurrency", () =>
            {
                It("should handle concurrent ban operations", () =>
                {
                    var addresses = new Address[10];
                    for (int i = 0; i < 10; i++)
                    {
                        addresses[i] = Address.CreateFromIpPort($"192.168.1.{130 + i}", 8080);
                    }

                    var tasks = new Task[10];
                    for (int i = 0; i < 10; i++)
                    {
                        int index = i;
                        tasks[i] = Task.Run(() => AddressBlacklist.Ban(addresses[index]));
                    }

                    Task.WaitAll(tasks);

                    for (int i = 0; i < 10; i++)
                    {
                        Expect(AddressBlacklist.IsBanned(addresses[i])).ToBe(true);
                    }
                });

                It("should handle concurrent read operations", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.140", 8080);
                    AddressBlacklist.Ban(address);

                    var tasks = new Task<bool>[10];
                    for (int i = 0; i < 10; i++)
                    {
                        tasks[i] = Task.Run(() => AddressBlacklist.IsBanned(address));
                    }

                    Task.WaitAll(tasks);

                    foreach (var task in tasks)
                    {
                        Expect(task.Result).ToBe(true);
                    }
                });

                // It("should handle concurrent sweep operations", () =>
                // {
                //     // Add some entries
                //     for (int i = 0; i < 5; i++)
                //     {
                //         var address = Address.CreateFromIpPort($"192.168.1.{141 + i}", 8080);
                //         AddressBlacklist.Ban(address, TimeSpan.FromMilliseconds(50));
                //     }
                //
                //     System.Threading.Thread.Sleep(100);
                //
                //     var tasks = new Task<int>[3];
                //     for (int i = 0; i < 3; i++)
                //     {
                //         tasks[i] = Task.Run(() => AddressBlacklist.SweepExpired());
                //     }
                //
                //     Task.WaitAll(tasks);
                //
                //     // At least one sweep should have found expired entries
                //     int totalRemoved = tasks.Sum(t => t.Result);
                //     Expect(totalRemoved).ToBeGreaterThanOrEqualTo(0);
                // });
            });

            Describe("AddressBlacklist Memory and Cleanup", () =>
            {
                It("should handle large number of entries", () =>
                {
                    var addresses = new Address[100];
                    for (int i = 0; i < 100; i++)
                    {
                        addresses[i] = Address.CreateFromIpPort($"192.168.3.{i}", (ushort)(8000 + i));
                        AddressBlacklist.Ban(addresses[i]);
                    }

                    Expect(AddressBlacklist.Count).ToBeGreaterThanOrEqualTo(100);

                    // Clean up
                    foreach (var address in addresses)
                    {
                        AddressBlacklist.Unban(address);
                    }
                });
            });
        }
    }
}
