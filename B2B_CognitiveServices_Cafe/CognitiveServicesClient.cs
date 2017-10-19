using B2B_CognitiveServices_Cafe.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace B2B_CognitiveServices_Cafe
{
    public class CognitiveServicesClient
    {
        // --------------------------------------
        // Edit these values
        // --------------------------------------
        private const string LuisAppId = "b2cc5cf4-4129-4daa-a6c6-97dbcac22121";
        private const string LuisSubscriptionKey = "7dbea38a27424387b2b3563b4c80ad72";




        public async Task<CoffeeOrderResult> MakeLuisRequest(string orderText)
        {
            string apiResponse = "";

            // INSERT LUIS API CALL CODE HERE!
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", LuisSubscriptionKey);
            var uri = $"https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{LuisAppId}?spellCheck=true&q={orderText}";
            var response = await client.GetAsync(uri);
            apiResponse = await response.Content.ReadAsStringAsync();


            var result = JsonConvert.DeserializeObject<LuisModel>(apiResponse);

            var order = ProcessOrder(result);

            return new CoffeeOrderResult
            {
                JsonResponse = apiResponse,
                Order = order
            };
        }

        private IEnumerable<CoffeeOrder> ProcessOrder(LuisModel result)
        {
            var orders = new List<CoffeeOrder>();
            try
            {
                if (result.topScoringIntent.intent == Intent.OrderCoffee)
                {
                    foreach (var compositeEntity in result.compositeEntities)
                    {
                        try
                        {
                            var coffeeOrder = compositeEntity.ToCoffeeOrder(result.entities);
                            if (coffeeOrder != null)
                            {
                                orders.Add(coffeeOrder);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Caught an error processing compositeEntity: " + e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught an error processing LuisModel: " + e);
            }

            return orders;
        }
    }
}
