using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Badgie.Migrator.Tests
{
    public class FileEnumerationTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"badgie-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void FilesAreEnumeratedInAlphabeticalOrder()
        {
            // Create files out of order
            File.WriteAllText(Path.Combine(_tempDir, "003_third.sql"), "SELECT 3");
            File.WriteAllText(Path.Combine(_tempDir, "001_first.sql"), "SELECT 1");
            File.WriteAllText(Path.Combine(_tempDir, "002_second.sql"), "SELECT 2");

            var pattern = "*.sql";
            var files = Directory.EnumerateFiles(_tempDir, pattern).OrderBy(f => f).ToList();

            Assert.AreEqual(3, files.Count);
            Assert.IsTrue(files[0].EndsWith("001_first.sql"));
            Assert.IsTrue(files[1].EndsWith("002_second.sql"));
            Assert.IsTrue(files[2].EndsWith("003_third.sql"));
        }

        [Test]
        public void WildcardPatternMatchesCorrectFiles()
        {
            File.WriteAllText(Path.Combine(_tempDir, "migration.sql"), "SELECT 1");
            File.WriteAllText(Path.Combine(_tempDir, "migration.txt"), "not sql");
            File.WriteAllText(Path.Combine(_tempDir, "other.sql"), "SELECT 2");

            var files = Directory.EnumerateFiles(_tempDir, "*.sql").ToList();

            Assert.AreEqual(2, files.Count);
            Assert.IsTrue(files.All(f => f.EndsWith(".sql")));
        }

        [Test]
        public void SpecificPatternMatchesSubset()
        {
            File.WriteAllText(Path.Combine(_tempDir, "v1_001.sql"), "SELECT 1");
            File.WriteAllText(Path.Combine(_tempDir, "v1_002.sql"), "SELECT 2");
            File.WriteAllText(Path.Combine(_tempDir, "v2_001.sql"), "SELECT 3");

            var files = Directory.EnumerateFiles(_tempDir, "v1_*.sql").OrderBy(f => f).ToList();

            Assert.AreEqual(2, files.Count);
            Assert.IsTrue(files.All(f => Path.GetFileName(f).StartsWith("v1_")));
        }

        [Test]
        public void EmptyDirectoryReturnsNoFiles()
        {
            var files = Directory.EnumerateFiles(_tempDir, "*.sql").ToList();

            Assert.AreEqual(0, files.Count);
        }

        [Test]
        public void NumericPrefixSortingWorksCorrectly()
        {
            // Test that string sorting handles numeric prefixes correctly
            File.WriteAllText(Path.Combine(_tempDir, "1_a.sql"), "");
            File.WriteAllText(Path.Combine(_tempDir, "2_b.sql"), "");
            File.WriteAllText(Path.Combine(_tempDir, "10_c.sql"), "");

            var files = Directory.EnumerateFiles(_tempDir, "*.sql").OrderBy(f => f).ToList();

            // String sorting: 1, 10, 2 (not numeric order!)
            Assert.IsTrue(Path.GetFileName(files[0]).StartsWith("1_"));
            Assert.IsTrue(Path.GetFileName(files[1]).StartsWith("10_")); // 10 comes before 2 alphabetically
            Assert.IsTrue(Path.GetFileName(files[2]).StartsWith("2_"));
        }

        [Test]
        public void ZeroPaddedPrefixSortingWorksCorrectly()
        {
            // This is the recommended naming convention
            File.WriteAllText(Path.Combine(_tempDir, "001_a.sql"), "");
            File.WriteAllText(Path.Combine(_tempDir, "002_b.sql"), "");
            File.WriteAllText(Path.Combine(_tempDir, "010_c.sql"), "");

            var files = Directory.EnumerateFiles(_tempDir, "*.sql").OrderBy(f => f).ToList();

            // With zero-padding, sorting is correct
            Assert.IsTrue(Path.GetFileName(files[0]).StartsWith("001_"));
            Assert.IsTrue(Path.GetFileName(files[1]).StartsWith("002_"));
            Assert.IsTrue(Path.GetFileName(files[2]).StartsWith("010_"));
        }

        [Test]
        public void TimestampPrefixSortingWorksCorrectly()
        {
            // Another common convention: YYYYMMDDHHMMSS
            File.WriteAllText(Path.Combine(_tempDir, "20240101120000_first.sql"), "");
            File.WriteAllText(Path.Combine(_tempDir, "20240102120000_second.sql"), "");
            File.WriteAllText(Path.Combine(_tempDir, "20240101130000_third.sql"), "");

            var files = Directory.EnumerateFiles(_tempDir, "*.sql").OrderBy(f => f).ToList();

            Assert.AreEqual("20240101120000_first.sql", Path.GetFileName(files[0]));
            Assert.AreEqual("20240101130000_third.sql", Path.GetFileName(files[1]));
            Assert.AreEqual("20240102120000_second.sql", Path.GetFileName(files[2]));
        }
    }
}
