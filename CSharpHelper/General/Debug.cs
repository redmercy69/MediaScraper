namespace CSharpHelper;

public static class Debug
{
    public static void WriteLine(IEnumerable<object> messages)
    {
        foreach (var message in messages)
        {
            Console.WriteLine(message);
        }
    }

    public static void WriteLine((string, string)[] messages)
    {
        foreach (var message in messages)
        {
            Console.WriteLine(message);
        }
    }
}