using B2B_CognitiveServices_Cafe.Model;
using System.Collections.Generic;

namespace B2B_CognitiveServices_Cafe
{
    public class CoffeeOrderResult
    {
        public string JsonResponse { get; set; }
        public IEnumerable<CoffeeOrder> Order { get; set; }
    }
}
