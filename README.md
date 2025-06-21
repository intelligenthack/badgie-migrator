![Badgie Migrator](https://raw.githubusercontent.com/intelligenthack/badgie-migrator/master/4pMMXly.png)

A SQL migration tool originally built for Badgie

## What are database migrations?

As you develop new versions of a server application which you want to deploy with no downtime, you'll need to write snippets of SQL that change the schema from the current version to the next, so they can be run on all the systems (production, staging, development...) in the same way. If these migrations are the only schema-changing operations on the database, this gives you a good guarantee that all the schemas remain in sync.

## How are migrations used?

Usually there's a build step that runs migrations. This in general should happen before running the application or running tests. It is a convention that we first alter the database in a backwards compatible way and then we deploy the new version of the application that takes advantage of the new version

## Are migrations safe?

In order to make migrations safe there are a few preconditions to know about.

1. each migration should have a *guard*, i.e. an `IF` statement that only runs the migration if the schema is in the right state. For example you might want to only add a certain table if the table is not present.
2. each migration should therefore be *idempotent*: if you run it twice, it should only change the schema once
3. migrations should be immutable
4. migrations should either add or delete database entities, and avoid altering existing ones. Adding a column is fine, altering it can be problematic, renaming it is likely not a good idea at all. Be aware that usually migrations are run before a new deployment of code so a column rename would probably break the existing version momentarily while the code is deployed.
5. in general data migrations (migrations that add, update or delete data) are complex and risky business and we recommend avoiding them in favor of writing code that seeds the database appropriately as it's run.

## Why Badgie Migrator?

Badgie migrator offers significant advantages over common ORM migrators (i.e. ruby-on-rails or Entity Framework)!

1. Easy, trivial deployability. The tool is just a small cross platform app which you can point to your migrations folder and it just works. It does not require the application to be present and it's completely decoupled from the platform you use. It just works.
2. Migrations are written in SQL, because we believe in simplicity. Databases use SQL, use SQL with databases and not a made up "domain language" where you have to go through hoops just to effectively run the SQL you need.
3. The state of migrations is kept safe in a database table. The table is easy to understand and "hack" if you ever need to -- not that this is ever required.
4. Migrations are ever only run once, even if idempotent. Safety in depth.
5. The tool catches migration mutations and stops them (unless you force it not to!) because we want to be able to run migrations on a "blank" db and recreate the current state reliably. If we mutate migrations we change the "past" and therefore the schema will not be the same between current systems (pre mutation) and newer systems (post mutation).
6. We support cases in which you have multiple migration folders, and/or multiple db connections, so you can use this little tool against big systems


## Installation
Install the migrator tool either as a global tool:

```
dotnet tool install -g Badgie.Migrator
```

...or as a CliToolReference in your project:

```
<ItemGroup>
    <DotNetCliToolPackageReference Include="Badgie.Migrator"/>
</ItemGroup>
```

## Usage
Once the tool is installed you can simply call it like:

```
dotnet-badgie-migrator <connection string> [drive:][path][filename pattern] [-f] [-i] [-d] [-n] [-V] [--no-stack-trace]
  -f runs mutated migrations
  -i if needed, installs the db table needed to store state
  -d:(SqlServer|Postgres|MySql) specifies whether to run against SQL Server, PostgreSQL or MySql
  -n avoids wrapping each execution in a transaction 
  -V verbose mode for debugging
  --no-stack-trace omits the (mostly useless) stack traces
```

Alternatively, if you have many databases to run migrations against you can pass a json configuration file with many configurations:

```
dotnet-badgie-migrator -json=<configuration filename.json>
```

Here is a sample file to use as a template:

```
[
  {
    "ConnectionString": "Connection 1",
    "Force": true,
    "Install": true,
    "SqlType": "SqlServer",
    "Path": "Path 1",
    "UseTransaction": true
  },                      
  {
    "ConnectionString": "Connection 2",
    "Force": false,
    "Install": false,
    "SqlType": "Postgres",
    "Path": "Path 2",
    "UseTransaction": false
  }
]
```

## Building

```
dotnet pack -c Release
```

Creates the DotNet CLI Tool package in App/bin/Release/badgie-migrator.{version}.nupkg
