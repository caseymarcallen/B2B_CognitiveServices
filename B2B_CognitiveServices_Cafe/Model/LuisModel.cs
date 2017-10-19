using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B_CognitiveServices_Cafe.Model
{
    public class LuisModel
    {
        public string query { get; set; }
        public string alteredQuery { get; set; }
        public Topscoringintent topScoringIntent { get; set; }
        public Intent[] intents { get; set; }
        public Entity[] entities { get; set; }
        public CompositeEntity[] compositeEntities { get; set; }
    }

    public class Topscoringintent
    {
        public string intent { get; set; }
        public float score { get; set; }
    }

    public class Intent
    {
        public static readonly string OrderCoffee = "Order Coffee";
        public static readonly string None = "None";

        public string intent { get; set; }
        public float score { get; set; }
    }

    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public float score { get; set; }
        public Resolution resolution { get; set; }
    }

    public class Resolution
    {
        public string[] values { get; set; }
        public string value { get; set; }
    }

    public class CompositeEntity
    {
        public string parentType { get; set; }
        public string value { get; set; }
        public Child[] children { get; set; }
        public CoffeeOrder ToCoffeeOrder(IEnumerable<Entity> relatedEntities)
        {
            int count = 1; // assume 1 unless specified otherwise
            CoffeeType coffeeType = CoffeeType.Unknown;
            var countEntity = children.Where(c => c.type == Child.NumberType).FirstOrDefault();
            if (countEntity != null)
            {
                if (!int.TryParse(countEntity.value, out count))
                {
                    var relatedEntity = relatedEntities.FirstOrDefault(re => re.entity == countEntity.value);
                    if (relatedEntity != null && relatedEntity.resolution != null && !string.IsNullOrEmpty(relatedEntity.resolution.value))
                    {
                        int.TryParse(relatedEntity.resolution.value, out count);
                    }
                }
            }

            var coffeeTypeEntity = children.Where(c => c.type == Child.CoffeeType).FirstOrDefault();
            if (coffeeTypeEntity != null)
            {
                if (!Enum.TryParse<CoffeeType>(coffeeTypeEntity.value.Replace(" ", ""), out coffeeType))
                {
                    var relatedEntity = relatedEntities.FirstOrDefault(re => re.entity == coffeeTypeEntity.value);
                    if (relatedEntity != null && relatedEntity.resolution != null && relatedEntity.resolution.values.Any())
                    {
                        if (Enum.TryParse<CoffeeType>(relatedEntity.resolution.values.First().Replace(" ", ""), out coffeeType))
                        {
                            return new CoffeeOrder
                            {
                                Number = count,
                                CoffeeType = coffeeType
                            };
                        }
                    }
                }
                
            }
            return null;
        }
    }

    public class Child
    {
        public static readonly string CoffeeType = "coffee type";
        public static readonly string NumberType = "builtin.number";

        public string type { get; set; }
        public string value { get; set; }
    }

}
