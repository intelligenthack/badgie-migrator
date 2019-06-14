using System;
using System.Linq;

namespace Badgie.Migrator
{
    public class Config
    {
        public string ConnectionString { get; set; }
        public bool Force { get; set; } = false;
        public bool Install { get; set; } = false;
        public SqlType SqlType { get; set; } = SqlType.Postgres;
        public string Path { get; set; } = ".";

        public static Config FromArgs(string[] args)
        {
            var _config = new Config();

            if (args == null || args.Length == 0 || String.IsNullOrWhiteSpace(args[0]))
            {
                Console.Error.WriteLine(@"Usage: dotnet-badgie-migrator <connection string> [drive:][path][filename] [-d:(SqlServer|Postgres)] [-f] [-i] [-d]
-f                      runs mutated migrations
-i                      if needed, installs the db table needed to store state
-d:(SqlServer|Postgres) specifies whether to run against SQL Server or PostgreSQL");
                return null;
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
