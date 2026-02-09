using System.Collections.Generic;

static class ListExtensions
{
    public static T GetSecondToLast<T>(this List<T> list)
    {
        if (list.Count < 2)
        {  
            return default; 
        }
        return list[list.Count - 2];
    }
}
