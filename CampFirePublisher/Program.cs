using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using ccnet.campfire.plugin;
using System.Reflection;
using System.Net;
using System.IO;
using System.Xml;

namespace CampFirePublisher {
    public class Program {
        static void Main(string[] args) {
            var arguments = new Arguments();
            ICommandLineParser parser = new CommandLineParser();
            if (args.Length > 0) {
                if (parser.ParseArguments(args, arguments)) {
                    if (arguments.ShowHelp == false) {
                        try {
                            Run(arguments);
                            System.Environment.ExitCode = 0;
                        } catch (ArgumentException argx) {
                            Out(argx.Message);
                            System.Environment.ExitCode = 1;
                        } catch (Exception ex) {
                            Out(ex.Message);
                            Out(ex.StackTrace);
                            System.Environment.ExitCode = 2;
                        }
                        WaitForExit(arguments.WaitForExit);
                        return;
                    }
                }
            }
            Out(arguments.HelpText());
            WaitForExit(arguments.WaitForExit);
        }

        public static void Run(Arguments arguments) {
            var room = new CampfireRoom(arguments.AccountName, arguments.AuthToken, arguments.RoomId, arguments.IsHttps);
            room.Post(arguments.Message);
        }

        static bool YesOrNo(string template, params object[] vars) {
            return YesOrNo(string.Format(template, vars));
        }

        static bool YesOrNo(string prompt) {
            string ret = Prompt(prompt);
            while (true) {
                if (ret.Equals(@"n", StringComparison.CurrentCultureIgnoreCase)) return false;
                if (ret.Equals(@"y", StringComparison.CurrentCultureIgnoreCase)) return true;
                ret = Prompt(@"Please enter y or n: ");
            }
        }

        static string Prompt(string template, params object[] vars) {
            return Prompt(string.Format(template, vars));
        }

        static string Prompt(string query) {
            Console.Write(query);
            return Console.ReadLine();
        }

        static void Out(string line) {
            Console.WriteLine(line);
        }

        static void Out(string template, params object[] vals) {
            Console.WriteLine(template, vals);
        }

        static void Separator() {
            Out(@"****************************************");
        }

        static void WaitForExit(bool wait) {
            if (wait) {
                Console.Write(@"Press <Enter> to end:");
                Console.ReadLine();
            }
        }


        public class Arguments {

            [HelpOption()]
            public string HelpText() {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(@"Usage: {0} ", System.IO.Path.GetFileName(this.GetType().Assembly.CodeBase));

                string indent = @"    ";
                StringBuilder details = new StringBuilder();

                foreach (FieldInfo fi in this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
                    foreach (object o in fi.GetCustomAttributes(false)) {
                        if (o is OptionAttribute) {
                            OptionAttribute opt = o as OptionAttribute;

                            sb.AppendFormat(@"{0}-{1} {2}{3} ", opt.Required ? @"" : @"[", opt.ShortName, opt.LongName, opt.Required ? @"" : @"]");

                            details.Append(indent);
                            details.AppendLine(string.Format(@"-{0}: {2}. {3} (long form {1}, default is '{4}')", opt.ShortName, opt.LongName, opt.Required ? @"Required" : @"Optional", opt.HelpText, fi.GetValue(this)));
                            break;
                        }

                        if (o is ValueListAttribute) {
                            ValueListAttribute vl = o as ValueListAttribute;
                            sb.AppendFormat(@"{0} [0..{1}] ", fi.Name, vl.MaximumElements == -1 ? @"*" : vl.MaximumElements.ToString());
                            break;
                        }
                    }
                }

                return sb.ToString();
            }

            [Option(@"a", @"account", HelpText = @"The account that contains the specified room.  Usually the first part of the campfire host name (http://<account>.campfirenow.com)", Required = true)]
            public string AccountName = @"";

            [Option(@"t", @"token", HelpText = @"The API token for the specified user ID", Required = true)]
            public string AuthToken = @"";

            [Option(@"r", @"room", HelpText = @"The numeric Room to which to publish messages", Required = true)]
            public int RoomId = 0;

            [Option(@"w", @"wait", HelpText = @"If TRUE, the program will wait for <Enter> before quitting")]
            public bool WaitForExit = false;

            [Option(@"s", @"https", HelpText = @"If TRUE, use HTTPS for Campfire transactions")]
            public bool IsHttps = false;

            [Option(@"?", @"help", HelpText = @"Show program usage (this text)")]
            public bool ShowHelp = false;

            [Option(@"m", @"message", HelpText = @"The text to be sent to Campfire", Required = true)]
            public string Message = @"";

        }
    }
}
