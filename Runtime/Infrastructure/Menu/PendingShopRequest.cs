using _Project.Core.Shop;

namespace _Project.Infrastructure.Menu
{
    public class PendingShopRequest : IPendingShopRequest
    {
        private bool _pending;

        public void Request()
        {
            _pending = true;
        }

        public bool Consume()
        {
            if (_pending == false)
            {
                return false;
            }

            _pending = false;
            return true;
        }
    }
}
