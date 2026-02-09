using Haggis.Model;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace Haggis.Extentions
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> DeepCopy<T>(this IEnumerable<T> source) where T : ICloneable
        {
            if (source == null)
                return null;

            return source.Select(item => (T)item.Clone()).ToList();
        }

        public static IEnumerable<T> DeepStructCopy<T>(this IEnumerable<T> source) where T : struct
        {
            if (source == null)
                return null;

           return source.Select(item => item).ToList();

        }

        public static T DeepCopyObject<T>(T obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, obj);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(memoryStream);
            }
        }

        public static List<Card> ToCards(this IEnumerable<string> source)
        {
            List<Card> cards = new List<Card>();

            foreach (string card in source)
            {
                cards.Add(card.ToCard());
            }
            return cards;
        }
        public static string ToLetters(this List<Card> source)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            for (int i = 0; i < source.Count; i++)
            {
                sb.Append(source[i].ToString());
                if (i < source.Count - 1)
                {
                    sb.Append("|"); 
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        public static List<T> AddAndReturn<T>(this List<T> list, List<T> items)
        {
            list.AddRange(items);
            return list;
        }
    }
}
