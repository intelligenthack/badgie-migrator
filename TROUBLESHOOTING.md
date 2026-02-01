# Troubleshooting

## Database does not have data table installed, did you forget to pass "-i"?

The MigrationRuns table doesn't exist yet. Run with `-i` to create it:

```
dotnet-badgie-migrator "..." "*.sql" -d:SQLite -i
```

You only need `-i` once per database. After that, the table exists and you can omit it.

## Changed - filename.sql / Execution error

The migrator detected that a migration file was modified after it was already executed. The MD5 hash stored in the database doesn't match the current file contents.

What happened: either the file was edited, or line endings changed, or it was re-saved with different encoding.

Options:

1. Restore the original file contents
2. Run with `-f` to force execution of the modified migration
3. Manually delete the row from MigrationRuns if you want to re-run it fresh:
   ```sql
   DELETE FROM MigrationRuns WHERE Filename = 'filename.sql';
   ```

Using `-f` will re-execute the migration and update the stored hash. Be careful with this in production - if the migration already ran, running it again might fail or cause duplicate data.

## Error - filename.sql / SQLite Error 1: 'near "...": syntax error'

There's a SQL syntax error in your migration file. The error message tells you which word SQLite didn't understand. Check your SQL.

Use `--no-stack-trace` to hide the long stack trace if you just want the error message.

## A network-related or instance-specific error occurred while establishing a connection to SQL Server

The connection string is wrong, or the server isn't reachable. Common causes:

- Wrong server name
- SQL Server not running
- Firewall blocking the connection
- Wrong port (default is 1433)

For local development with self-signed certs, you may need `TrustServerCertificate=True` in the connection string.

## Could not find a part of the path

The migration folder path doesn't exist. Check that you typed it correctly and the directory exists.

## Files run in unexpected order

Migrations run in alphabetical order by filename. If you have `1.sql`, `10.sql`, `2.sql`, they run as: `1.sql`, `10.sql`, `2.sql`.

Use zero-padded numbers: `001.sql`, `002.sql`, `010.sql`.

## Verbose output

Add `-V` to see what the migrator is doing step by step. This shows which files it finds, the MD5 hashes it computes, and whether each migration is being run or skipped.
