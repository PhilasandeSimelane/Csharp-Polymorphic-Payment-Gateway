
using System;
using System.Globalization;
using System.Net.Http;        // Unlocks HttpClient (the "truck")
using System.Text.Json;     // Unlocks JsonSerializer (the "translator")
using System.Text.Json.Nodes;
using System.Threading.Tasks; 
using System.Collections.Generic; 
using System.Threading.Tasks;
using System.Dynamic;


namespace PaymentSystem
{


    public class Program
    {
        // Must be async Task Main to allow the program to "await" the slow internet call
        public static async Task Main(string[] args)
        {
            // 1. Create the API Tool (The Service)
            // This is the implementation of the contract (the Employee)
            IExchangeRateService apiService = new CoinDeskApiService();
            CurrencyConversion forexService = new FrankfurterApiService();

            // 2. Create payment methods.
            VisaPayment visa = new VisaPayment("4000111122223333", "123", 2028m);

            // 3. Dependency Injection: We INJECT the API service (the tool) 
            //    into the Bitcoin class when we create it.
            BitcoinPayment btc = new BitcoinPayment("1K6F89H4J2K1L3M4N5P6Q7R8S9T0U1V2W3X4", apiService,forexService);

            // --- POLYMORPHISM TEST ---
            // 4. We combine two different classes into one list of the Interface Type (IPaymentProcessor)
            List<IPaymentProcessor> paymentMethods = new List<IPaymentProcessor>
            {
                visa,
                btc
            };

            decimal chargeAmount = 5000.00m; // The transaction amount

            foreach (IPaymentProcessor processor in paymentMethods)
            {
                Console.WriteLine($"\n--- Attempting to process using: {processor.GetType().Name} ---");

                if (processor.ValidateDetails())
                {
                    // 5. We MUST "await" the transaction here because the contract 
                    //    returns a Task (it might be slow).
                    string transactionId = await processor.ExecuteTransaction(chargeAmount);

                    Console.WriteLine($"Result: SUCCESS! Transaction ID: {transactionId}");
                }
                else
                {
                    Console.WriteLine("Result: FAILURE: Validation failed.");
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // CONTRACTS (The Interfaces)
    // ------------------------------------------------------------------

    // Defines the contract for any class that processes payments
    public interface IPaymentProcessor
    {
        bool ValidateDetails();
        Task<string> ExecuteTransaction(decimal amount);
    }

    // Defines the contract for any class that provides live exchange rates
    public interface IExchangeRateService
    {
        Task<decimal> GetCurrentRate(string currencyPair);
        
        
    }


    // ------------------------------------------------------------------
    // IMPLEMENTATIONS (The Classes)
    // ------------------------------------------------------------------

    public class CoinDeskApiService : IExchangeRateService
    {

        private readonly HttpClient _httpClient = new HttpClient();
        // This is the "Golden Ticket" code you built. It gets the live price.
        public async Task<decimal> GetCurrentRate(string currencyPair)
        {
            string currency = currencyPair.ToUpper();
            // The URL is built dynamically using the currency pair input
            string apiUrl = "https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT";

            try
            {
                // The "Truck" goes to the internet
                string jsonResponse = await _httpClient.GetStringAsync(apiUrl);

                // The "Hacker Fix" - Using JsonNode to bypass static models
                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonResponse);

                // We drill down dynamically using the currency variable
                string priceString = jsonNode["price"].GetValue<string>();
                decimal liveRate = decimal.Parse(priceString, System.Globalization.CultureInfo.InvariantCulture);

                Console.WriteLine($"Hack complete. The live Bitcoin price is: {liveRate.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))}");

                return liveRate;
            }
            catch (Exception ex)
            {
                // The system handles the error and returns 0 as a failure signal
                Console.WriteLine($"Error: Binance API failed. {ex.Message}");
                return 0;
            }
        }
    }
    //Currency Conversion from USD to ZAR
    public interface CurrencyConversion
    {
        Task<decimal> GetExchangeRate(string fromCurrency, string toCurrency);

    }
    public class FrankfurterApiService : CurrencyConversion
    {
        private readonly HttpClient _httpClient = new HttpClient();
        public async Task<decimal> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            // Target API: Frankfurter API (easy, free Forex conversion)
            string apiUrl = $"https://api.frankfurter.app/latest?from={fromCurrency}&to={toCurrency}";

            try
            {
                string jsonResponse = await _httpClient.GetStringAsync(apiUrl);
                var jsonNode = JsonNode.Parse(jsonResponse);

                // We drill down into the 'rates' object dynamically
                // [rates][ZAR]
                decimal rate = (decimal)jsonNode["rates"][toCurrency];

                Console.WriteLine($"[Forex] Fetched rate {fromCurrency} -> {toCurrency}: {rate:F4}");
                return rate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Forex API: {ex.Message}");
                return 0m;
            }
        }
    }
    public class VisaPayment : IPaymentProcessor
    {
        public string CardNumber { get; set; }
        public string CVC { get; set; }
        public decimal ExpirationYear { get; set; }

        public VisaPayment(string number, string cvc, decimal year)
        {
            this.CardNumber = number;
            this.CVC = cvc;
            this.ExpirationYear = year;
        }

        public bool ValidateDetails()
        {
            Console.WriteLine($"[VISA] Checking validity for card ending in {CardNumber.Substring(12)}...");

            if (CardNumber.Length == 16)
            {
                Console.WriteLine("[VISA] Validation Successful.");
                return true;
            }
            Console.WriteLine("[VISA] Validation Failed: Invalid card length.");
            return false;
        }

        // Fulfills the Task<string> contract by wrapping the fast result
        public Task<string> ExecuteTransaction(decimal amount)
        {
            Console.WriteLine($"[VISA] Executing fast charge of {amount:C}...");
            string transactionId = $"VISA_CONFIRMED_TXN_{Guid.NewGuid().ToString().Substring(0, 4)}";
            return Task.FromResult(transactionId);
        }
    }

    public class BitcoinPayment : IPaymentProcessor
    {
        public string WalletAddress { get; set; }

        // Dependency Injection: Holds the tool provided by the Main method
        private readonly IExchangeRateService _rateService;
        // Forex exchange
        private readonly CurrencyConversion _forexService;

        public BitcoinPayment(string address, IExchangeRateService rateService,CurrencyConversion forexService )
        {
            this.WalletAddress = address;
            this._rateService = rateService;
            this._forexService = forexService;
        }

        public bool ValidateDetails()
        {
            Console.WriteLine($"[BTC] Checking validity for address starting with {WalletAddress.Substring(0, 5)}...");

            if (WalletAddress.Length >= 30)
            {
                Console.WriteLine("[BTC] Validation Successful.");
                return true;
            }
            Console.WriteLine("[BTC] Validation Unsuccessful: Invalid wallet address length.");
            return false;
        }

        public async Task<string> ExecuteTransaction(decimal amount)
        {
            Console.WriteLine($"[BTC] Executing charge of {amount:C}...");
            Console.WriteLine("[BTC] Contacting API for live exchange rate...");
           

            // We await the live price from the service!
            decimal exchangeRate = await _rateService.GetCurrentRate("USD");

            if (exchangeRate == 0)
            {
                Console.WriteLine("[BTC] Error: API failed. Transaction cancelled.");
                return "BTC_FAIL_API_ERROR";
            }

            decimal btcAmount = amount / exchangeRate;

            Console.WriteLine($"[BTC] Live rate: {exchangeRate.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))}. Transferring {btcAmount:F10} BTC...");
            decimal zarRate = await _forexService.GetExchangeRate("USD", "ZAR");
            if (zarRate == 0)
            {
                Console.WriteLine("[ZAR] Error: API failed. Conversion cancelled.");
                return "ZAR_FAIL_API_ERROR";
            }
            decimal forexAmount = exchangeRate * zarRate;
            Console.WriteLine($"Converting [BTC] Live rate {exchangeRate.ToString("C", CultureInfo.CreateSpecificCulture("en-US"))} to [ZAR]: {forexAmount.ToString("C", CultureInfo.CreateSpecificCulture("en-ZA"))} at $1 TO ZAR Amount: {zarRate.ToString("C", CultureInfo.CreateSpecificCulture("en-ZA"))}.");
            return $"BTC_CONFIRMED_TXN_{Guid.NewGuid().ToString().Substring(0, 4)}";

            
        }
    }
}
