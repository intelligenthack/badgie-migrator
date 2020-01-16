using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

[assembly: InternalsVisibleToAttribute("Badgie.Migrator.Tests")]

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

        public List<Config> Configurations { get; set; }

        public static Config FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var configurations = JsonConvert.DeserializeObject<List<Config>>(json);
            if (configurations == null || !configurations.Any()) return null;
            if (configurations.Count > 1) configurations[0].Configurations = configurations;
            return configurations[0];
        }

        internal static Func<string, string> FileLoader = (path) => System.IO.File.ReadAllText(path);

        public static Config FromArgs(string[] args)
        {
            var _config = new Config();

            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.Error.WriteLine(@"Usage: dotnet-badgie-migrator <connection string> [drive:][path][filename] [-d:(SqlServer|Postgres)] [-f] [-i] [-n]
-f                      runs mutated migrations
-i                      if needed, installs the db table needed to store state
-d:(SqlServer|Postgres) specifies whether to run against SQL Server or PostgreSQL
-n                      avoids wrapping each execution in a transaction

Alternative usage: dotnet-badgie-migrator -json=filename
-json                   path to a json file that contains a array of configurations 
                        which will be executed in order.

                        Example json configuration file:
                        [
                          {
                            ""ConnectionString"": <connection string>,
                            ""Force"": true|false,
                            ""Install"": true|false,
                            ""SqlType"": ""SqlServer""|""Postgres"",
                            ""UseTransaction"": true|false
                          },                      
                          {
                            ""ConnectionString"": <connection string>,
                            ""Force"": true|false,
                            ""Install"": true|false,
                            ""SqlType"": ""SqlServer""|""Postgres"",
                            ""UseTransaction"": true|false
                          }
                        ]");
                return null;
            }

            var big = string.Concat(args);
            if (big.StartsWith("-json="))
            {
                return FromJson(FileLoader(big.Substring("-json=".Length)));
            }

            _config.ConnectionString = args.First();

            if (args.Length > 1)
            {
                foreach (var str in args.Skip(1))
                {
                    switch (str.Substring(0, 2))
                    {
                        case "-f":
                            _config.Force = true;
                            break;

                        case "-i":
                            _config.Install = true;
                            break;

                        case "-d":
                            if (!str.StartsWith("-d:") ||
                                !Enum.TryParse<SqlType>(str.Substring(3), out var sqlType))
                            {
                                return null;
                            }

                            _config.SqlType = sqlType;
                            break;

                        case "-n":
                            _config.UseTransaction = false;
                            break;

                        default:
                            _config.Path = str;
                            break;
                    }
                }
            }
            return _config;
        }
    }
}
