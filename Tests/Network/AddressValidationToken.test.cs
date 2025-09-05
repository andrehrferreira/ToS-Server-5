using NanoSockets;
using Wormhole;

namespace Tests
{
    public class AddressValidationTokenTests : AbstractTest
    {
        public AddressValidationTokenTests()
        {
            Describe("AddressValidationToken Cookie Generation", () =>
            {
                It("should generate valid cookies for different addresses", () =>
                {
                    var address1 = Address.CreateFromIpPort("192.168.1.100", 8080);
                    var address2 = Address.CreateFromIpPort("192.168.1.101", 8080);

                    byte[] cookie1 = AddressValidationToken.Generate(address1);
                    byte[] cookie2 = AddressValidationToken.Generate(address2);

                    Expect(cookie1).NotToBeNull();
                    Expect(cookie2).NotToBeNull();
                    Expect(cookie1.Length).ToBe(48);
                    Expect(cookie2.Length).ToBe(48);

                    bool identical = true;
                    for (int i = 0; i < cookie1.Length; i++)
                    {
                        if (cookie1[i] != cookie2[i])
                        {
                            identical = false;
                            break;
                        }
                    }

                    Expect(identical).ToBe(false);
                });

                It("should generate different cookies for same address on subsequent calls", () =>
                {
                    var address = Address.CreateFromIpPort("127.0.0.1", 9090);

                    byte[] cookie1 = AddressValidationToken.Generate(address);
                    byte[] cookie2 = AddressValidationToken.Generate(address);

                    Expect(cookie1).NotToBeNull();
                    Expect(cookie2).NotToBeNull();

                    bool identical = true;

                    for (int i = 0; i < cookie1.Length; i++)
                    {
                        if (cookie1[i] != cookie2[i])
                        {
                            identical = false;
                            break;
                        }
                    }

                    Expect(identical).ToBe(false);
                });

                It("should generate cookies with correct structure", () =>
                {
                    var address = Address.CreateFromIpPort("10.0.0.1", 3000);
                    byte[] cookie = AddressValidationToken.Generate(address);

                    Expect(cookie.Length).ToBe(48);

                    long timestamp = BitConverter.ToInt64(cookie, 0);
                    var cookieTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                    var now = DateTimeOffset.UtcNow;

                    var timeDiff = Math.Abs((now - cookieTime).TotalSeconds);
                    Expect(timeDiff).ToBeLessThan(1.0);
                });

                It("should handle localhost address", () =>
                {
                    var localhost = Address.CreateFromIpPort("127.0.0.1", 8080);
                    byte[] cookie = AddressValidationToken.Generate(localhost);

                    Expect(cookie).NotToBeNull();
                    Expect(cookie.Length).ToBe(48);
                });

                It("should handle zero port address", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.50", 0);
                    byte[] cookie = AddressValidationToken.Generate(address);

                    Expect(cookie).NotToBeNull();
                    Expect(cookie.Length).ToBe(48);
                });
            });

            Describe("AddressValidationToken Cookie Validation", () =>
            {
                It("should validate freshly generated cookies", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.200", 5000);
                    byte[] cookie = AddressValidationToken.Generate(address);

                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(true);
                });

                It("should reject cookies with wrong length", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.201", 5001);
                    byte[] invalidCookie = new byte[32];

                    bool isValid = AddressValidationToken.Validate(invalidCookie, address);
                    Expect(isValid).ToBe(false);
                });

                It("should reject null cookies", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.202", 5002);
                    bool isValid = AddressValidationToken.Validate(null, address);
                    Expect(isValid).ToBe(false);
                });

