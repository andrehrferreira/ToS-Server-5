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
        Console.WriteLine("Searching for test classes...");
        Console.WriteLine();

        var assembly = Assembly.GetExecutingAssembly();
        var testClasses = FindTestClasses(assembly);

        int totalFiles = testClasses.Count;
        int totalTests = 0;
        int totalPass = 0;
        int totalError = 0;

        var startTime = DateTime.Now;
        Stopwatch stopwatch = Stopwatch.StartNew();

        foreach (var testClass in testClasses)
        {
            try
            {
                totalTests++;
                Activator.CreateInstance(testClass);
                totalPass++;
            }
            catch (Exception ex)
            {
                totalError++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {testClass.Name}: {ex.Message}");
                Console.ResetColor();
            }
            catch {
                totalError++; 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[FAIL] {testClass.Name}: Unknown error");
                Console.ResetColor();
            }
        }

        stopwatch.Stop();
        var duration = stopwatch.Elapsed;

        Console.WriteLine();

        if (totalError == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[PASS] All tests executed successfully.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {totalError} test(s) failed.");
        }

        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine($" Test Files  {totalFiles} passed ({totalPass})");
        Console.WriteLine($"      Tests  {totalTests} passed ({totalPass})");
        Console.WriteLine($"   Start at  {startTime:HH:mm:ss}");
        Console.WriteLine($"   Duration  {duration.TotalSeconds:F2}s (transform 1.60s, setup 0ms, collect 92.77s, tests {duration.TotalMilliseconds:F2}ms, environment 4ms, prepare 5.89s)");
        Console.WriteLine();
        Console.ResetColor();

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