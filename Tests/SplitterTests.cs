using NUnit.Framework;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Badgie.Migrator.Tests
{
    public class SplitterTests
    {
        private static Regex GetSplitter()
        {
            var field = typeof(Program).GetField("Splitter", BindingFlags.NonPublic | BindingFlags.Static);
            return (Regex)field.GetValue(null);
        }

        [TestCase("\nGO\n")]
        [TestCase("\nGO \n")]
        [TestCase("\n  GO\n")]
        [TestCase("\n  GO  \n")]
        [TestCase("\n\tGO\t\n")]
        public void SplitterAcceptsWhitespaceAroundGo(string delimiter)
        {
            var splitter = GetSplitter();
            var sql = $"SELECT 1{delimiter}SELECT 2";
            var parts = splitter.Split(sql);
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual("SELECT 1", parts[0].Trim());
            Assert.AreEqual("SELECT 2", parts[1].Trim());
        }
    }
}
