namespace WebApp.Utilities.Trading
{
    public interface IPriceUpdater
    {
        void RegisterObserver(IPriceObserver observer);
        void RemoveObserver(IPriceObserver observer);
        Task NotifyObserversAsync(string symbol, float bid, float ask);
    }
}
