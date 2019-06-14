using System;
using System.Data;
using Dapper;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Data.Common;
using Npgsql;
using System.Diagnostics;
using System.Data.SqlClient;

namespace Badgie.Migrator
{
    enum SqlType
    {
        Postgres,
        SqlServer
    }

    class Program
    {
        private static string _connectionString;
        private static bool _force = false;
        private static bool _install = false;
        private static SqlType _sqltype = SqlType.Postgres;
        private static string path = ".";

        static void Main(string[] args)
        {
            var time = new Stopwatch();
            time.Start();

            if (args.Length == 0)
            {
                Console.Error.WriteLine(@"Usage: dotnet-badgie-migrator <connection string> [drive:][path][filename] [-d:(SqlServer|Postgres)] [-f] [-i] [-d]
-f                      runs mutated migrations
-i                      if needed, installs the db table needed to store state
-d:(SqlServer|Postgres) specifies whether to run against SQL Server or PostgreSQL");
                Console.Beep();
                Environment.Exit(-2);
            }

            _connectionString = args[0];

            if (args.Length > 1)
            {
                var pars = new String[args.Length - 1];
                Array.Copy(args, 1, pars, 0, args.Length-1);
                ParseParameters(pars);
            }


            if (!EnsureTableExists()) Error("Database does not have data table installed, did you forget to pass \"-i\"?");
            if (!ExecuteFolder(path)) Error("Execution error");
            time.Stop();
            Console.WriteLine("All done in {0:0} ms", time.Elapsed.TotalMilliseconds);
        }

        private static void ParseParameters(String[] pars)
        {
            foreach (var str in pars)
            {
                switch (str.Substring(0, 2))
                {
                    case "-f":
                        _force = true;
                        break;

                    case "-i":
                        _install = true;
                        break;

                    case "-d":
                        _sqltype = CheckDbType(str);
                        break;

                    default:
                        path = str;
                        break;
                }
            }
        }


        private static SqlType CheckDbType(String str)
        {
            if (str.Length < 4) Error("Database type unspecified");
            var strType = str.Substring(3, str.Length - 3);
            if (!Enum.TryParse<SqlType>(strType, out var sqlType)) Error($"Unrecognized database type {strType}");
            return sqlType;
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
                switch (_sqltype)
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

            if (!installed && _install)
            {
                using (var x = CreateConnection())
                    x.Execute(TableCreationStatement());
                installed = true;
            }
            return installed;
        }

        private static DbConnection CreateConnection()
        {
            switch (_sqltype)
            {
                case SqlType.Postgres:
                    var pgconn = new NpgsqlConnection(_connectionString);
                    if (pgconn.State != ConnectionState.Open)
                        pgconn.Open();
                    return pgconn;
                case SqlType.SqlServer:
                    var msconn =  new SqlConnection(_connectionString);
                    if (msconn.State == ConnectionState.Broken || msconn.State == ConnectionState.Closed)
                        msconn.Open();
                    return msconn;
                default:
                    throw new NotSupportedException();
            }
        }


        private static string TableCreationStatement()
        {
            switch (_sqltype)
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
                if (!_force && result == MigrationResult.Changed) return false;
            }
            return true;
        }

        public class MigrationRun
        {
            public int Id { get; set; }
            public DateTime LastRun { get; set; }
            public string Filename { get; set; }
            public string MD5 { get; set; }
            public MigrationResult MigrationResult { get; set; }
        }

        public enum MigrationResult
        {
            Run,
            Skipped,
            Changed
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
                run = conn.QueryFirstOrDefault<MigrationRun>("select * from MigrationRuns where Filename = @migrationFilename", new { migrationFilename = Path.GetFileName(migrationFilename) });
            if (run != null)
            {
                if (crc == run.MD5) return MigrationResult.Skipped;
                if (_force)
                {
                    RunFile(sql);
                    using (var conn = CreateConnection())
                        conn.Execute("update MigrationRuns set LastRun = @LastRun, MigrationResult = @MigrationResult, MD5 = @MD5 where Filename = @Filename", new MigrationRun
                        {
                            LastRun = DateTime.UtcNow,
                            Filename = Path.GetFileName(migrationFilename),
                            MD5 = crc,
                            MigrationResult = MigrationResult.Changed
                        });
                }
                return MigrationResult.Changed;
            }
            RunFile(sql);
            using (var conn = CreateConnection())
                conn.Execute("insert into MigrationRuns (LastRun, MigrationResult, MD5, Filename) values (@LastRun, @MigrationResult, @MD5, @Filename)", new MigrationRun
                {
                    LastRun = DateTime.UtcNow,
                    Filename = Path.GetFileName(migrationFilename),
                    MD5 = crc,
                    MigrationResult = MigrationResult.Run
                });
            return MigrationResult.Run;
        }

        static Regex Splitter = new Regex(@"\nGO\s?\n", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static void RunFile(string sql)
        {
            foreach (var part in Splitter.Split(sql))
            {
                if (String.IsNullOrEmpty(part)) continue;
                using (var conn = CreateConnection())
                {
                    var transaction = conn.BeginTransaction();
                    try
                    {
                        conn.Execute(part, transaction: transaction);
                    }
                    catch
                    {
                        transaction.Rollback();
                        Debug.WriteLine("ERROR HERE");
                        Debug.WriteLine(part);
                        throw;
                    }
                    transaction.Commit();
                }
            }
        }

    }
}
