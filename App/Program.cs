using Dapper;
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
        private static Config _config;

        private static void Main(string[] args)
        {
            var time = new Stopwatch();
            time.Start();

            _config = Config.FromArgs(args);
            if (_config == null)
            {
                Environment.Exit(-2);
            }

            if (!EnsureTableExists())
            {
                Error("Database does not have data table installed, did you forget to pass \"-i\"?");
            }

            if (!ExecuteFolder(_config.Path))
            {
                Error("Execution error");
            }

            time.Stop();
            Console.WriteLine("All done in {0:0} ms", time.Elapsed.TotalMilliseconds);
        }

        public static void Error(string error)
        {
            Console.Error.WriteLine(error);
            System.Environment.Exit(-1);
        }

        public static bool EnsureTableExists()
        {
            bool installed;
            using (var x = CreateConnection())
            {
                switch (_config.SqlType)
                {
                    case SqlType.Postgres:
                        installed = x.GetSchema("Tables", new string[] { null, "public", "migrationruns", null }).Rows.Count > 0;
                        break;

                    case SqlType.SqlServer:
                        installed = x.GetSchema("Tables", new string[] { null, "dbo", "MigrationRuns", null }).Rows.Count > 0;
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            if (!installed && _config.Install)
            {
                using (var x = CreateConnection())
                {
                    x.Execute(TableCreationStatement());
                }

                installed = true;
            }
            return installed;
        }

        private static DbConnection CreateConnection()
        {
            switch (_config.SqlType)
            {
                case SqlType.Postgres:
                    {
                        var conn = new NpgsqlConnection(_config.ConnectionString);
                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Open();
                        }

                        return conn;
                    }
                case SqlType.SqlServer:
                    {
                        var conn = new SqlConnection(_config.ConnectionString);
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


        private static string TableCreationStatement()
        {
            switch (_config.SqlType)
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
                default:
                    throw new NotSupportedException();
            }
        }

        public static bool ExecuteFolder(string path)
        {
            var info = new FileInfo(path);
            foreach (var file in Directory.EnumerateFiles(info.Directory.FullName, info.Name).OrderBy(f => f))
            {
                MigrationResult result;
                try
                {
                    result = ExecuteMigration(file);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                    return false;
                }
                Console.WriteLine("{0} - {1}", result, file);
                if (!_config.Force && result == MigrationResult.Changed)
                {
                    return false;
                }
            }
            return true;
        }

        public static MigrationResult ExecuteMigration(string migrationFilename)
        {
            var sql = File.ReadAllText(migrationFilename);
            string crc;
            using (var md5 = MD5.Create())
            {
                crc = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(sql)));
            }
            MigrationRun run;
            using (var conn = CreateConnection())
            {
                run = conn.QueryFirstOrDefault<MigrationRun>("select * from MigrationRuns where Filename = @migrationFilename", new { migrationFilename = Path.GetFileName(migrationFilename) });
            }

            if (run != null)
            {
                if (crc == run.MD5)
                {
                    return MigrationResult.Skipped;
                }

                if (_config.Force)
                {
                    RunFile(sql);
                    using (var conn = CreateConnection())
                    {
                        conn.Execute("update MigrationRuns set LastRun = @LastRun, MigrationResult = @MigrationResult, MD5 = @MD5 where Filename = @Filename", new MigrationRun
                        {
                            LastRun = DateTime.UtcNow,
                            Filename = Path.GetFileName(migrationFilename),
                            MD5 = crc,
                            MigrationResult = MigrationResult.Changed
                        });
                    }
                }
                return MigrationResult.Changed;
            }
            RunFile(sql);
            using (var conn = CreateConnection())
            {
                conn.Execute("insert into MigrationRuns (LastRun, MigrationResult, MD5, Filename) values (@LastRun, @MigrationResult, @MD5, @Filename)", new MigrationRun
                {
                    LastRun = DateTime.UtcNow,
                    Filename = Path.GetFileName(migrationFilename),
                    MD5 = crc,
                    MigrationResult = MigrationResult.Run
                });
            }

            return MigrationResult.Run;
        }

        private static Regex Splitter = new Regex(@"\nGO\s?\n", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static void RunFile(string sql)
        {
            foreach (var part in Splitter.Split(sql))
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }

                using (var conn = CreateConnection())
                {
                    if (_config.UseTransaction)
                    {
                        var transaction = conn.BeginTransaction();
                        try
                        {
                            conn.Execute(part, transaction: transaction);
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                        transaction.Commit();
                    }
                    else
                    {
                        conn.Execute(part);
                    }
                }
            }
        }

    }
}
