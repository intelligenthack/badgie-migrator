using System;
using Dapper;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Data.Common;
using Npgsql;
using System.Diagnostics;

namespace Badgie.Migrator
{
    class Program
    {
        static string _connectionString;
        static bool _force = false;
        private static bool _install = false;
        static void Main(string[] args)
        {
            var time = new Stopwatch();
            time.Start();
            var path = ".";
            switch (args.Length)
            {
                case 0:
                default:
                    Console.Error.WriteLine(@"Usage: badgie-migrator <connection string> [drive:][path][filename] [-f] [-i]
-f runs mutated migrations
-i if needed, installs the db table needed to store state");
                    Console.Beep();
                    Environment.Exit(-2);
                    return;
                case 1:
                    _connectionString = args[0];
                    break;
                case 2:
                    _connectionString = args[0];
                    if (args[1] == "-f") 
                        _force = true;
                    else if (args[1] == "-i") 
                        _install = true;
                    else
                        path = args[1];
                    break;
                case 3:
                    _connectionString = args[0];
                    path = args[1];
                    if (args[2] == "-f") 
                        _force = true;
                    else if (args[2] == "-i") 
                        _install = true;
                    else if (args[2] == "-fi" || args[2] == "-if") 
                    {
                        _force = true;
                        _install = true;
                    }
                    break;
                case 4:
                    _connectionString = args[0];
                    path = args[1];
                    if (args[2] == "-f" && args[3] == "-i" || args[2] == "-i" && args[3] == "-f") 
                    {
                        _force = true;
                        _install = true;
                    }
                    break;
            }
            
            if (!EnsureTableExists()) Error("Database does not have data table installed, did you forget to pass \"-i\"?");
            if (!ExecuteFolder(path)) Error("Execution error");
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
            using (var x = new NpgsqlConnection(_connectionString))
             installed = x.GetSchema("Tables", new string[] {null, "public", "migrationruns", null}).Rows.Count > 0;
            if (!installed && _install)
            {
                using (var x = new NpgsqlConnection(_connectionString))
                    x.Execute(@"
CREATE SEQUENCE MigrationRuns_Id_seq INCREMENT 1 MINVALUE 1 MAXVALUE 2147483647 START 1 CACHE 1;
CREATE TABLE ""public"".MigrationRuns (
    Id integer DEFAULT nextval('MigrationRuns_Id_seq') NOT NULL,
    LastRun timestamp  NOT NULL,
    Filename character varying(2000) NOT NULL,
    MD5 character varying(50) NOT NULL,
    MigrationResult integer NOT NULL,
    CONSTRAINT ""MigrationRuns_Id"" PRIMARY KEY (Id)
) WITH (oids = false);");
                installed = true;
            }
            return installed;
        }

        public static bool ExecuteFolder(string path)
        {
            var info = new FileInfo(path);
            foreach(var file in Directory.EnumerateFiles(info.Directory.FullName, info.Name).OrderBy(f => f))
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
            public MigrationResult MigrationResult {get; set;}
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
            using (var conn = new NpgsqlConnection(_connectionString))
                run = conn.QueryFirstOrDefault<MigrationRun>("select * from public.MigrationRuns where Filename = @migrationFilename", new { migrationFilename });
            if (run != null)
            {
                if (crc == run.MD5) return MigrationResult.Skipped;
                if (_force) 
                {
                    RunFile(sql);
                    using (var conn = new NpgsqlConnection(_connectionString))
                        conn.Execute("update public.MigrationRuns set LastRun = @LastRun, MigrationResult = @MigrationResult, MD5 = @MD5 where Filename = @Filename", new MigrationRun
                        {
                            LastRun = DateTime.UtcNow,
                            Filename = migrationFilename,
                            MD5 = crc,
                            MigrationResult = MigrationResult.Changed
                        });
                }
                return MigrationResult.Changed;
            }
            RunFile(sql);
            using (var conn = new NpgsqlConnection(_connectionString))
                conn.Execute("insert into public.MigrationRuns (LastRun, MigrationResult, MD5, Filename) values (@LastRun, @MigrationResult, @MD5, @Filename)", new MigrationRun
                {
                    LastRun = DateTime.UtcNow,
                    Filename = migrationFilename,
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
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
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
            }
        }

    }
}
