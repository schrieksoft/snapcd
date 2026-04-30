namespace SnapCd.Server.Core.Misc.Configuration.DataLoaders;

public interface IDataLoader
{
    public IDictionary<string, string> Load(IDictionary<string, string> input);
}