using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Badgie.Migrator.Tests
{
    public class Md5HashTests
    {
        private static string ComputeMd5(string content)
        {
            using var md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(content)));
        }

        [Test]
        public void Md5_SameContent_ProducesSameHash()
        {
            var content = "SELECT * FROM users";
            var hash1 = ComputeMd5(content);
            var hash2 = ComputeMd5(content);

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void Md5_DifferentContent_ProducesDifferentHash()
        {
            var hash1 = ComputeMd5("SELECT * FROM users");
            var hash2 = ComputeMd5("SELECT * FROM orders");

            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void Md5_WhitespaceChanges_ProducesDifferentHash()
        {
            var hash1 = ComputeMd5("SELECT * FROM users");
            var hash2 = ComputeMd5("SELECT *  FROM users"); // extra space

            Assert.AreNotEqual(hash1, hash2, "Whitespace changes should produce different hash");
        }

        [Test]
        public void Md5_CaseChanges_ProducesDifferentHash()
        {
            var hash1 = ComputeMd5("SELECT * FROM users");
            var hash2 = ComputeMd5("select * from users");

            Assert.AreNotEqual(hash1, hash2, "Case changes should produce different hash");
        }

        [Test]
        public void Md5_EmptyString_ProducesValidHash()
        {
            var hash = ComputeMd5("");

            Assert.IsNotNull(hash);
            Assert.IsNotEmpty(hash);
        }

        [Test]
        public void Md5_HashIsBase64Encoded()
        {
            var hash = ComputeMd5("SELECT 1");

            // Base64 should only contain valid characters
            Assert.DoesNotThrow(() => Convert.FromBase64String(hash));
        }

        [Test]
        public void Md5_LargeContent_ProducesFixedLengthHash()
        {
            var smallHash = ComputeMd5("SELECT 1");
            var largeContent = new string('X', 100000);
            var largeHash = ComputeMd5(largeContent);

            // MD5 always produces 16 bytes = 24 chars in Base64 (with padding)
            Assert.AreEqual(smallHash.Length, largeHash.Length);
            Assert.AreEqual(24, largeHash.Length);
        }

        [Test]
        public void Md5_UnicodeContent_HandledCorrectly()
        {
            var hash1 = ComputeMd5("SELECT 'Hello, 世界!'");
            var hash2 = ComputeMd5("SELECT 'Hello, 世界!'");

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void Md5_NewlineVariations_ProduceDifferentHashes()
        {
            var hashUnix = ComputeMd5("SELECT 1\nSELECT 2");
            var hashWindows = ComputeMd5("SELECT 1\r\nSELECT 2");

            Assert.AreNotEqual(hashUnix, hashWindows, "Different line endings should produce different hashes");
        }
    }
}
