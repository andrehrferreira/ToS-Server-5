/*
* ContractTraspiler
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

using System;
using System.Reflection;

public class ContractTraspiler : AbstractTranspiler
{
    public static void Generate()
    {
        var contracts = GetContracts();

        string projectDirectory = GetProjectDirectory();
        string baseDirectoryPath = Path.Combine(projectDirectory, "Core", "Packets");
        string networkDirectoryPath = Path.Combine(projectDirectory, "Core", "Network");

        List<string> serverPackets = new List<string>();
        List<string> clientPackets = new List<string>();

        if (!Directory.Exists(baseDirectoryPath))
            Directory.CreateDirectory(baseDirectoryPath);

        if (!Directory.Exists(networkDirectoryPath))
            Directory.CreateDirectory(networkDirectoryPath);

        foreach (var contract in contracts)
        {
            var fields = contract.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var attribute = contract.GetCustomAttribute<ContractAttribute>();
            var contractName = contract.Name;
            var rawName = contractName.Replace("Contract", "");

            string filePath = Path.Combine(baseDirectoryPath, $"{rawName}Packet.cs");

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("// This file was generated automatically, please do not change it.");
                writer.WriteLine();
                writer.WriteLine("using System.Runtime.CompilerServices;");
                writer.WriteLine();
                writer.WriteLine($"public struct {rawName}Packet");
                writer.WriteLine("{");

                //Body

                writer.WriteLine("}");
                writer.WriteLine();
            }
        }
    }
}