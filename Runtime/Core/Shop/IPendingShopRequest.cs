namespace _Project.Core.Shop
{
    public interface IPendingShopRequest
    {
        void Request();

        bool Consume();
    }
}
