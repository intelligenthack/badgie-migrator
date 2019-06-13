# badgie-migrator
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
dotnet-badgie-migrator <connection string> [drive:][path][filename] [-f] [-i] [-d]
  -f runs mutated migrations
  -i if needed, installs the db table needed to store state
  -d (SqlServer|Postgres) specifies whether to run against SQL Server or PostgreSQL
```

## Building

```
dotnet pack -c Release
```

Creates the DotNet CLI Tool package in App/bin/Relase/badgie-migrator.{version}.nupkg
