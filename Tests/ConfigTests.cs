using NUnit.Framework;

namespace Badgie.Migrator.Tests
{
    public class ConfigTests
    {
        [Test]
        public void NullGivesError()
        {
            var config = Config.FromArgs(null);
            Assert.IsNull(config);
        }

        [Test]
        public void EmptyGivesError()
        {
            var config = Config.FromArgs(new string[] { });
            Assert.IsNull(config);
        }

        [TestCase("")]
        [TestCase("  ")]
        public void BlankGivesError(string arg)
        {
            var args = new string[] { arg };
            var config = Config.FromArgs(args);
            Assert.IsNull(config);
        }

        [TestCase("Server=127.0.0.1;Port=5432;Database=myDataBase;User Id=myUsername;Password = myPassword;")]
        [TestCase("Server=127.0.0.1;Port=5432;Database=myDataBase;Integrated Security=true;")]
        [TestCase("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password = myPassword;")]
        [TestCase("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;")]
        [TestCase("Server=myServerName\\myInstanceName;Database=myDataBase;User Id=myUsername;Password = myPassword;")]
        [TestCase("Server=(localdb)\\MyInstance;Integrated Security=true;")]
        [TestCase("-a")]
        [TestCase("--force")]
        public void ValidConnections(string arg)
        {
            var args = new string[] { arg };
            var config = Config.FromArgs(args);
            Assert.IsNotNull(config);
            Assert.AreEqual(arg, config.ConnectionString);
            Assert.AreEqual(false, config.Verbose);
            Assert.AreEqual(true, config.StackTraces);
        }

        [TestCase("path", "-i", "-f", "-d:SqlServer")]
        [TestCase("path", "-i", "-d:SqlServer", "-f")]
        [TestCase("path", "-f", "-i", "-d:SqlServer")]
        [TestCase("path", "-d:SqlServer", "-i", "-f")]
        [TestCase("path", "-f", "-d:SqlServer", "-i")]
        [TestCase("path", "-d:SqlServer", "-f", "-i")]
        [TestCase("-i", "path", "-f", "-d:SqlServer")]
        [TestCase("-i", "path", "-d:SqlServer", "-f")]
        [TestCase("-f", "path", "-i", "-d:SqlServer")]
        [TestCase("-d:SqlServer", "path", "-i", "-f")]
        [TestCase("-f", "path", "-d:SqlServer", "-i")]
        [TestCase("-d:SqlServer", "path", "-f", "-i")]
        [TestCase("-i", "-f", "path", "-d:SqlServer")]
        [TestCase("-i", "-d:SqlServer", "path", "-f")]
        [TestCase("-f", "-i", "path", "-d:SqlServer")]
        [TestCase("-d:SqlServer", "-i", "path", "-f")]
        [TestCase("-f", "-d:SqlServer", "path", "-i")]
        [TestCase("-d:SqlServer", "-f", "path", "-i")]
        [TestCase("-i", "-f", "-d:SqlServer", "path")]
        [TestCase("-i", "-d:SqlServer", "-f", "path")]
        [TestCase("-f", "-i", "-d:SqlServer", "path")]
        [TestCase("-d:SqlServer", "-i", "-f", "path")]
        [TestCase("-f", "-d:SqlServer", "-i", "path")]
        [TestCase("-d:SqlServer", "-f", "-i", "path")]
        public void FourParams(string arg1, string arg2, string arg3, string arg4)
        {
            var args = new string[] { "connection", arg1, arg2, arg3, arg4 };
            var config = Config.FromArgs(args);
            Assert.IsNotNull(config);
            Assert.AreEqual(args[0], config.ConnectionString);
            Assert.AreEqual(true, config.Install);
            Assert.AreEqual(true, config.Force);
            Assert.AreEqual(SqlType.SqlServer, config.SqlType);
            Assert.AreEqual("path", config.Path);
            Assert.AreEqual(false, config.Verbose);
            Assert.AreEqual(true, config.StackTraces);
        }

        [TestCase("path", "-i", "-f", "-d:SqlServer", "-n")]
        [TestCase("path", "-i", "-d:SqlServer", "-n", "-f")]
        [TestCase("path", "-f", "-i", "-n", "-d:SqlServer")]
        [TestCase("path", "-d:SqlServer", "-n", "-i", "-f")]
        [TestCase("path", "-f", "-d:SqlServer", "-i", "-n")]
        [TestCase("path", "-d:SqlServer", "-n", "-f", "-i")]
        [TestCase("-i", "-n", "path", "-f", "-d:SqlServer")]
        [TestCase("-n", "-d:SqlServer", "-i", "path", "-f")]
        public void FiveParams(string arg1, string arg2, string arg3, string arg4, string arg5)
        {
            var args = new string[] { "connection", arg1, arg2, arg3, arg4, arg5 };
            var config = Config.FromArgs(args);
            Assert.IsNotNull(config);
            Assert.AreEqual(args[0], config.ConnectionString);
            Assert.AreEqual(true, config.Install);
            Assert.AreEqual(true, config.Force);
            Assert.AreEqual(SqlType.SqlServer, config.SqlType);
            Assert.AreEqual("path", config.Path);
            Assert.AreEqual(false, config.UseTransaction);
            Assert.AreEqual(false, config.Verbose);
            Assert.AreEqual(true, config.StackTraces);
        }

