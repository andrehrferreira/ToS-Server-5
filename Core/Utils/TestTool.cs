/*
 * TestTool
 *
 * Author: Andre Ferreira
 *
 * Copyright (c) Uzmi Games. Licensed under the MIT License.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Diagnostics;
using System.Reflection;

public class TestTool
{
    private readonly string _projectDirectory;

    public TestTool()
    {
        _projectDirectory = GetProjectDirectory();
    }

    public bool RunAllTests()
    {
        Console.WriteLine("🧪 Starting Test Suite...");
        Console.WriteLine($"🔍 Searching for test classes...");
        Console.WriteLine();

        var assembly = Assembly.GetExecutingAssembly();
        var testClasses = FindTestClasses(assembly);

        Console.WriteLine($"📋 Found {testClasses.Count} test classes to execute.");
        Console.WriteLine();

        int totalFiles = testClasses.Count;
        int totalTests = 0;
        int totalPass = 0;
        int totalError = 0;

        var startTime = DateTime.Now;
        Stopwatch stopwatch = Stopwatch.StartNew();

                        foreach (var testClass in testClasses)
        {
            Console.WriteLine($"🔍 Running {testClass.Name}...");

            try
            {
                totalTests++;
                Activator.CreateInstance(testClass);
                totalPass++;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ {testClass.Name} - PASSED");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                totalError++;
                Console.ForegroundColor = ConsoleColor.Red;

                // Handle TargetInvocationException to get the actual test failure
                if (ex is TargetInvocationException tie && tie.InnerException != null)
                {
                    Console.WriteLine($"❌ {testClass.Name} - FAILED: {tie.InnerException.Message}");
                    if (tie.InnerException.StackTrace != null)
                    {
                        Console.WriteLine($"   Stack trace: {tie.InnerException.StackTrace}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ {testClass.Name} - FAILED: {ex.Message}");
                    if (ex.StackTrace != null)
                    {
                        Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                    }
                }

                Console.ResetColor();
            }

            Console.WriteLine(); // Add spacing between test classes
        }

        stopwatch.Stop();
        var duration = stopwatch.Elapsed;

        Console.WriteLine();

        if (totalError == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ [PASS] All {totalFiles} test classes executed successfully.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ [FAIL] {totalError} out of {totalFiles} test classes failed.");
        }

        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($"📊 Test Summary:");
        Console.WriteLine($"   Test Classes: {totalFiles} total");
        Console.WriteLine($"   Passed: {totalPass} ({(totalPass * 100.0 / totalFiles):F1}%)");
        Console.WriteLine($"   Failed: {totalError} ({(totalError * 100.0 / totalFiles):F1}%)");
        Console.WriteLine($"   Start Time: {startTime:HH:mm:ss}");
        Console.WriteLine($"   Duration: {duration.TotalSeconds:F2}s");
        Console.WriteLine();

        if (totalError == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🎉 All tests passed! Server can start safely.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("💥 Some tests failed! Server startup will be blocked.");
        }

        Console.ResetColor();
        Console.WriteLine();

        return totalError == 0;
    }

    private List<Type> FindTestClasses(Assembly assembly)
    {
        return assembly.GetTypes()
                       .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AbstractTest)))
                       .ToList();
    }

    private string GetProjectDirectory()
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        var directoryInfo = new DirectoryInfo(assemblyPath);

        while (directoryInfo != null && directoryInfo.Name != "bin")
            directoryInfo = directoryInfo.Parent;

        if (directoryInfo != null && directoryInfo.Parent != null)
            return directoryInfo.Parent.FullName;

        return AppDomain.CurrentDomain.BaseDirectory;
    }
}
