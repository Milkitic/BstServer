using System;

namespace BstServer;

public static class StringExtension
{
    public static string[] SplitByStep(this string source, int step)
    {
        int len = source.Length;
        string[] array = new string[(int)Math.Ceiling((double)len / step)];
        for (int i = 0, j = 0; j < array.Length; i += step, j++)
        {
            if (i + step > len)
                array[j] = source.Substring(i);
            else
                array[j] = source.Substring(i, step);
        }

        return array;
    }
}