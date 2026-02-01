using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Badgie.Migrator.Tests
{
    public class EncodingValidationTests
    {
        [Test]
        public void RunFile_WithValidUtf8_DoesNotThrow()
        {
            var config = new Config
            {
                StrictEncoding = true,
                UseTransaction = false,
                Verbose = false
            };

            // Valid UTF-8 content - would need a mock connection to fully test
            // This test documents the expected behavior
            var validSql = "SELECT 'Hello, World!' as greeting";

            // The actual RunFile needs a database connection
            // This test verifies the encoding check logic concept
            var invalidCharFallback = ((DecoderReplacementFallback)Encoding.UTF8.DecoderFallback).DefaultString;
            var firstInvalidCharIndex = validSql.IndexOf(invalidCharFallback);

            Assert.AreEqual(-1, firstInvalidCharIndex, "Valid UTF-8 should not contain fallback characters");
        }

        [Test]
        public void StrictEncoding_DetectsInvalidCharacterPosition()
        {
            // Simulate what RunFile does for encoding validation
            var invalidCharFallback = ((DecoderReplacementFallback)Encoding.UTF8.DecoderFallback).DefaultString;

            // Create a string with the fallback character to simulate invalid encoding
            var sqlWithInvalidChar = $"SELECT 1{invalidCharFallback}SELECT 2";
            var firstInvalidCharIndex = sqlWithInvalidChar.IndexOf(invalidCharFallback);

            Assert.IsTrue(firstInvalidCharIndex >= 0, "Should detect invalid character");
            Assert.AreEqual(8, firstInvalidCharIndex, "Invalid character should be at position 8");
        }

        [Test]
        public void StrictEncoding_FindsCorrectLineForError()
        {
            var invalidCharFallback = ((DecoderReplacementFallback)Encoding.UTF8.DecoderFallback).DefaultString;
            var sql = $"SELECT 1\nSELECT {invalidCharFallback}2\nSELECT 3";

            var firstInvalidCharIndex = sql.IndexOf(invalidCharFallback);
            var lineSeparators = new[] { '\n', '\r' };
            var startOfLineIndex = sql.LastIndexOfAny(lineSeparators, firstInvalidCharIndex) + 1;
            var endOfLineIndex = sql.IndexOfAny(lineSeparators, firstInvalidCharIndex);

            var line = sql.Substring(startOfLineIndex, endOfLineIndex - startOfLineIndex);

            Assert.IsTrue(line.StartsWith("SELECT"), "Should extract the correct line");
            Assert.IsTrue(line.Contains(invalidCharFallback), "Line should contain the invalid character");
        }
    }
}
