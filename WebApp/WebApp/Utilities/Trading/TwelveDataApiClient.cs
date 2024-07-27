using static System.Net.WebRequestMethods;
using Newtonsoft.Json.Linq;
using System.Globalization;
namespace WebApp.Utilities.Trading
{
    public class TwelveDataApiClient
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;
        private readonly RateLimiter _rateLimiter;
        /// <summary>
        /// Initializes a new instance of the <see cref="TwelveDataApiClient"/> class with the specified API key.
        /// </summary>
        /// <param name="apiKey">The API key used to authenticate with the Twelve Data API.</param>
        public TwelveDataApiClient(string apiKey)
        {
            _client = new HttpClient();
            _apiKey = apiKey;
            _rateLimiter = new RateLimiter(5000); // Assuming a rate limit of 60 requests per minute
        }
        private string AdjustInterval(string interval)
        {
            return interval switch
            {
                "1D" => "1day",
                "1W" => "1week",
                "1M" => "1month",
                _ => interval
            };
        }
        /// <summary>
        /// Retrieves time series data for the specified symbol from the Twelve Data API.
        /// </summary>
        /// <param name="symbol">The symbol for which to retrieve time series data.</param>
        /// <returns>A <see cref="JArray"/> containing the time series data.</returns>
        public async Task<JArray> GetTimeSeries(string symbol, string interval, DateTime? startDate = null)
        {
            interval = AdjustInterval(interval);
            string apiUrl = "";

            if (startDate.HasValue)
            {
                 apiUrl = $"https://api.twelvedata.com/time_series?{startDate.Value:yyyy-MM-dd}&symbol={symbol}&interval={interval}&timezone=America/New_York&outputsize=5000&apikey={_apiKey}";
            }
            else
            {
                return new JArray();
            }

            try
            {
                HttpResponseMessage response = await _client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    // Extract the values array
                    JArray values = (JArray)data["values"]!;
                    return values;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return new JArray();
        }



        /// <summary>
        /// Retrieves time series data for the specified symbol from the Twelve Data API.
        /// </summary>
        /// <param name="symbol">The symbol for which to retrieve time series data.</param>
        /// <param name="interval">The interval for the time series data.</param>
        /// <param name="startDate">The start date for the time series data.</param>
        /// <param name="endDate">The end date for the time series data.</param>
        /// <returns>A <see cref="JArray"/> containing the time series data.</returns>
        public async Task<JArray> GetTimeSeriesEndDate(string symbol, string interval, DateTime? endDate = null)
        {
            interval = AdjustInterval(interval);
            string apiUrl = "";

            if (endDate.HasValue)
            {
                apiUrl = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval={interval}&end_date={endDate.Value:yyyy-MM-dd}&timezone=America/New_York&outputsize=5000&apikey={_apiKey}";
            }
            else
            {
                apiUrl = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval={interval}&&timezone=America/New_York&outputsize=5000&apikey={_apiKey}";
            }

            try
            {
                HttpResponseMessage response = await _client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    // Extract the values array
                    JArray values = (JArray)data["values"]!;
                    return values;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return new JArray();
        }




        /// <summary>
        /// Retrieves time series data for the specified symbol from the Twelve Data API.
        /// </summary>
        /// <param name="symbol">The symbol for which to retrieve time series data.</param>
        /// <returns>A <see cref="JArray"/> containing the time series data.</returns>
        public async Task<JArray> GetTimeSeriesSmallOutput(string symbol, string interval, DateTime? startDate = null)
        {
            interval = AdjustInterval(interval);
            string apiUrl = "";

            if (startDate.HasValue)
            {
                apiUrl = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval={interval}&timezone=America/New_York&outputsize=30&apikey={_apiKey}";
            }
            else
            {
                return new JArray();
            }

            try
            {
                HttpResponseMessage response = await _client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    // Extract the values array
                    JArray values = (JArray)data["values"]!;
                    return values;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return new JArray();
        }

        /// <summary>
        /// Retrieves time series data for the specified symbol from the Twelve Data API.
        /// </summary>
        /// <param name="symbol">The symbol for which to retrieve time series data.</param>
        /// <returns>A <see cref="JArray"/> containing the time series data.</returns>
        public async Task<DateTime?> GetEarliestTimestamp(string symbol, string interval)
        {
            interval = AdjustInterval(interval);
            string apiUrl = $"https://api.twelvedata.com/earliest_timestamp?symbol={symbol}&interval={interval}&apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    // Extract the earliest timestamp
                    string earliestTimestampStr = data["datetime"]?.ToString();
                    if (!string.IsNullOrEmpty(earliestTimestampStr))
                    {
                        DateTime earliestTimestamp = DateTime.ParseExact(earliestTimestampStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        return earliestTimestamp;
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null;
        }


        /// <summary>
        /// Retrieves the API usage details, including the number of used and remaining credits.
        /// </summary>
        /// <returns>A tuple containing the number of used and remaining credits.</returns>
        /// <author>Antoine Bélanger</author>
        public async Task<(int used, int remaining)> GetApiUsageAsync()
        {
            string apiUrl = $"https://api.twelvedata.com/api_usage?apikey={_apiKey}";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    int usedCredits = data["current_usage"].Value<int>();
                    int limit = data["plan_limit"].Value<int>();

                    int remainingCredits = limit - usedCredits;
                    return (usedCredits, remainingCredits);
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return (0, 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, 0);
            }
        }

    }




}
