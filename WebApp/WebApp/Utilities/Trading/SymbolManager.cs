namespace WebApp.Utilities.Trading
{
    using System.Collections.Generic;

    /// <summary>
    /// Manages a list of trading symbols.
    /// </summary>
    /// <author>Antoine Bélanger</author
    public class SymbolManager
    {
        /// <summary>
        /// Gets the list of available trading symbols.
        /// </summary>
        public List<string> Symbols { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolManager"/> class.
        /// </summary>
        /// <author>Antoine Bélanger</author>
        public SymbolManager()
        {
            LoadSymbols();
        }


        /// <summary>
        /// Loads the list of trading symbols.
        /// </summary>
        /// <author>Antoine Bélanger</author>
        private void LoadSymbols()
        {
            //Symbols = new List<string> { "BTC/USD","USD/CAD" };
            Symbols = new List<string> { "BTC/USD","USD/CAD" };
            // Add more symbols as needed
        }
    }

}
