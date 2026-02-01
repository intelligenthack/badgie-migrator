using NUnit.Framework;
using System;

namespace Badgie.Migrator.Tests
{
    public class MigrationModelTests
    {
        [Test]
        public void MigrationResult_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)MigrationResult.Run);
            Assert.AreEqual(1, (int)MigrationResult.Skipped);
            Assert.AreEqual(2, (int)MigrationResult.Changed);
        }

        [Test]
        public void MigrationResult_CanBeCastToTinyInt()
        {
            // Verify values fit in TINYINT (0-255)
            foreach (MigrationResult result in Enum.GetValues(typeof(MigrationResult)))
            {
                var value = (int)result;
                Assert.IsTrue(value >= 0 && value <= 255, $"{result} value {value} should fit in TINYINT");
            }
        }

        [Test]
        public void MigrationRun_CanBeInstantiated()
        {
            var run = new MigrationRun
            {
                Id = 1,
                LastRun = DateTime.UtcNow,
                Filename = "001_test.sql",
                MD5 = "abc123==",
                MigrationResult = MigrationResult.Run
            };

            Assert.AreEqual(1, run.Id);
            Assert.AreEqual("001_test.sql", run.Filename);
            Assert.AreEqual("abc123==", run.MD5);
            Assert.AreEqual(MigrationResult.Run, run.MigrationResult);
        }

        [Test]
        public void MigrationRun_LastRunCanBeUtc()
        {
            var utcNow = DateTime.UtcNow;
            var run = new MigrationRun { LastRun = utcNow };

            Assert.AreEqual(utcNow, run.LastRun);
        }

        [Test]
        public void MigrationRun_FilenameCanContainPath()
        {
            var run = new MigrationRun { Filename = "migrations/001_test.sql" };

            Assert.AreEqual("migrations/001_test.sql", run.Filename);
        }

        [Test]
        public void MigrationRun_Md5IsBase64String()
        {
            var run = new MigrationRun { MD5 = "1B2M2Y8AsgTpgAmY7PhCfg==" }; // MD5 of empty string

            Assert.DoesNotThrow(() => Convert.FromBase64String(run.MD5));
        }
    }
}
