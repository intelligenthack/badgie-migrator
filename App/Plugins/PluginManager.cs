using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Badgie.Migrator.Plugins
{
    /// <summary>
    /// Manages database plugins and orchestrates their execution during migrations
    /// </summary>
    public class PluginManager
    {
        private readonly List<IDbPlugin> _availablePlugins;
        private readonly List<IDbPlugin> _activePlugins;

        public PluginManager()
        {
            _availablePlugins = new List<IDbPlugin>();
            _activePlugins = new List<IDbPlugin>();
            
            // Register built-in plugins
            RegisterBuiltInPlugins();
        }

        /// <summary>
        /// Register all built-in plugins
        /// </summary>
        private void RegisterBuiltInPlugins()
        {
            _availablePlugins.Add(new TimescaleDbPlugin());
        }

        /// <summary>
        /// Discovers and activates plugins that should run for the given connection and configuration
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        public void DiscoverAndActivatePlugins(DbConnection connection, Config config)
        {
            _activePlugins.Clear();

            if (!config.EnablePlugins)
            {
                if (config.Verbose)
                {
                    Console.WriteLine("Info: Plugins are disabled in configuration");
                }
                return;
            }

            foreach (var plugin in _availablePlugins)
            {
                try
                {
                    if (plugin.ShouldActivate(connection, config))
                    {
                        _activePlugins.Add(plugin);
                        if (config.Verbose)
                        {
                            Console.WriteLine($"Info: Activated plugin: {plugin.Name}");
                        }
                    }
                    else
                    {
                        if (config.Verbose)
                        {
                            Console.WriteLine($"Info: Plugin {plugin.Name} not applicable for this database");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error checking plugin {plugin.Name}: {ex.Message}");
                    if (config.Verbose && config.StackTraces)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            if (config.Verbose && _activePlugins.Count == 0)
            {
                Console.WriteLine("Info: No plugins activated for this database");
            }
        }

        /// <summary>
        /// Executes PreMigration on all active plugins
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        public void ExecutePreMigration(DbConnection connection, Config config)
        {
            foreach (var plugin in _activePlugins)
            {
                try
                {
                    if (config.Verbose)
                    {
                        Console.WriteLine($"Info: Executing pre-migration for plugin: {plugin.Name}");
                    }
                    plugin.PreMigration(connection, config);
                    if (config.Verbose)
                    {
                        Console.WriteLine($"Info: Pre-migration completed for plugin: {plugin.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: Pre-migration failed for plugin {plugin.Name}: {ex.Message}");
                    if (config.StackTraces)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    throw; // Re-throw to stop migration process
                }
            }
        }

        /// <summary>
        /// Executes PostMigration on all active plugins
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        public void ExecutePostMigration(DbConnection connection, Config config)
        {
            // Execute in reverse order to mirror cleanup
            for (int i = _activePlugins.Count - 1; i >= 0; i--)
            {
                var plugin = _activePlugins[i];
                try
                {
                    if (config.Verbose)
                    {
                        Console.WriteLine($"Info: Executing post-migration for plugin: {plugin.Name}");
                    }
                    plugin.PostMigration(connection, config);
                    if (config.Verbose)
                    {
                        Console.WriteLine($"Info: Post-migration completed for plugin: {plugin.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Post-migration failed for plugin {plugin.Name}: {ex.Message}");
                    if (config.StackTraces)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    // Don't re-throw post-migration errors to avoid masking the main migration success
                }
            }
        }

        /// <summary>
        /// Executes OnMigrationFailure on all active plugins
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        /// <param name="originalException">The exception that caused the migration failure</param>
        public void ExecuteOnMigrationFailure(DbConnection connection, Config config, Exception originalException)
        {
            // Execute in reverse order to mirror cleanup
            for (int i = _activePlugins.Count - 1; i >= 0; i--)
            {
                var plugin = _activePlugins[i];
                try
                {
                    if (config.Verbose)
                    {
                        Console.WriteLine($"Info: Executing failure cleanup for plugin: {plugin.Name}");
                    }
                    plugin.OnMigrationFailure(connection, config, originalException);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failure cleanup failed for plugin {plugin.Name}: {ex.Message}");
                    if (config.StackTraces)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    // Don't re-throw cleanup errors
                }
            }
        }

        /// <summary>
        /// Gets the names of all currently active plugins
        /// </summary>
        public IEnumerable<string> GetActivePluginNames()
        {
            return _activePlugins.Select(p => p.Name);
        }
    }
}