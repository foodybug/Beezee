using System;
using System.Collections.Generic;

public static class ExtensionMethods
{
    public static void Shuffle<T>(this IList<T> list)
    {
        Random rng = new Random();
        int n = list.Count;

        // 리스트의 마지막 요소부터 순회
        while (n > 1)
        {
            n--;
            // 현재 요소(n)와 0부터 n까지의 무작위 인덱스(k)를 선택
            int k = rng.Next(n + 1);

            // 두 요소의 위치를 교환
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
