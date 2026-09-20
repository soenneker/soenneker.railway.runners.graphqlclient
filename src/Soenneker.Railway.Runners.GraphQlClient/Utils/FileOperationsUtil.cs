using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQl.Schema.Conversion.Abstract;
using Soenneker.GraphQl.Schema.Download.Abstract;
using Soenneker.Git.Util.Abstract;
using Soenneker.Utils.Dotnet.Abstract;
using Soenneker.Utils.Environment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Railway.Runners.GraphQlClient.Utils.Abstract;

namespace Soenneker.Railway.Runners.GraphQlClient.Utils;

public sealed class FileOperationsUtil(
    ILogger<FileOperationsUtil> logger, IConfiguration configuration, IGitUtil gitUtil,
    IDotnetUtil dotnetUtil, IGraphQlGenerator generator,
    IGraphQlSchemaDownloadUtil download, IGraphQlSchemaConversionUtil conversion) : IFileOperationsUtil
{
    public async ValueTask Process(CancellationToken cancellationToken = default)
    {
        string? localDirectory = configuration["Railway:RepositoryDirectory"];
        string repository = string.IsNullOrWhiteSpace(localDirectory)
            ? await gitUtil.CloneToTempDirectory($"https://github.com/soenneker/{Constants.Library.ToLowerInvariant()}", cancellationToken: cancellationToken)
            : Path.GetFullPath(localDirectory);
        string projectDirectory = Path.Combine(repository, "src", Constants.Library);
        string project = Path.Combine(projectDirectory, Constants.Library + ".csproj");
        if (!File.Exists(project))
            throw new FileNotFoundException("The Railway client project must exist before regeneration.", project);

        string? schemaPath = configuration["Railway:SchemaPath"];
        string sdl;
        if (!string.IsNullOrWhiteSpace(schemaPath))
            sdl = await File.ReadAllTextAsync(Path.GetFullPath(schemaPath), cancellationToken);
        else
        {
            string endpoint = configuration["Railway:ClientBaseUrl"] ?? "https://backboard.railway.com/graphql/v2";
            string? apiKey = configuration["Railway:ApiKey"];
            Dictionary<string, string>? headers = null;
            if (!string.IsNullOrWhiteSpace(apiKey))
                headers = new Dictionary<string, string>
                {
                    [configuration["Railway:AuthHeaderName"] ?? "Authorization"] =
                        (configuration["Railway:AuthHeaderValueTemplate"] ?? "Bearer {token}").Replace("{token}", apiKey, StringComparison.Ordinal)
                };
            sdl = conversion.Convert(await download.Download(endpoint, headers: headers, cancellationToken: cancellationToken));
        }

        // Validate generation before replacing the dedicated generated-source directory.
        string generatedDirectory = Path.Combine(projectDirectory, "Generated");
        var scalars = new Dictionary<string, string>();
        foreach (Match scalar in Regex.Matches(sdl, @"(?m)^scalar (\w+)"))
        {
            string name = scalar.Groups[1].Value;
            // JSON preserves arbitrary objects, arrays and large numbers without precision loss.
            scalars[name] = name == "DateTime" ? "DateTimeOffset" : "System.Text.Json.JsonElement";
        }
        var options = new GeneratorConfig
        {
            Namespace = Constants.Library,
            OutputDirectory = generatedDirectory,
            EntryClientName = "RailwayGraphQlClient",
            ScalarMappings = scalars
        };
        var files = RailwayGeneratedFiles.Normalize(generator.Generate(sdl, options).Files);
        if (Directory.Exists(generatedDirectory))
            Directory.Delete(generatedDirectory, recursive: true);
        foreach (var file in files)
        {
            string path = Path.GetFullPath(Path.Combine(generatedDirectory, file.RelativePath));
            if (!path.StartsWith(generatedDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Generated file is outside the output directory.");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, file.Content, cancellationToken);
        }
        logger.LogInformation("Generated {Count} source files", files.Count);
        await File.WriteAllTextAsync(Path.Combine(repository, "graphql.schema"), sdl, cancellationToken);

        await dotnetUtil.Restore(project, cancellationToken: cancellationToken);
        if (!await dotnetUtil.Build(project, true, "Release", false, cancellationToken: cancellationToken))
            throw new InvalidOperationException("The regenerated Railway client did not build. Nothing was pushed.");

        // Explicit local targets are never committed or pushed by the runner.
        if (!string.IsNullOrWhiteSpace(localDirectory))
        {
            logger.LogInformation("Regenerated Railway client in {Repository}", repository);
            return;
        }
        await gitUtil.CommitAndPush(repository, "Regenerate Railway GraphQL client",
            EnvironmentUtil.GetVariableStrict("GH__TOKEN"), EnvironmentUtil.GetVariableStrict("GIT__NAME"),
            EnvironmentUtil.GetVariableStrict("GIT__EMAIL"), cancellationToken);
    }
}


