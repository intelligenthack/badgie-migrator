# badgie-migrator
A SQL migration tool originally built for Badgie

## Installation
Install the migrator tool either as a global tool:

```
dotnet tool install --global Badgie.Migrator --version 0.1.0
```

...or as a CliToolReference in your project:

```
<ItemGroup>
    <DotNetCliToolPackageReference Include="Badgie.Migrator"  Version="0.1.0"/>
</ItemGroup>
```

## Usage
Once the tool is installed you can simply call it like:

```
dotnet-badgie-migrator <connection string> [drive:][path][filename] [-f] [-i]
  -f runs mutated migrations
  -i if needed, installs the db table needed to store state
```

## Building

```
dotnet pack -c Release
```

Creates the DotNet CLI Tool package in App/bin/Relase/badgie-migrator.{version}.nupkg
