# Example Database Migrations (SQL Server, MySQL, Postgres)

This document provides examples of valid, idempotent, and immutable migrations for SQL Server, MySQL, and Postgres. Each migration includes a guard (an IF statement or equivalent) to ensure it only runs if the schema is in the correct state, following best practices for safe schema evolution.

## Guidelines

- **Guarded**: Each migration uses an IF statement or equivalent to check schema state before applying changes.
- **Idempotent**: Running the migration multiple times will only change the schema once.
- **Immutable**: Migrations should not be edited after creation.
- **Add/Delete Only**: Migrations should only add or delete entities (tables, columns), not alter or rename existing ones.
- **No Data Migrations**: Avoid migrations that manipulate data; seed data through application code.

---

## SQL Server

```sql
-- Create 'users' table if it does not exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE [dbo].[users] (
        [id] INT IDENTITY(1,1) PRIMARY KEY,
        [username] NVARCHAR(50) NOT NULL,
        [email] NVARCHAR(100) NOT NULL
    );
END
```

---

## MySQL

```sql
-- Create 'users' table if it does not exist
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL
) ENGINE=InnoDB;
```

---

## Postgres

```sql
-- Create 'users' table if it does not exist
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL
);
```

---

## Advanced Example: Changing a NOT NULL Column Safely (Guarded Multi-Statement)

Changing a NOT NULL column (e.g., changing its type or name) should be done in multiple safe, idempotent steps. Here is a recommended approach:

### 1. Add the New Nullable Column

**Migration:**

#### SQL Server
```sql
-- Add new nullable column if it does not exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'new_column' AND Object_ID = Object_ID(N'users'))
BEGIN
    ALTER TABLE [dbo].[users] ADD [new_column] NVARCHAR(100) NULL;
END
```

#### MySQL
```sql
-- Add new nullable column if it does not exist
ALTER TABLE users ADD COLUMN IF NOT EXISTS new_column VARCHAR(100) NULL;
```

#### Postgres
```sql
-- Add new nullable column if it does not exist
ALTER TABLE users ADD COLUMN IF NOT EXISTS new_column VARCHAR(100);
```

**Code:**
- Write new data to `new_column`.
- Read from `new_column`, falling back to the old column if `new_column` is NULL.

**Minimal Code Examples:**

#### Step 1: Write to new column, read with fallback
```csharp
// Write
user.NewColumn = value;
// Read with fallback
var value = user.NewColumn ?? user.OldColumn;
```

### 2. In-Code Data Migration (Batch Process)

- Write application code to copy data from the old column to `new_column` in small batches.
- This is not a schema migration, but a background process run by the application.

**Minimal Code Examples:**

#### Step 2: In-code data migration (batch)
```csharp
// Pseudocode for batch migration
foreach (var user in Users.Where(u => u.NewColumn == null && u.OldColumn != null).Take(100))
{
    user.NewColumn = user.OldColumn;
    Save(user);
}
```

> **Tip:** For .NET projects, consider using [LINQPad](https://www.linqpad.net/) to run and test your in-code data migrations interactively and safely before integrating them into your application or automation scripts. LINQPad is especially useful for running batch updates and exploring your data model with LINQ queries.

### 3. Drop Fallback Reading

- Update application code to read only from `new_column`.

**Minimal Code Examples:**

#### Step 3: Read only from new column
```csharp
var value = user.NewColumn;
```

### 4. Drop the Old Column and Make the New Column NOT NULL (Single Guard)

**Migration:**

#### SQL Server
```sql
-- Drop old column and make new_column NOT NULL if conditions are met
IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'old_column' AND Object_ID = Object_ID(N'users'))
    AND EXISTS (SELECT * FROM sys.columns WHERE Name = N'new_column' AND is_nullable = 1 AND Object_ID = Object_ID(N'users'))
BEGIN
    ALTER TABLE [dbo].[users] DROP COLUMN [old_column];
    ALTER TABLE [dbo].[users] ALTER COLUMN [new_column] NVARCHAR(100) NOT NULL;
END
```

#### MySQL
```sql
-- Drop old column and make new_column NOT NULL if both conditions are met
DELIMITER //
CREATE PROCEDURE migrate_users_column()
BEGIN
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'users' AND COLUMN_NAME = 'old_column')
       AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'users' AND COLUMN_NAME = 'new_column' AND IS_NULLABLE = 'YES') THEN
        ALTER TABLE users DROP COLUMN old_column;
        ALTER TABLE users MODIFY COLUMN new_column VARCHAR(100) NOT NULL;
    END IF;
END //
DELIMITER ;
CALL migrate_users_column();
DROP PROCEDURE migrate_users_column;
```

#### Postgres
```sql
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='old_column')
       AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='users' AND column_name='new_column' AND is_nullable='YES') THEN
        EXECUTE 'ALTER TABLE users DROP COLUMN old_column';
        EXECUTE 'ALTER TABLE users ALTER COLUMN new_column SET NOT NULL';
    END IF;
END$$;
```

---

## Notes

- The guard (IF statement or CREATE TABLE IF NOT EXISTS) ensures the migration is idempotent and safe.
- To delete a table, use `DROP TABLE IF EXISTS table_name;`.
- Avoid using ALTER TABLE to modify or rename existing columns or tables.
- For adding columns, use `ALTER TABLE ... ADD COLUMN IF NOT EXISTS ...` where supported (Postgres 9.6+).
- For MySQL and standard SQL, check for column existence before adding (may require procedural SQL or manual checks).
