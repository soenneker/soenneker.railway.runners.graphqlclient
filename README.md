# Soenneker.Railway.Runners.GraphQlClient

Downloads Railway's introspection schema, generates the typed .NET client and verifies a Release build. Uses Soenneker.GraphQL.Generator 4.0.143 with a deterministic Railway compatibility pass for group-name collisions, Domain interface properties, string enums and JSON scalars.

Local regeneration from this repository (PowerShell):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Local'
dotnet run --project src/Soenneker.Railway.Runners.GraphQlClient -- --Railway:RepositoryDirectory=C:\git\Soenneker\Railway\soenneker.railway.graphqlclient
```

An explicit local directory must contain the existing client project. Local mode never commits or pushes. Only the dedicated `Generated` directory is replaced; the schema snapshot is written to `graphql.schema`.

For offline reproduction, add `--Railway:SchemaPath=<absolute-path-to-graphql.schema>`. The default schema endpoint is `https://backboard.railway.com/graphql/v2`. Optional `Railway:ApiKey`, `Railway:AuthHeaderName`, `Railway:AuthHeaderValueTemplate` and `Railway:ClientBaseUrl` settings control introspection authentication and endpoint overrides.

Without `Railway:RepositoryDirectory`, the scheduled workflow clones `soenneker/soenneker.railway.graphqlclient`, regenerates and builds, then commits and pushes using `GH__TOKEN`, `GIT__NAME` and `GIT__EMAIL`. A failed build exits unsuccessfully and does not push. The schema compatibility pass is regression-tested; revisit it when changing the generator version.
