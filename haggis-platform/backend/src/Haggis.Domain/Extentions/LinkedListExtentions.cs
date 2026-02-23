using Haggis.Domain.Model;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class LinkedListExtensions
{
    public static string ToJson(this LinkedList<TrickPlay> source)
    {
        return JsonConvert.SerializeObject(source, Formatting.Indented);
    }
}
