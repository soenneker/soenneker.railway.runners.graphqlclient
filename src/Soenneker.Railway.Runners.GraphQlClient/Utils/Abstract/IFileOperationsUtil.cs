using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Railway.Runners.GraphQlClient.Utils.Abstract;

/// <summary>Regenerates Railway's typed client from its introspection endpoint or an existing SDL schema.</summary>
public interface IFileOperationsUtil
{
    /// <summary>
    /// Generates and builds the client. Railway:RepositoryDirectory targets an existing local checkout without pushing.
    /// Without a local target, clones the client repository and pushes only after a successful build.
    /// Railway:SchemaPath optionally selects an offline SDL file; Railway:ApiKey optionally authenticates introspection.
    /// </summary>
    ValueTask Process(CancellationToken cancellationToken = default);
}
