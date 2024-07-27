namespace WebApp.Utilities.Trading
{
    /// <summary>
    /// Manages the registration and notification of price observers.
    /// </summary>
    /// <author>Antoine Bélanger</author>
    public class PriceUpdater : IPriceUpdater
    {
        private readonly List<IPriceObserver> _observers = new List<IPriceObserver>();

        /// <summary>
        /// Registers a new observer to receive price updates.
        /// </summary>
        /// <param name="observer">The observer to register.</param>
        public void RegisterObserver(IPriceObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
        /// <summary>
        /// Removes an observer from the notification list.
        /// </summary>
        /// <param name="observer">The observer to remove.</param>
        public void RemoveObserver(IPriceObserver observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
            }
        }

        /// <summary>
        /// Notifies all registered observers of a price update.
        /// </summary>
        /// <param name="symbol">The trading symbol.</param>
        /// <param name="bid">The bid price.</param>
        /// <param name="ask">The ask price.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task NotifyObserversAsync(string symbol, float bid, float ask)
        {
            var tasks = _observers.Select(observer => observer.OnPriceUpdateAsync(symbol, bid, ask));
            await Task.WhenAll(tasks);
        }
    }

}
