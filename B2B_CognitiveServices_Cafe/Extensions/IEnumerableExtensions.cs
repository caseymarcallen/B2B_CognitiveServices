using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B_CognitiveServices_Cafe.Extensions
{
    public static class IEnumerableExtensions
    {
        private static Random random = new Random();
        public static string Random(this IList<string> collection)
        {
            return collection[random.Next(0, collection.Count)];
        }
    }
}
