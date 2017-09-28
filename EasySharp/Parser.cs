using System.Collections.Generic;
using System.Linq;

namespace EasySharp
{
    public class Parser
    {
        public string CSharpCode { get; private set; }

        private const string KEY_IMPORT = "import";
        private const string KEY_PRINT = "print";
        private const string KEY_FOR = "for";
        private const string KEY_IN = "in";

        public Parser(List<string> esLines)
        {
            CSharpCode = string.Empty;

            string strProgram = string.Empty;

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
                if (line.IsKey(KEY_IMPORT, out string valueImport))
                {
                    foreach (string x in valueImport.SplitValues())
                    {
                        CSharpCode += string.Format("using {0};", x);
                    }
                }
                // Print
                else if (line.IsKey(KEY_PRINT, out string valuePrint))
                {
                    foreach (string x in valuePrint.SplitValues())
                    {
                        strProgram += string.Format("System.Console.WriteLine({0});", x);
                    }
                }
                // Python-like For
                else if (IsFor(line, out string iterator, out string enumerable))
                {
                    strProgram += string.Format("foreach (var {0} in {1})", iterator, enumerable);
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
                        strProgram += line + ";";
                    }
                    else
                    {
                        strProgram += line;
                    }
                }
            }

            CSharpCode += "public class Program { private static void Main(string[] args) { " + strProgram + " } }";
        }

        private bool IsFor(string str, out string iterator, out string enumerable)
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