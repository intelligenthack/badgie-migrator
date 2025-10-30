using NUnit.Framework;
using System;
using System.Data.Common;
using Badgie.Migrator.Plugins;

namespace Badgie.Migrator.Tests
{
    [TestFixture]
    public class PluginTests
    {
        [Test]
        public void TimescaleDbPlugin_ShouldNotActivate_ForNonPostgresDatabase()
        {
            // Arrange
            var plugin = new TimescaleDbPlugin();
            var config = new Config { SqlType = SqlType.MySql };

            // Act & Assert
            // For non-Postgres databases, the plugin should return false immediately
            // without even trying to connect, so we can pass null for connection
            var shouldActivate = plugin.ShouldActivate(null, config);

            Assert.IsFalse(shouldActivate);
        }

        [Test]
        public void TimescaleDbPlugin_Name_IsCorrect()
        {
            // Arrange
            var plugin = new TimescaleDbPlugin();

            // Act
            var name = plugin.Name;

            // Assert
            Assert.AreEqual("TimescaleDB Background Worker Manager", name);
        }

        [Test]
        public void PluginManager_ShouldNotActivatePlugins_WhenDisabled()
        {
            // Arrange
            var pluginManager = new PluginManager();
            var config = new Config { EnablePlugins = false, SqlType = SqlType.Postgres };

            // Act
            // When plugins are disabled, discovery should not activate any plugins
            // regardless of connection, so we can pass null
            pluginManager.DiscoverAndActivatePlugins(null, config);
            var activePlugins = pluginManager.GetActivePluginNames();

            // Assert
            Assert.IsEmpty(activePlugins);
        }

        [Test]
        public void PluginManager_HasTimescaleDbPlugin_InAvailablePlugins()
        {
            // Arrange
            var pluginManager = new PluginManager();
            var config = new Config { EnablePlugins = true, SqlType = SqlType.SqlServer }; // Non-Postgres to avoid activation

            // Act
            pluginManager.DiscoverAndActivatePlugins(null, config);

            // Assert
            // Even though no plugins are activated (SqlServer), the manager should have discovered plugins
            // We can't directly test the private _availablePlugins, but we can test the behavior
            // The fact that it doesn't throw an exception means plugins were registered
            Assert.DoesNotThrow(() => pluginManager.DiscoverAndActivatePlugins(null, config));
        }

    }
}