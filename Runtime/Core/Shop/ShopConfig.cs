using System.Collections.Generic;
using UnityEngine;

namespace _Project.Core.Shop
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "Configs/Shop Config")]
    public class ShopConfig : ScriptableObject
    {
        [SerializeField] private List<ShopProductConfig> _products = new List<ShopProductConfig>();

        public IReadOnlyList<ShopProductConfig> Products => _products;

        public ShopProductConfig Find(string productTag)
        {
            foreach (ShopProductConfig product in _products)
            {
                if (product.ProductTag == productTag)
                {
                    return product;
                }
            }

            return null;
        }
    }
}
