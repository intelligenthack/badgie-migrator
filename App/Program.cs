using Dapper;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Badgie.Migrator
{
    public class Program
    {

        private static void Main(string[] args)
        {
            var time = new Stopwatch();
            time.Start();

            var _config = Config.FromArgs(args);
            if (_config == null)
            {
                Environment.Exit(-2);
            }

            if (_config.Configurations != null && _config.Configurations.Count > 1)
            {
                if (_config.Verbose) Console.WriteLine("Info: {0} configurations found.", _config.Configurations.Count);
                foreach (var config in _config.Configurations)
                {
                    var time2 = new Stopwatch();
                    time2.Start();
                    Console.WriteLine("Migrating connection: \"{0}\"", config.ConnectionString);
                    if (_config.Verbose) Console.WriteLine("Info: ensuring configuration table exists...");
                    if (!EnsureTableExists(config))
                    {
                        Error("Database does not have data table installed, did you forget to pass \"-i\"?");
                    }
                    if (_config.Verbose) Console.WriteLine("Info: ...done!");
                    if (_config.Verbose) Console.WriteLine("Info: migrating folder...");
                    if (!ExecuteFolder(config))
                    {
                        Error("Execution error");
                    }
                    if (_config.Verbose) Console.WriteLine("Info: ...done!");
                    time2.Stop();
                    Console.WriteLine("Migrations done in {0:0} ms", time2.Elapsed.TotalMilliseconds);

                }

            }
            else
            {
                if (_config.Verbose) Console.WriteLine("Info: ensuring configuration table exists...");
                if (!EnsureTableExists(_config))
                {
                    Error("Database does not have data table installed, did you forget to pass \"-i\"?");
                }
                if (_config.Verbose) Console.WriteLine("Info: ...done!");

                if (_config.Verbose) Console.WriteLine("Info: migrating folder...");
                if (!ExecuteFolder(_config))
                {
                    Error("Execution error");
                }
                if (_config.Verbose) Console.WriteLine("Info: ...done!");
            }

            time.Stop();
            Console.WriteLine("All done in {0:0} ms", time.Elapsed.TotalMilliseconds);
        }

        public static void Error(string error)
        {
            Console.Error.WriteLine(error);
            System.Environment.Exit(-1);
        }

        public static bool EnsureTableExists(Config config)
        {
            bool installed;
            using (var x = CreateConnection(config))
            {
                switch (config.SqlType)
                {
                    case SqlType.MySql:
                        if (config.Verbose) Console.WriteLine("Info: verifying table on MySQL");
                        installed = x.GetSchema("Tables", new string[] { null, x.Database, "migration_runs", null }).Rows.Count > 0;
                        break;

                    case SqlType.Postgres:
                        if (config.Verbose) Console.WriteLine("Info: verifying table on Postgres");
                        installed = x.GetSchema("Tables", new string[] { null, "public", "migrationruns", null }).Rows.Count > 0;
                        break;

                    case SqlType.SqlServer:
                        if (config.Verbose) Console.WriteLine("Info: verifying table on SQL Server");
                        installed = x.GetSchema("Tables", new string[] { null, "dbo", "MigrationRuns", null }).Rows.Count > 0;
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }
            if (config.Verbose) Console.WriteLine(installed ? "Info: table found!" : "Info: table not found!");

            if (!installed && config.Install)
            {
                if (config.Verbose) Console.WriteLine("Info: attempting to create migrations table...");
                using (var x = CreateConnection(config))
                {
                    x.Execute(TableCreationStatement(config));
                }
                if (config.Verbose) Console.WriteLine("Info: ...done!");

                installed = true;
            }
            return installed;
        }

        private static DbConnection CreateConnection(Config config)
        {
            switch (config.SqlType)
            {
                case SqlType.MySql:
                    {
                        var conn = new MySqlConnection(config.ConnectionString);
                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Open();
                        }

                        return conn;
                    }
                case SqlType.Postgres:
                    {
                        var conn = new NpgsqlConnection(config.ConnectionString);
                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Open();
                        }

                        return conn;
                    }
                case SqlType.SqlServer:
                    {
                        var conn = new SqlConnection(config.ConnectionString);
                        if (conn.State == ConnectionState.Broken || conn.State == ConnectionState.Closed)
                        {
                            conn.Open();
                        }

                        return conn;
                    }
                default:
                    throw new NotSupportedException();
            }
        }


        private static string TableCreationStatement(Config config)
        {
            switch (config.SqlType)
            {
                case SqlType.SqlServer:
                    return @"
CREATE TABLE [dbo].[MigrationRuns] (
    Id              INT             IDENTITY (1, 1) NOT NULL,
    LastRun         DATETIME        NOT NULL,
    Filename        NVARCHAR(2000)  NOT NULL,
    MD5             VARCHAR(50)     NOT NULL,
    MigrationResult TINYINT         NOT NULL,
    CONSTRAINT [PK_MigrationRuns] PRIMARY KEY CLUSTERED ([Id] ASC)
);";
                case SqlType.Postgres:
                    return @"
CREATE SEQUENCE MigrationRuns_Id_seq INCREMENT 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1;
CREATE TABLE ""public"".MigrationRuns (
    Id integer DEFAULT nextval('MigrationRuns_Id_seq') NOT NULL,
    LastRun timestamp  NOT NULL,
    Filename character varying(2000) NOT NULL,
    MD5 character varying(50) NOT NULL,
    MigrationResult integer NOT NULL,
    CONSTRAINT ""MigrationRuns_Id"" PRIMARY KEY (Id)
) WITH (oids = false);";
                case SqlType.MySql:
                    return @"
CREATE TABLE `migration_runs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `last_run` datetime NOT NULL,
  `filename` text NOT NULL,
  `md5` varchar(50) NOT NULL,
  `migration_result` tinyint NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                default:
                    throw new NotSupportedException();
            }
        }

        public static bool ExecuteFolder(Config config)
        {
            var path = config.Path;
            var info = new FileInfo(path);
            var files = Directory.EnumerateFiles(info.Directory.FullName, info.Name).OrderBy(f => f).ToList();

            if (config.Verbose)
            {
                Console.WriteLine("Info: migrations found (in order):");
                foreach (var file in files)
                    Console.WriteLine("Info: {0}", file);
            }

            foreach (var file in files)
            {
                MigrationResult result;
                if (config.Verbose) Console.WriteLine("Info: handling {0}...", file);
                try
                {
                    result = ExecuteMigration(file, config);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error - {file}");
                    if (config.StackTraces) Console.Error.WriteLine(ex.ToString());
                    else Console.Error.WriteLine(ex.Message);
                    return false;
                }
                Console.WriteLine("{0} - {1}", result, file);
                if (!config.Force && result == MigrationResult.Changed)
                {
                    return false;
                }
                if (config.Verbose) Console.WriteLine("Info: ...done!");
            }
            return true;
        }

        public static MigrationResult ExecuteMigration(string migrationFilename, Config config)
        {
            var sql = File.ReadAllText(migrationFilename);
            string crc;
            using (var md5 = MD5.Create())
            {
                crc = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(sql)));
                if (config.Verbose) Console.WriteLine("Info: file MD5: {0}", crc);
            }
            MigrationRun run;
            using (var conn = CreateConnection(config))
            {
                run = conn.QueryFirstOrDefault<MigrationRun>(
                    config.SqlType switch
                    {
                        SqlType.MySql => "select * from `migration_runs` where filename = @migrationFilename",
                        _ => "select * from MigrationRuns where Filename = @migrationFilename"
                    },
                    new { migrationFilename = Path.GetFileName(migrationFilename) }
                );
            }

            if (run != null)
            {
                if (config.Verbose) Console.WriteLine("Info: found run in table!");
                if (crc == run.MD5)
                {
                    if (config.Verbose) Console.WriteLine("Info: same MD5, skipping");
                    return MigrationResult.Skipped;
                }

                if (!config.Force) return MigrationResult.Changed;

                if (config.Verbose) Console.WriteLine("Info: forced run");

                RunFile(sql, config);
                using var conn = CreateConnection(config);
                conn.Execute(config.SqlType switch
                {
                    SqlType.MySql => "update `migration_runs` set last_run = @LastRun, migration_result = @MigrationResult, md5 = @MD5 where filename = @Filename",
                    _ => "update MigrationRuns set LastRun = @LastRun, MigrationResult = @MigrationResult, MD5 = @MD5 where Filename = @Filename"

                }, new MigrationRun
                {
                    LastRun = DateTime.UtcNow,
                    Filename = Path.GetFileName(migrationFilename),
                    MD5 = crc,
                    MigrationResult = MigrationResult.Changed
                });
                if (config.Verbose) Console.WriteLine("Info: updating migration table");
                return MigrationResult.Changed;
            }
            if (config.Verbose) Console.WriteLine("Info: fresh migration!");
            RunFile(sql, config);
            using (var conn = CreateConnection(config))
            {
                if (config.Verbose) Console.WriteLine("Info: saving in migration table");
                conn.Execute(config.SqlType switch
                {
                    SqlType.MySql => "insert into `migration_runs` (last_run, migration_result, md5, filename) values (@LastRun, @MigrationResult, @MD5, @Filename)",
                    _ => "insert into MigrationRuns (LastRun, MigrationResult, MD5, Filename) values (@LastRun, @MigrationResult, @MD5, @Filename)"

                }, new MigrationRun
                {
                    LastRun = DateTime.UtcNow,
                    Filename = Path.GetFileName(migrationFilename),
                    MD5 = crc,
                    MigrationResult = MigrationResult.Run
                });
            }

            return MigrationResult.Run;
        }

        private static readonly Regex Splitter = new Regex(@"\nGO\s?\n", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly string InvalidCharFallback = ((DecoderReplacementFallback)Encoding.UTF8.DecoderFallback).DefaultString;

        public static void RunFile(string sql, Config config)
        {
            if (config.StrictEncoding)
            {
                var firstInvalidCharIndex = sql.IndexOf(InvalidCharFallback);
                if (firstInvalidCharIndex >= 0)
                {
                    var lineSeparators = new[] { '\n', '\r' };
                    var startOfLineIndex = sql.LastIndexOfAny(lineSeparators, firstInvalidCharIndex) + 1;
                    var endOfLineIndex = sql.IndexOfAny(lineSeparators, firstInvalidCharIndex);
                    if (endOfLineIndex < 0)
                    {
                        endOfLineIndex = sql.Length;
                    }

                    var line = sql.Substring(startOfLineIndex, endOfLineIndex - startOfLineIndex);

                    throw new InvalidDataException($"Found an invalid character at offset {firstInvalidCharIndex}:\n    | {line}\n    | {"^".PadLeft(firstInvalidCharIndex - startOfLineIndex + 1)}\nPlease make sure that the encoding of the file is correct (UTF-8 is recommended).\n");
                }
            }

            var parts = Splitter.Split(sql);
            if (config.Verbose) Console.WriteLine("Info: found {0} part(s)", parts.Length);

            var pnum = 0;
            foreach (var part in parts)
            {
                pnum++;
                if (config.Verbose) Console.WriteLine("Info: running part {0}", pnum);

                if (string.IsNullOrEmpty(part))
                {
                    if (config.Verbose) Console.WriteLine("Info: part is empty");
                    continue;
                }

                using var conn = CreateConnection(config);

                if (config.UseTransaction)
                {
                    if (config.Verbose) Console.WriteLine("Info: executing with transaction...");
                    var transaction = conn.BeginTransaction();
                    try
                    {
                        conn.Execute(part, transaction: transaction);
                    }
                    catch
                    {
                        if (config.Verbose) Console.WriteLine("Info: Error, rolling back!");
                        transaction.Rollback();
                        throw;
                    }
                    if (config.Verbose) Console.WriteLine("Info: ...done!");
                    transaction.Commit();
                }
                else
                {
                    if (config.Verbose) Console.WriteLine("Info: executing without transaction...");
                    conn.Execute(part);
                    if (config.Verbose) Console.WriteLine("Info: ...done!");
                }
            }
        }

    }
}
