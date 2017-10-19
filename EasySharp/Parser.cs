using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySharp
{
    public static class Parser
    {
        private const string KEY_IMPORT = "import";
        private const string KEY_ASSERT = "assert";
        private const string KEY_PRINT = "print";
        private const string KEY_FOR = "for";
        private const string KEY_IN = "in";

        public static string ConvertToCSharp(List<string> esLines)
        {
            bool includeAssert = false;
            List<string> listUsing = new List<string>();
            List<string> programLines = new List<string>();

            // Remove leading and trailing whitespaces
            // Go through the source code lines
            foreach (string line in esLines.Select(x => x.TrimWhitespace()))
            {
                // Skip empty lines
                if (line == string.Empty)
                {
                    continue;
                }

                // Import
                if (line.IsKey(KEY_IMPORT, out string importValue))
                {
                    listUsing.AddRange(importValue.SplitValues());
                }
                // Assert
                else if (line.IsKey(KEY_ASSERT, out string assertValue))
                {
                    // Debug.Assert() only works in debug mode
                    //programLines.Add(string.Format("System.Diagnostics.Debug.Assert({0});", assertValue));

                    includeAssert = true;
                    programLines.Add(string.Format("__Assert({0}, \"{0}\");", assertValue));
                }
                // Print
                else if (line.IsKey(KEY_PRINT, out string printValue))
                {
                    programLines.Add(string.Format("System.Console.WriteLine({0});", string.Join(" + \' \' + ", printValue.SplitValues())));
                }
                // For(each)
                else if (IsFor(line, out string iterator, out string enumerable))
                {
                    programLines.Add(string.Format("foreach (var {0} in {1})", iterator, enumerable));
                }
                // Regular code
                else
                {
                    // Automatically add semicolons to the end of commands
                    // Ignore lines that already end with a semicolon and C# code blocks
                    // which are not supposed to be terminated by a semicolon
                    if (!line.EndsWith(";") &&
                        !line.IsOldKey("if") &&
                        !line.IsOldKey("else") &&
                        !line.IsOldKey("while") &&
                        !line.IsOldKey("for") &&
                        !line.IsOldKey("foreach"))
                    {
                        programLines.Add(line + ";");
                    }
                    else
                    {
                        programLines.Add(line);
                    }
                }
            }

            return
                // Using statements
                string.Join(string.Empty, listUsing.Select(x => $"using {x};" + Environment.NewLine)) +

                // Program class scope
                "public class Program" + Environment.NewLine +
                "{" + Environment.NewLine +

                // Main function scope
                "private static void Main(string[] args)" + Environment.NewLine +
                "{" + Environment.NewLine +

                // Main function C# code
                string.Join(string.Empty, programLines.Select(x => x + Environment.NewLine)) +

                // End of main function
                "}" + Environment.NewLine +

                // Assert function
                (includeAssert ?
                "private static void __Assert(bool conditionResult, string conditionString)" + Environment.NewLine +
                "{" + Environment.NewLine +
                "if (!conditionResult)" + Environment.NewLine +
                "{" + Environment.NewLine +
                "System.Console.Write(\"Assertion failed: \" + conditionString + System.Environment.NewLine + \"Press ENTER to exit...\");" + Environment.NewLine +
                "System.Console.ReadLine();" + Environment.NewLine +
                "System.Environment.Exit(-1);" + Environment.NewLine +
                "}" + Environment.NewLine +
                "}" + Environment.NewLine :
                string.Empty) +

                // End of Program class
                "}";
        }

        private static bool IsFor(string str, out string iterator, out string enumerable)
        {
            if (str.IsKey(KEY_FOR, out string value))
            {
                List<string> parts = value.SplitValues(' ', 3);

                if (parts.Count == 3 && parts[1] == KEY_IN)
                {
                    iterator = parts[0];
                    enumerable = parts[2];
                    return true;
                }
            }

            iterator = string.Empty;
            enumerable = string.Empty;

            return false;
        }
    }

    public static class Extensions
    {
        public static string TrimWhitespace(this string str) => str.Trim(' ', '\t');

        public static bool IsKey(this string str, string key, out string value)
        {
            if (str.StartsWith(key + " "))
            {
                value = str.Substring(key.Length + 1).TrimWhitespace();
                return true;
            }
            else
            {
                value = string.Empty;
                return false;
            }
        }

        public static bool IsOldKey(this string str, string key) => (str.StartsWith(key + " ") || str.StartsWith(key + "("));

        public static List<string> SplitValues(this string str, char splitChar = ',', int maxValues = 0)
        {
            List<string> values = new List<string>();

            int valueStartIdx = 0;
            bool insideQuotes = false;

            int depth = 0;

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '"' && (i == 0 || str[i - 1] != '\\'))
                {
                    insideQuotes = !insideQuotes;
                }
                else if (!insideQuotes)
                {
                    if (str[i] == '(' || str[i] == '[' || str[i] == '{')
                    {
                        depth++;
                    }
                    else if (str[i] == ')' || str[i] == ']' || str[i] == '}')
                    {
                        depth--;
                    }
                    else if (depth == 0 && str[i] == splitChar)
                    {
                        int len = i - valueStartIdx;
                        values.Add(str.Substring(valueStartIdx, len).TrimWhitespace());
                        valueStartIdx = i + 1;

                        if (maxValues > 0 && values.Count == maxValues - 1)
                        {
                            break;
                        }
                    }
                }
            }

            values.Add(str.Substring(valueStartIdx).TrimWhitespace());

            return values;
        }
    }
}