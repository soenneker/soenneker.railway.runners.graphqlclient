using System;
using System.Linq;
using Soenneker.GraphQL.Generator;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.Railway.Runners.GraphQlClient.Utils;

namespace Soenneker.Railway.Runners.GraphQlClient.Tests;

public sealed class RailwayGraphQlClientRunnerTests
{
    [Test]
    public void PreservesOperationsAcrossRailwayNamingCollisions()
    {
        const string schema = """
            interface Domain { domain: String! }
            type CustomDomain implements Domain { domain: String! }
            type ServiceDomain implements Domain { domain: String! }
            type Query {
              gitHubSshKeys: String
              gitHubRepoAccessAvailable: String
              githubRepos: String
              githubRepoBranches: String
              egressGatewayHAPreview: String
              egressGatewayLegacyPreview: String
              volumeInstance: String
              privateNetworkEndpoint: String
            }
            type Mutation {
              egressGatewayAssociationCreate: String
              egressGatewayAssociationsClear: String
              volumeInstanceUpdate: String
              privateNetworkEndpointCreate: String
            }
            """;
        var generated = new GraphQlGenerator().Generate(schema, new GeneratorConfig { Namespace = "Test", OutputDirectory = "unused" });
        var files = RailwayGeneratedFiles.Normalize(generated.Files);
        if (files.Select(f => f.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
            throw new Exception("Generated file paths would overwrite each other on Windows.");
        string github = files.Single(f => f.RelativePath == "Clients/Groups/GithubBuilder.cs").Content;
        if (!github.Contains("SshKeys =>") || !github.Contains("Repos =>"))
            throw new Exception("A GitHub operation was lost while merging case-sensitive group names.");
        string egress = files.Single(f => f.RelativePath == "Clients/Groups/EgressGatewayBuilder.cs").Content;
        if (!egress.Contains("HAPreview =>") || !egress.Contains("AssociationCreate =>"))
            throw new Exception("An egress gateway operation was lost.");
        foreach (string domain in new[] { "CustomDomain", "ServiceDomain" })
        {
            string source = files.Single(f => f.RelativePath.Replace('\\', '/') == $"Types/Objects/{domain}.cs").Content;
            if (!source.Contains("public string DomainValue") || !source.Contains("JsonPropertyName(\"domain\")"))
                throw new Exception("The Domain interface mapping is incorrect.");
        }
    }
}

