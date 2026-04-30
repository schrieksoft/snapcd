namespace SnapCd.Server.Core.Misc.Utils.Helpers;

public static class AttributeHelper
{
    public static IEnumerable<KeyValuePair<string, object>> ParseAttributes(string attributes)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(attributes))
        {
            var parts = attributes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0];
                    var value = keyValue[1].Trim('"');
                    result.Add(key, value);
                }
            }
        }

        return result;
    }
}