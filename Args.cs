using System.Collections.Generic;
using static System.Console;

namespace headless
{
    public sealed class Args
    {
        public IReadOnlyList<string> Positional;
        public IReadOnlyDictionary<string, string> Named;
        public IReadOnlyList<string> Reminder;

        private Args(IReadOnlyList<string> positional, IReadOnlyDictionary<string, string> named, IReadOnlyList<string> reminder)
        {
            Positional = positional;
            Named = named;
            Reminder = reminder ?? new string[0];
        }

        public static Args Parse(IEnumerable<string> cmdArgs, HashSet<string> namedArgNames)
        {
            var positional = new List<string>();
            var named = new Dictionary<string, string>();
            List<string> reminder = null;

            string namedArg = null;
            foreach (var arg in cmdArgs)
            {
                if (reminder != null)
                {
                    reminder.Add(arg);
                }
                else if (namedArg != null)
                {
                    named.Add(namedArg, arg);
                    namedArg = null;
                }
                else if (arg == "--")
                {
                    reminder = new List<string>();
                }
                else if(arg.StartsWith("--"))
                {
                    var argparts = arg.Substring(2).Split('=');
                    if (namedArgNames.Contains(argparts[0]))
                    {
                        if (argparts.Length > 1)
                        {
                            named.Add(argparts[0], argparts[1]);
                            if (argparts.Length > 2)
                            {
                                WriteLine($"WARN: Extra '=' delimiters in {arg} are ignored");
                            }
                        }
                        else
                        {
                            // will wait for arg
                            namedArg = arg.Substring(2);
                        }
                    }
                    else
                    {
                        positional.Add(arg);
                    }
                }
                else
                {
                    positional.Add(arg);
                }
            }

            if (namedArg != null)
            {
                WriteLine($"Error: expect value for arg {namedArg}");
            }
            
            return new Args(positional.AsReadOnly(), named, reminder?.AsReadOnly());
        }
    }}