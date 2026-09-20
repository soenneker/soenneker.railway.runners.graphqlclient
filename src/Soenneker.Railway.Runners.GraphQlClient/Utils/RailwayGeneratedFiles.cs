using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Soenneker.GraphQL.Generator.Dtos;

namespace Soenneker.Railway.Runners.GraphQlClient.Utils;

// Compatibility with Soenneker.GraphQL.Generator 4.0.143 and Railway's schema.
// Normalize in memory before writing: Windows would otherwise overwrite the GitHub/Github groups.
internal static class RailwayGeneratedFiles
{
    internal static IReadOnlyList<GeneratedFile> Normalize(IEnumerable<GeneratedFile> files)
    {
        var result = new List<GeneratedFile>();
        var groups = new Dictionary<string, (string Header, List<string> Members)>(StringComparer.Ordinal);
        foreach (GeneratedFile file in files)
        {
            string content = file.Content.Replace("GitHubBuilder", "GithubBuilder", StringComparison.Ordinal);
            content = content.Replace("GetPrivateNetworkEndpointRequestBuilder Endpoint =>", "GetPrivateNetworkEndpointRequestBuilder GetEndpoint =>", StringComparison.Ordinal)
                             .Replace("GetVolumeInstanceRequestBuilder Instance =>", "GetVolumeInstanceRequestBuilder GetInstance =>", StringComparison.Ordinal);
            if (file.RelativePath.Replace('\\', '/') is "Types/Objects/CustomDomain.cs" or "Types/Objects/ServiceDomain.cs")
                content = content.Replace("public string Domain {", "public string DomainValue {", StringComparison.Ordinal);

            content = Regex.Replace(content, @"(public System\.Text\.Json\.JsonElement @?\w+ \{ get; init; \}) = null!;", "$1");
            content = Regex.Replace(content, @"public enum (\w+)", "[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<$1>))]\npublic enum $1");
            Match group = Regex.Match(content, @"public sealed partial class (\w+Builder)\s*\{");
            if (group.Success && !group.Groups[1].Value.EndsWith("RequestBuilder", StringComparison.Ordinal))
            {
                string name = group.Groups[1].Value;
                // Group builders contain only transport construction and expression-bodied child accessors.
                string header = content[..(group.Index + group.Length)];
                string[] members = Regex.Matches(content, @"    public [^\r\n]+ => [^\r\n]+;").Select(m => m.Value).ToArray();
                if (!groups.TryGetValue(name, out var existing))
                    groups[name] = (header, new List<string>(members));
                else
                    existing.Members.AddRange(members);
                continue;
            }
            result.Add(file with { Content = content });
        }
        foreach (var (name, group) in groups)
        {
            string content = group.Header + "\n    private readonly IGraphQlClient _graphQlClient;\n\n" +
                             $"    public {name}(IGraphQlClient graphQlClient) => _graphQlClient = graphQlClient;\n\n" +
                             string.Join("\n\n", group.Members.Distinct(StringComparer.Ordinal)) + "\n}\n";
            result.Add(new GeneratedFile($"Clients/Groups/{name}.cs", content));
        }
        if (result.Select(file => file.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Count)
            throw new InvalidOperationException("Generated file paths collide. Generation stopped before overwriting the existing client.");
        return result;
    }
}


