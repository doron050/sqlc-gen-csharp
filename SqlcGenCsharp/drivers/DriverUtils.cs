namespace DefaultNamespace;

using System.Text.RegularExpressions;

public class DriverUtils
{
    public static string FieldName(string prefix, int index, Column column = null)
    {
        string name = $"{prefix}_{index}";
        if (column != null)
        {
            name = column.Name;
        }

        return Regex.Replace(name.ToLower(), "(_[a-z])", m => m.Value.ToUpper().Replace("_", ""));
    }

    public static string ArgName(int index, Column column = null)
    {
        return FieldName("arg", index, column);
    }

    public static string ColName(int index, Column column = null)
    {
        return FieldName("col", index, column);
    }
}