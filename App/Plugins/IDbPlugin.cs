using System;
using System.Data.Common;

namespace Badgie.Migrator.Plugins
{
    /// <summary>
    /// Interface for database-specific plugins that can execute custom logic before and after migrations
    /// </summary>
    public interface IDbPlugin
    {
        /// <summary>
        /// The name of the plugin for identification and logging
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines if this plugin should be active for the given database connection and configuration
        /// </summary>
        /// <param name="connection">The database connection to test</param>
        /// <param name="config">The migration configuration</param>
        /// <returns>True if the plugin should be activated, false otherwise</returns>
        bool ShouldActivate(DbConnection connection, Config config);

        /// <summary>
        /// Called before any migrations are executed in the current batch
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        void PreMigration(DbConnection connection, Config config);

        /// <summary>
        /// Called after all migrations in the current batch have been executed successfully
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        void PostMigration(DbConnection connection, Config config);

        /// <summary>
        /// Called if migrations fail, allowing the plugin to perform cleanup
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="config">The migration configuration</param>
        /// <param name="exception">The exception that caused the failure</param>
        void OnMigrationFailure(DbConnection connection, Config config, Exception exception);
    }
}