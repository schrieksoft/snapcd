namespace SnapCd.Runner.Configuration.DataLoaders;

public interface IDataLoader
{
    public IDictionary<string, string> Load(IDictionary<string, string> input);
}
