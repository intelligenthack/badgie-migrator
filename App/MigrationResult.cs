namespace Badgie.Migrator
{
    /// <summary>
    /// Represents the status of a migration run
    /// </summary>
    public enum MigrationResult
    {
        /// <summary>
        /// The migration was run successfully
        /// </summary>
        Run,
        /// <summary>
        /// The migration was run previously, and it has been skipped
        /// </summary>
        Skipped,
        /// <summary>
        /// The migration was run previously, and the current version has changed
        /// </summary>
        Changed
    }
}
