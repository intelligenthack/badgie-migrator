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

        private static void Main(string[] args)
        {
            var time = new Stopwatch();
            time.Start();

            var _config = Config.FromArgs(args);
            if (_config == null)
            {
                Environment.Exit(-2);
            }

            if (_config.Configurations != null && _config.Configurations.Count>1)
            {
                foreach(var config in _config.Configurations)
                {
                    var time2 = new Stopwatch();
                    time2.Start();
                    Console.WriteLine("Migrating connection: \"{0}\"", config.ConnectionString);
                    if (!EnsureTableExists(config))
                    {
                        Error("Database does not have data table installed, did you forget to pass \"-i\"?");
                    }
                    if (!ExecuteFolder(config))
                    {
                        Error("Execution error");
                    }
                    time2.Stop();
                    Console.WriteLine("Migrations done in {0:0} ms", time2.Elapsed.TotalMilliseconds);

                }

            }
            else
            {
                if (!EnsureTableExists(_config))
                {
                    Error("Database does not have data table installed, did you forget to pass \"-i\"?");
                }

                if (!ExecuteFolder(_config))
                {
                    Error("Execution error");
                }
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

            if (!installed && config.Install)
            {
                using (var x = CreateConnection(config))
                {
                    x.Execute(TableCreationStatement(config));
                }

                installed = true;
            }
            return installed;
        }

        private static DbConnection CreateConnection(Config config)
        {
            switch (config.SqlType)
            {
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
                default:
                    throw new NotSupportedException();
            }
        }

        public static bool ExecuteFolder(Config config)
        {
            var path = config.Path;
            var info = new FileInfo(path);
            foreach (var file in Directory.EnumerateFiles(info.Directory.FullName, info.Name).OrderBy(f => f))
            {
                MigrationResult result;
                try
                {
                    result = ExecuteMigration(file, config);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                    return false;
                }
                Console.WriteLine("{0} - {1}", result, file);
                if (!config.Force && result == MigrationResult.Changed)
                {
                    return false;
                }
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
            }
            MigrationRun run;
            using (var conn = CreateConnection(config))
            {
                run = conn.QueryFirstOrDefault<MigrationRun>("select * from MigrationRuns where Filename = @migrationFilename", new { migrationFilename = Path.GetFileName(migrationFilename) });
            }

            if (run != null)
            {
                if (crc == run.MD5)
                {
                    return MigrationResult.Skipped;
                }

                if (config.Force)
                {
                    RunFile(sql, config);
                    using (var conn = CreateConnection(config))
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
            RunFile(sql, config);
            using (var conn = CreateConnection(config))
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

        public static void RunFile(string sql, Config config)
        {
            foreach (var part in Splitter.Split(sql))
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }
                try
                {

                    using (var conn = CreateConnection(config))
                    {
                        if (config.UseTransaction)
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
                catch(Exception ex)
                {
                    throw new ApplicationException(String.Format("Error at:\n{0}", part), ex);
                }
            }
        }

    }
}
