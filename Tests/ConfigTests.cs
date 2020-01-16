using NUnit.Framework;
using Badgie.Migrator;

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