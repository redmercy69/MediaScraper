namespace CSharpHelper;

public static class Computer
{
    public static string FixFilePath(string filePath)
    {
        //Prevent file name too long error
        if (filePath.Length > 100)
            filePath = filePath[..100];
            
        filePath = filePath.Replace("&amp;" , "&")
                           .Replace("&#039;", "'");

        return filePath;
    }
}