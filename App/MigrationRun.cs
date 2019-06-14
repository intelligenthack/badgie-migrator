using System;

namespace Badgie.Migrator
{
    /// <summary>
    /// Represents the status of a migration run
    /// </summary>
    public class MigrationRun
    {
        /// <summary>
        /// Sequential Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Last time the migration was run
        /// </summary>
        public DateTime LastRun { get; set; }

        /// <summary>
        /// The filename representing the migration
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The migration checksum, used to verify changes
        /// </summary>
        public string MD5 { get; set; }

        /// <summary>
        /// The result of the last run, as a <see cref="MigrationResult"/>
        /// </summary>
        public MigrationResult MigrationResult { get; set; }
    }
}
