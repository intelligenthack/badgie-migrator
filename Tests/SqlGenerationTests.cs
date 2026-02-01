using NUnit.Framework;
using System.Reflection;

namespace Badgie.Migrator.Tests
{
    public class SqlGenerationTests
    {
        private static string GetTableCreationStatement(Config config)
        {
            var method = typeof(Program).GetMethod("TableCreationStatement", BindingFlags.NonPublic | BindingFlags.Static);
            return (string)method.Invoke(null, new object[] { config });
        }

        [Test]
        public void SqlServer_TableCreationStatement_IsValid()
        {
            var config = new Config { SqlType = SqlType.SqlServer };
            var sql = GetTableCreationStatement(config);

            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("CREATE TABLE [dbo].[MigrationRuns]"));
            Assert.IsTrue(sql.Contains("INT"));
            Assert.IsTrue(sql.Contains("IDENTITY"));
            Assert.IsTrue(sql.Contains("DATETIME"));
            Assert.IsTrue(sql.Contains("NVARCHAR(2000)"));
            Assert.IsTrue(sql.Contains("VARCHAR(50)"));
            Assert.IsTrue(sql.Contains("TINYINT"));
            Assert.IsTrue(sql.Contains("PRIMARY KEY"));
        }

        [Test]
        public void Postgres_TableCreationStatement_IsValid()
        {
            var config = new Config { SqlType = SqlType.Postgres };
            var sql = GetTableCreationStatement(config);

            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("CREATE SEQUENCE"));
            Assert.IsTrue(sql.Contains("CREATE TABLE \"public\".MigrationRuns"));
            Assert.IsTrue(sql.Contains("integer"));
            Assert.IsTrue(sql.Contains("timestamp"));
            Assert.IsTrue(sql.Contains("character varying(2000)"));
            Assert.IsTrue(sql.Contains("character varying(50)"));
            Assert.IsTrue(sql.Contains("PRIMARY KEY"));
        }

        [Test]
        public void MySql_TableCreationStatement_IsValid()
        {
            var config = new Config { SqlType = SqlType.MySql };
            var sql = GetTableCreationStatement(config);

            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("CREATE TABLE `migration_runs`"));
            Assert.IsTrue(sql.Contains("int NOT NULL AUTO_INCREMENT"));
            Assert.IsTrue(sql.Contains("datetime"));
            Assert.IsTrue(sql.Contains("text"));
            Assert.IsTrue(sql.Contains("varchar(50)"));
            Assert.IsTrue(sql.Contains("tinyint"));
            Assert.IsTrue(sql.Contains("PRIMARY KEY"));
            Assert.IsTrue(sql.Contains("ENGINE=InnoDB"));
        }

        [Test]
        public void SqlServer_UsesCorrectColumnNames()
        {
            var config = new Config { SqlType = SqlType.SqlServer };
            var sql = GetTableCreationStatement(config);

            Assert.IsTrue(sql.Contains("Id"));
            Assert.IsTrue(sql.Contains("LastRun"));
            Assert.IsTrue(sql.Contains("Filename"));
            Assert.IsTrue(sql.Contains("MD5"));
            Assert.IsTrue(sql.Contains("MigrationResult"));
        }

        [Test]
        public void Postgres_UsesCorrectColumnNames()
        {
            var config = new Config { SqlType = SqlType.Postgres };
            var sql = GetTableCreationStatement(config);

            Assert.IsTrue(sql.Contains("Id"));
            Assert.IsTrue(sql.Contains("LastRun"));
            Assert.IsTrue(sql.Contains("Filename"));
            Assert.IsTrue(sql.Contains("MD5"));
            Assert.IsTrue(sql.Contains("MigrationResult"));
        }

        [Test]
        public void MySql_UsesSnakeCaseColumnNames()
        {
            var config = new Config { SqlType = SqlType.MySql };
            var sql = GetTableCreationStatement(config);

            Assert.IsTrue(sql.Contains("`id`"));
            Assert.IsTrue(sql.Contains("`last_run`"));
            Assert.IsTrue(sql.Contains("`filename`"));
            Assert.IsTrue(sql.Contains("`md5`"));
            Assert.IsTrue(sql.Contains("`migration_result`"));
        }

        [Test]
        public void SQLite_TableCreationStatement_IsValid()
        {
            var config = new Config { SqlType = SqlType.SQLite };
            var sql = GetTableCreationStatement(config);

            Assert.IsNotNull(sql);
            Assert.IsTrue(sql.Contains("CREATE TABLE MigrationRuns"));
            Assert.IsTrue(sql.Contains("INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT"));
            Assert.IsTrue(sql.Contains("TEXT NOT NULL"));
            Assert.IsTrue(sql.Contains("INTEGER NOT NULL"));
        }

        [Test]
        public void SQLite_UsesCorrectColumnNames()
        {
            var config = new Config { SqlType = SqlType.SQLite };
            var sql = GetTableCreationStatement(config);

            Assert.IsTrue(sql.Contains("Id"));
            Assert.IsTrue(sql.Contains("LastRun"));
            Assert.IsTrue(sql.Contains("Filename"));
            Assert.IsTrue(sql.Contains("MD5"));
            Assert.IsTrue(sql.Contains("MigrationResult"));
        }

        [Test]
        public void AllDatabases_HaveMatchingColumnCount()
        {
            // Each database should define exactly 5 columns
            var expectedColumns = 5;

            var sqlServerSql = GetTableCreationStatement(new Config { SqlType = SqlType.SqlServer });
            var postgresSql = GetTableCreationStatement(new Config { SqlType = SqlType.Postgres });
            var mySqlSql = GetTableCreationStatement(new Config { SqlType = SqlType.MySql });
            var sqliteSql = GetTableCreationStatement(new Config { SqlType = SqlType.SQLite });

            // Count column definitions (rough check via NOT NULL occurrences)
            var sqlServerColumns = System.Text.RegularExpressions.Regex.Matches(sqlServerSql, @"NOT NULL").Count;
            var postgresColumns = System.Text.RegularExpressions.Regex.Matches(postgresSql, @"NOT NULL").Count;
            var mySqlColumns = System.Text.RegularExpressions.Regex.Matches(mySqlSql, @"NOT NULL").Count;
            var sqliteColumns = System.Text.RegularExpressions.Regex.Matches(sqliteSql, @"NOT NULL").Count;

            Assert.AreEqual(expectedColumns, sqlServerColumns, "SQL Server should have 5 columns");
            Assert.AreEqual(expectedColumns, postgresColumns, "Postgres should have 5 columns");
            Assert.AreEqual(expectedColumns, mySqlColumns, "MySQL should have 5 columns");
            Assert.AreEqual(expectedColumns, sqliteColumns, "SQLite should have 5 columns");
        }
    }
}
