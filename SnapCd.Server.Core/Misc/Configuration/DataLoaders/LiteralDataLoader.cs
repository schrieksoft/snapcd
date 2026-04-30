namespace SnapCd.Server.Core.Misc.Configuration.DataLoaders;

public class LiteralDataLoader : IDataLoader
{
    public IDictionary<string, string> Load(IDictionary<string, string> input)
    {
        return input;
    }
}