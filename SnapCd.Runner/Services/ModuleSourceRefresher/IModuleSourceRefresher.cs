using SnapCd.Contracts;

namespace SnapCd.Runner.Services.ModuleSourceRefresher;

public interface IModuleSourceRefresher
{
    public string GetRemoteDefinitiveRevision(string sourceUrl, string sourceRevision, SourceRevisionType sourceRevisionType);
}