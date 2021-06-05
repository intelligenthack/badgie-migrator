![Badgie Migrator](./4pMMXly.png)

A SQL migration tool originally built for Badgie

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
dotnet-badgie-migrator <connection string> [drive:][path][filename] [-f] [-i] [-d] [-n]
  -f runs mutated migrations
  -i if needed, installs the db table needed to store state
  -d:(SqlServer|Postgres) specifies whether to run against SQL Server or PostgreSQL
  -n avoids wrapping each execution in a transaction 
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

Creates the DotNet CLI Tool package in App/bin/Relase/badgie-migrator.{version}.nupkg
