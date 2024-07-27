namespace WebApp.Utilities.Trading
{
    public interface IPriceObserver
    {
        Task OnPriceUpdateAsync(string symbol, float bid, float ask);
    }

}