                It("should reject empty cookies", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.203", 5003);
                    byte[] emptyCookie = new byte[0];

                    bool isValid = AddressValidationToken.Validate(emptyCookie, address);
                    Expect(isValid).ToBe(false);
                });

                It("should reject cookies for different addresses", () =>
                {
                    var address1 = Address.CreateFromIpPort("192.168.1.204", 5004);
                    var address2 = Address.CreateFromIpPort("192.168.1.205", 5005);

                    byte[] cookie = AddressValidationToken.Generate(address1);
                    bool isValid = AddressValidationToken.Validate(cookie, address2);

                    Expect(isValid).ToBe(false);
                });

                It("should reject cookies with tampered signature", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.206", 5006);
                    byte[] cookie = AddressValidationToken.Generate(address);
                    cookie[16] ^= 0x01;

                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(false);
                });

                It("should reject cookies with tampered timestamp", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.207", 5007);
                    byte[] cookie = AddressValidationToken.Generate(address);
                    cookie[0] ^= 0x01;

                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(false);
                });

                It("should reject cookies with tampered random data", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.208", 5008);
                    byte[] cookie = AddressValidationToken.Generate(address);
                    cookie[8] ^= 0x01;

                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(false);
                });

                It("should handle validation of multiple cookies concurrently", () =>
                {
                    var addresses = new Address[10];
                    var cookies = new byte[10][];

                    for (int i = 0; i < 10; i++)
                    {
                        addresses[i] = Address.CreateFromIpPort($"192.168.1.{210 + i}", (ushort)(6000 + i));
                        cookies[i] = AddressValidationToken.Generate(addresses[i]);
                    }

                    var tasks = new Task<bool>[10];
                    for (int i = 0; i < 10; i++)
                    {
                        int index = i;
                        tasks[i] = Task.Run(() => AddressValidationToken.Validate(cookies[index], addresses[index]));
                    }

                    Task.WaitAll(tasks);

                    for (int i = 0; i < 10; i++)
                    {
                        Expect(tasks[i].Result).ToBe(true);
                    }
                });
            });

            Describe("AddressValidationToken Cookie Expiration", () =>
            {
                It("should reject expired cookies", async () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.220", 7000);
                    byte[] cookie = AddressValidationToken.Generate(address);

                    long oldTimestamp = DateTimeOffset.UtcNow.AddSeconds(-15).ToUnixTimeSeconds();
                    byte[] timestampBytes = BitConverter.GetBytes(oldTimestamp);
                    Array.Copy(timestampBytes, 0, cookie, 0, 8);

                    var address_copy = address;
                    unsafe
                    {
                        byte* addrPtr = (byte*)&address_copy;
                        var data = new List<byte>();
                        data.AddRange(new ReadOnlySpan<byte>(addrPtr, sizeof(Address)));
                        data.AddRange(timestampBytes);
                        data.AddRange(cookie.AsSpan(8, 8).ToArray());

                        using var hmac = new System.Security.Cryptography.HMACSHA256(GetServerSecret());
                        var signature = hmac.ComputeHash(data.ToArray());
                        Array.Copy(signature, 0, cookie, 16, 32);
                    }

                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(false);
                });

                It("should accept cookies within TTL", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.221", 7001);
                    byte[] cookie = AddressValidationToken.Generate(address);
                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(true);
                });

                It("should handle edge case of exactly TTL limit", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.222", 7002);
                    byte[] cookie = AddressValidationToken.Generate(address);
                    long exactTTLTimestamp = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeSeconds();
                    byte[] timestampBytes = BitConverter.GetBytes(exactTTLTimestamp);
                    Array.Copy(timestampBytes, 0, cookie, 0, 8);
                    var address_copy = address;
                    unsafe
                    {
                        byte* addrPtr = (byte*)&address_copy;
                        var data = new List<byte>();
                        data.AddRange(new ReadOnlySpan<byte>(addrPtr, sizeof(Address)));
                        data.AddRange(timestampBytes);
                        data.AddRange(cookie.AsSpan(8, 8).ToArray());

                        using var hmac = new System.Security.Cryptography.HMACSHA256(GetServerSecret());
                        var signature = hmac.ComputeHash(data.ToArray());
                        Array.Copy(signature, 0, cookie, 16, 32);
                    }

                    bool isValid = AddressValidationToken.Validate(cookie, address);
                    Expect(isValid).ToBe(false);
                });
            });

            Describe("AddressValidationToken Security Properties", () =>
            {
                It("should generate unique random components", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.230", 8000);
                    var cookies = new byte[5][];

                    for (int i = 0; i < 5; i++)
                    {
                        cookies[i] = AddressValidationToken.Generate(address);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        for (int j = i + 1; j < 5; j++)
                        {
                            bool randomPartsIdentical = true;
                            for (int k = 8; k < 16; k++)
                            {
                                if (cookies[i][k] != cookies[j][k])
                                {
                                    randomPartsIdentical = false;
                                    break;
                                }
                            }
                            Expect(randomPartsIdentical).ToBe(false);
                        }
                    }
                });

                It("should maintain consistent validation for same cookie", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.231", 8001);
                    byte[] cookie = AddressValidationToken.Generate(address);

                    for (int i = 0; i < 10; i++)
                    {
                        bool isValid = AddressValidationToken.Validate(cookie, address);
                        Expect(isValid).ToBe(true);
                    }
                });

                It("should handle concurrent cookie generation safely", () =>
                {
                    var address = Address.CreateFromIpPort("192.168.1.232", 8002);
                    var cookies = new byte[10][];
                    var tasks = new Task[10];

                    for (int i = 0; i < 10; i++)
                    {
                        int index = i; // Capture for closure
                        tasks[i] = Task.Run(() =>
                        {
                            cookies[index] = AddressValidationToken.Generate(address);
                        });
                    }

                    Task.WaitAll(tasks);

                    for (int i = 0; i < 10; i++)
                    {
                        Expect(cookies[i]).NotToBeNull();
                        Expect(cookies[i].Length).ToBe(48);

                        bool isValid = AddressValidationToken.Validate(cookies[i], address);
                        Expect(isValid).ToBe(true);
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        for (int j = i + 1; j < 10; j++)
                        {
                            bool identical = true;
                            for (int k = 0; k < cookies[i].Length; k++)
                            {
                                if (cookies[i][k] != cookies[j][k])
                                {
                                    identical = false;
                                    break;
                                }
                            }
                            Expect(identical).ToBe(false);
                        }
                    }
                });

                It("should handle different port numbers for same IP", () =>
                {
                    var address1 = Address.CreateFromIpPort("192.168.1.233", 8003);
                    var address2 = Address.CreateFromIpPort("192.168.1.233", 8004);

                    byte[] cookie1 = AddressValidationToken.Generate(address1);
                    byte[] cookie2 = AddressValidationToken.Generate(address2);

                    bool identical = true;
                    for (int i = 0; i < cookie1.Length; i++)
                    {
                        if (cookie1[i] != cookie2[i])
                        {
                            identical = false;
                            break;
                        }
                    }

                    Expect(identical).ToBe(false);
                    Expect(AddressValidationToken.Validate(cookie1, address1)).ToBe(true);
                    Expect(AddressValidationToken.Validate(cookie1, address2)).ToBe(false);
                    Expect(AddressValidationToken.Validate(cookie2, address1)).ToBe(false);
                    Expect(AddressValidationToken.Validate(cookie2, address2)).ToBe(true);
                });
            });
        }

        private byte[] GetServerSecret()
        {
            var field = typeof(AddressValidationToken).GetField("ServerSecret",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (byte[])field.GetValue(null);
        }
    }
}
