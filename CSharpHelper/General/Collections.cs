namespace CSharpHelper;

public static class Collections
{
    public static void FillNextEmpty<T>(T[] array, T element)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null)
            {
                array[i] = element;
                return;
            }
        }
    }
}