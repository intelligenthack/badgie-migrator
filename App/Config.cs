using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Badgie.Migrator.Tests")]

namespace Badgie.Migrator
{
    public class Config
    {
        public string ConnectionString { get; set; }
        public bool Force { get; set; } = false;
        public bool Install { get; set; } = false;
        public SqlType SqlType { get; set; } = SqlType.Postgres;
        public string Path { get; set; } = ".";
        public bool UseTransaction { get; set; } = true;
        public bool Verbose { get; set; } = false;
        public bool StackTraces {get; set; } = true;
        public bool StrictEncoding {get; set; } = false;

        public List<Config> Configurations { get; set; }

        public static Config FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var configurations = JsonConvert.DeserializeObject<List<Config>>(json);
            if (configurations == null || !configurations.Any()) return null;
            if (configurations.Count > 1) configurations[0].Configurations = configurations;
            return configurations[0];
        }

        internal static Func<string, string> FileLoader = System.IO.File.ReadAllText;

        public static Config FromArgs(string[] args)
        {
            var config = new Config();

            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.Error.WriteLine(@"Usage: dotnet-badgie-migrator <connection string> [drive:][path][filename] [-d:(SqlServer|Postgres|MySql)] [-f] [-i] [-n] [-V] [--no-stack-trace] [--strict-encoding]
-f                      runs mutated migrations
-i                      if needed, installs the db table needed to store state
-d:<type>               specifies whether to run against SQL Server, PostgreSQL or MySql
-n                      avoids wrapping each execution in a transaction
-V                      Verbose mode: executes with tracing
--no-stack-trace        Omit the (mostly useless) stack traces
--strict-encoding       Refuse to run migrations containing invalid characters

Alternative usage: dotnet-badgie-migrator -json=filename
-json                   path to a json file that contains a array of configurations 
                        which will be executed in order.

                        Example json configuration file:
                        [
                          {
                            ""ConnectionString"": <connection string>,
                            ""Force"": true|false,
                            ""Install"": true|false,
                            ""SqlType"": ""SqlServer""|""Postgres""|""MySql"",
                            ""Path"": ""<path to migrations with wildcards>"",
                            ""UseTransaction"": true|false,
                            ""StackTraces"": true|false,
                            ""StrictEncoding"": true|false
                          },
                          {
                            ""ConnectionString"": <connection string>,
                            ""Force"": true|false,
                            ""Install"": true|false,
                            ""SqlType"": ""SqlServer""|""Postgres""|""MySql"",
                            ""Path"": ""<path to migrations with wildcards>"",
                            ""UseTransaction"": true|false,
                            ""StackTraces"": true|false,
                            ""StrictEncoding"": true|false
                          }
                        ]");
                return null;
            }

            var big = string.Concat(args);
            if (big.StartsWith("-json="))
            {
                return FromJson(FileLoader(big["-json=".Length..]));
            }

            config.ConnectionString = args.First();

            if (args.Length <= 1) return config;

            foreach (var str in args.Skip(1))
            {
                switch (str[..2])
                {
                    case "-f":
                        config.Force = true;
                        break;

                    case "-V":
                        config.Verbose = true;
                        break;

                    case "--":
                    {
                        switch (str[2..])
                        {
                            case "no-stack-trace":
                                config.StackTraces = false;
                                break;

                            case "strict-encoding":
                                config.StrictEncoding = true;
                                break;
                        }
                        break;
                    }

                    case "-i":
                        config.Install = true;
                        break;

                    case "-d":
                        if (!str.StartsWith("-d:") ||
                            !Enum.TryParse<SqlType>(str[3..], out var sqlType))
                        {
                            return null;
                        }

                        config.SqlType = sqlType;
                        break;

                    case "-n":
                        config.UseTransaction = false;
                        break;

                    default:
                        config.Path = str;
                        break;
                }
            }
            return config;
        }
    }
}
