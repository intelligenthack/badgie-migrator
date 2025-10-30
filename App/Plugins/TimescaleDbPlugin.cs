using System;
using System.Data.Common;
using Dapper;

namespace Badgie.Migrator.Plugins
{
    /// <summary>
    /// Plugin for TimescaleDB that manages background workers during migrations
    /// </summary>
    public class TimescaleDbPlugin : IDbPlugin
    {
        public string Name => "TimescaleDB Background Worker Manager";

        private bool _backgroundWorkersWereStopped;

        /// <summary>
        /// Checks if TimescaleDB extension is installed in the database
        /// </summary>
        public bool ShouldActivate(DbConnection connection, Config config)
        {
            // Only activate for PostgreSQL databases
            if (config.SqlType != SqlType.Postgres)
            {
                return false;
            }

            try
            {
                // Check if TimescaleDB extension is installed
                var timescaleInstalled = connection.QueryFirstOrDefault<bool>(
                    "SELECT EXISTS(SELECT 1 FROM pg_extension WHERE extname = 'timescaledb');"
                );

                return timescaleInstalled;
            }
            catch (Exception ex)
            {
                if (config.Verbose)
                {
                    Console.WriteLine($"Info: Could not detect TimescaleDB extension: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Stops TimescaleDB background workers before migrations
        /// </summary>
        public void PreMigration(DbConnection connection, Config config)
        {
            try
            {
                if (config.Verbose)
                {
                    Console.WriteLine("Info: Stopping TimescaleDB background workers...");
                }

                // Stop background workers
                connection.Execute("SELECT _timescaledb_functions.stop_background_workers();");
                
                _backgroundWorkersWereStopped = true;
                
                if (config.Verbose)
                {
                    Console.WriteLine("Info: TimescaleDB background workers stopped successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Failed to stop TimescaleDB background workers: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Restarts TimescaleDB background workers after migrations
        /// </summary>
        public void PostMigration(DbConnection connection, Config config)
        {
            if (!_backgroundWorkersWereStopped)
            {
                if (config.Verbose)
                {
                    Console.WriteLine("Info: TimescaleDB background workers were not stopped, skipping restart");
                }
                return;
            }

            try
            {
                if (config.Verbose)
                {
                    Console.WriteLine("Info: Starting TimescaleDB background workers...");
                }

                // Restart background workers
                connection.Execute("SELECT _timescaledb_functions.start_background_workers();");

                _backgroundWorkersWereStopped = false;
                
                if (config.Verbose)
                {
                    Console.WriteLine("Info: TimescaleDB background workers started successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restart TimescaleDB background workers: {ex.Message}");
                // Don't throw here as the migrations succeeded
            }
        }

        /// <summary>
        /// Attempts to restart background workers if migrations fail
        /// </summary>
        public void OnMigrationFailure(DbConnection connection, Config config, Exception exception)
        {
            if (!_backgroundWorkersWereStopped)
            {
                return;
            }

            try
            {
                if (config.Verbose)
                {
                    Console.WriteLine("Info: Attempting to restart TimescaleDB background workers after migration failure...");
                }

                // Try to restart background workers even after failure
                connection.Execute("SELECT _timescaledb_functions.start_background_workers();");
                
                _backgroundWorkersWereStopped = false;
                
                if (config.Verbose)
                {
                    Console.WriteLine("Info: TimescaleDB background workers restarted after failure");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to restart TimescaleDB background workers after migration failure: {ex.Message}");
                Console.WriteLine("Warning: You may need to manually restart TimescaleDB background workers using: SELECT _timescaledb_functions.start_background_workers();");
            }
        }
    }
}