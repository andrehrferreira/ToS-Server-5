using System.IO;

namespace Tests
{
    public class IntegritySystemTests : AbstractTest
    {
        public IntegritySystemTests()
        {
            Describe("IntegritySystem", () =>
            {
                It("should generate and load integrity table", () =>
                {
                    string projectDir = IntegritySystem.GetProjectDirectory();
                    string packageJson = Path.Combine(projectDir, "package.json");
                    string unreal = Path.Combine(projectDir, "Unreal");
                    string tables = Path.Combine(projectDir, "Tables");

                    if (Directory.Exists(tables))
                        Directory.Delete(tables, true);

                    IntegritySystem.InitializeIntegrityTable(packageJson, unreal);
                    bool result = IntegritySystem.GetCurrentTable(out var table);

                    Expect(result).ToBeTrue();
                    Expect(table).NotToBeNull();
                    Expect(table.KeyCount).ToBeGreaterThan(0);

                    if (Directory.Exists(tables))
                        Directory.Delete(tables, true);
                });
            });
        }
    }
}
