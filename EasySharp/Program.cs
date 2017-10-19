using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace EasySharp
{
    public class Program
    {
        private const string VS_DEV_BATCH_PATH = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\Common7\\Tools\\VsDevCmd.bat";

        private const string SOURCE_PATH = "E:\\EasySharp.es";
        private const string OUTPUT_PATH = "E:\\EasySharp.exe";

        private static void Main(string[] args)
        {
            // Read the E# source code line by line
            List<string> esharpLines = new List<string>();

            using (StreamReader sr = new StreamReader(SOURCE_PATH))
            {
                while (!sr.EndOfStream)
                {
                    esharpLines.Add(sr.ReadLine());
                }
            }

            // Write temporary C# code
            string tempCSharpPath = Path.GetTempFileName();

            using (StreamWriter sw = new StreamWriter(tempCSharpPath))
            {
                sw.Write(Parser.ConvertToCSharp(esharpLines));
            }

            // Show the C# code
            Process.Start("notepad++", tempCSharpPath);

            // Run the C# compiler
            Process.Start("cmd", string.Format("/c (\"{0}\") && csc \"{1}\" /out:\"{2}\" || pause", VS_DEV_BATCH_PATH, tempCSharpPath, OUTPUT_PATH));
        }
    }
}