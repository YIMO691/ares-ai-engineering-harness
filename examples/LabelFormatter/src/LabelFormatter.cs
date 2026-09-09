namespace LabelDemo;
public static class LabelFormatter
{
    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input;
    }
}
