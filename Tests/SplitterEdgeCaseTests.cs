using NUnit.Framework;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Badgie.Migrator.Tests
{
    public class SplitterEdgeCaseTests
    {
        private static Regex GetSplitter()
        {
            var field = typeof(Program).GetField("Splitter", BindingFlags.NonPublic | BindingFlags.Static);
            return (Regex)field.GetValue(null);
        }

        [TestCase("\ngo\n")]
        [TestCase("\nGo\n")]
        [TestCase("\ngO\n")]
        [TestCase("\nGO\n")]
        public void SplitterIsCaseInsensitive(string delimiter)
        {
            var splitter = GetSplitter();
            var sql = $"SELECT 1{delimiter}SELECT 2";
            var parts = splitter.Split(sql);
            Assert.AreEqual(2, parts.Length);
        }

        [Test]
        public void SplitterHandlesMultipleGoStatements()
        {
            var splitter = GetSplitter();
            var sql = "SELECT 1\nGO\nSELECT 2\nGO\nSELECT 3";
            var parts = splitter.Split(sql);
            Assert.AreEqual(3, parts.Length);
        }

        [Test]
        public void SplitterDoesNotSplitGoWithoutNewlines()
        {
            var splitter = GetSplitter();
            var sql = "SELECT 'GO' as status";
            var parts = splitter.Split(sql);
            Assert.AreEqual(1, parts.Length);
            Assert.AreEqual(sql, parts[0]);
        }

        [Test]
        public void SplitterDoesNotSplitGoInMiddleOfLine()
        {
            var splitter = GetSplitter();
            var sql = "SELECT 1 GO SELECT 2";
            var parts = splitter.Split(sql);
            Assert.AreEqual(1, parts.Length);
        }

        [Test]
        public void SplitterHandlesGoAtEndOfFile()
        {
            var splitter = GetSplitter();
            var sql = "SELECT 1\nGO\n";
            var parts = splitter.Split(sql);
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("SELECT 1", parts[0].Trim());
        }

        [Test]
        public void SplitterRequiresNewlinesAroundGo()
        {
            var splitter = GetSplitter();
            // GO must have newlines on both sides to be recognized
            // "\nGO\n" is the pattern - bare GO without trailing newline won't match
            var sql = "SELECT 1\nGO\nSELECT 2\nGO\nSELECT 3";
            var parts = splitter.Split(sql);
            Assert.AreEqual(3, parts.Length);
            Assert.AreEqual("SELECT 1", parts[0].Trim());
            Assert.AreEqual("SELECT 2", parts[1].Trim());
            Assert.AreEqual("SELECT 3", parts[2].Trim());
        }

        [Test]
        public void SplitterPreservesContentWithGoInStrings()
        {
            var splitter = GetSplitter();
            var sql = "INSERT INTO t VALUES ('GO')\nGO\nSELECT 1";
            var parts = splitter.Split(sql);
            Assert.AreEqual(2, parts.Length);
            Assert.IsTrue(parts[0].Contains("'GO'"));
        }
    }
}