        [TestCase("path", "-i", "-f", "-d:SqlServer", "-n", "-V")]
        [TestCase("path", "-i", "-d:SqlServer", "-n", "-V", "-f")]
        [TestCase("path", "-f", "-i", "-V", "-n", "-d:SqlServer")]
        [TestCase("path", "-d:SqlServer", "-V", "-n", "-i", "-f")]
        [TestCase("-V", "path", "-f", "-d:SqlServer", "-i", "-n")]
        [TestCase("path", "-d:SqlServer", "-V", "-n", "-f", "-i")]
        public void SixParams(string arg1, string arg2, string arg3, string arg4, string arg5, string arg6)
        {
            var args = new string[] { "connection", arg1, arg2, arg3, arg4, arg5, arg6 };
            var config = Config.FromArgs(args);
            Assert.IsNotNull(config);
            Assert.AreEqual(args[0], config.ConnectionString);
            Assert.AreEqual(true, config.Install);
            Assert.AreEqual(true, config.Force);
            Assert.AreEqual(SqlType.SqlServer, config.SqlType);
            Assert.AreEqual("path", config.Path);
            Assert.AreEqual(false, config.UseTransaction);
            Assert.AreEqual(true, config.Verbose);
            Assert.AreEqual(true, config.StackTraces);
        }

        [TestCase("path", "-i", "--no-stack-trace", "-f", "-d:SqlServer", "-n", "-V")]
        [TestCase("path", "-i", "-d:SqlServer", "--no-stack-trace", "-n", "-V", "-f")]
        [TestCase("path", "-f", "-i", "-V", "-n", "-d:SqlServer", "--no-stack-trace")]
        [TestCase("path", "-d:SqlServer", "--no-stack-trace", "-V", "-n", "-i", "-f")]
        [TestCase("-V", "--no-stack-trace", "path", "-f", "-d:SqlServer", "-i", "-n")]
        [TestCase("path", "-d:SqlServer", "--no-stack-trace", "-V", "-n", "-f", "-i")]
        public void SevenParams(string arg1, string arg2, string arg3, string arg4, string arg5, string arg6, string arg7)
        {
            var args = new string[] { "connection", arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
            var config = Config.FromArgs(args);
            Assert.IsNotNull(config);
            Assert.AreEqual(args[0], config.ConnectionString);
            Assert.AreEqual(true, config.Install);
            Assert.AreEqual(true, config.Force);
            Assert.AreEqual(SqlType.SqlServer, config.SqlType);
            Assert.AreEqual("path", config.Path);
            Assert.AreEqual(false, config.UseTransaction);
            Assert.AreEqual(true, config.Verbose);
            Assert.AreEqual(false, config.StackTraces);
        }

        [TestCase("-json=foo", "", "foo")]
        [TestCase("-json=\"foo\"", "", "\"foo\"")]
        [TestCase("-json=", "foo", "foo")]
        [TestCase("-json=", "\"foo\"", "\"foo\"")]
        public void JsonParam(string arg1, string arg2, string expected)
        {
            var args = new string[] { arg1, arg2 };

            var filename = "";
            Config.FileLoader = (x) =>
              {
                  filename = x;
                  return "[]";
              };
            var config = Config.FromArgs(args);
            Assert.AreEqual(expected, filename);
        }

        [Test]
        public void ConfigJson()
        {
            var json = @"[
                          {
                            ""ConnectionString"": ""Connection 1"",
                            ""Force"": true,
                            ""Install"": true,
                            ""SqlType"": ""SqlServer"",
                            ""Path"": ""Path 1"",
                            ""UseTransaction"": true
                          },                      
                          {
                            ""ConnectionString"": ""Connection 2"",
                            ""Force"": false,
                            ""Install"": false,
                            ""SqlType"": ""Postgres"",
                            ""Path"": ""Path 2"",
                            ""UseTransaction"": false
                          },                      
                          {
                            ""ConnectionString"": ""Connection 3"",
                            ""Force"": false,
                            ""Install"": false,
                            ""SqlType"": ""MySql"",
                            ""Path"": ""Path 3"",
                            ""UseTransaction"": false
                          }
                        ]";
            var config = Config.FromJson(json);

            AssertConfig(config, "Connection 1", true, true, SqlType.SqlServer, true, "Path 1");
            Assert.AreEqual(3, config.Configurations.Count);
            AssertConfig(config.Configurations[0], "Connection 1", true, true, SqlType.SqlServer, true, "Path 1");
            AssertConfig(config.Configurations[1], "Connection 2", false, false, SqlType.Postgres, false, "Path 2");
            AssertConfig(config.Configurations[2], "Connection 3", false, false, SqlType.MySql, false, "Path 3");

        }

        public void AssertConfig(Config config, string connection, bool force, bool install, SqlType sqlType, bool useTransaction, string path)
        {
            Assert.IsNotNull(config);
            Assert.AreEqual(connection, config.ConnectionString);
            Assert.AreEqual(force, config.Force);
            Assert.AreEqual(install, config.Install);
            Assert.AreEqual(sqlType, config.SqlType);
            Assert.AreEqual(useTransaction, config.UseTransaction);
            Assert.AreEqual(path, config.Path);
        }


        [TestCase("path")]
        [TestCase("C:\\foo")]
        [TestCase("C:\\foo\\*.sql")]
        [TestCase(".\\foo\\*.sql")]
        [TestCase("C:\\Program Files\\")]
        [TestCase("/home/john/src/*.sql")]
        public void ValidPaths(string arg)
        {
            var args = new string[] { "connection", arg };
            var config = Config.FromArgs(args);
            Assert.IsNotNull(config);
            Assert.AreEqual(arg, config.Path);
        }
    }
}