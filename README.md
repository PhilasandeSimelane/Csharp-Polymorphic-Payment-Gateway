# Csharp-Polymorphic-Payment-Gateway
# C# Polymorphic Payment Gateway (Live API Integration)

##  Project Overview: Polymorphic Payment Processor

This application simulates a scalable, professional-grade checkout system capable of processing multiple, disparate payment methods using modern C# architecture.

The core challenge of this project was integrating fast, static logic (Visa) with **slow, volatile external data** (Bitcoin exchange rates) without crashing the application or making the code complex.

## Key Architectural Concepts Demonstrated

This project is not a simple script; it is a demonstration of structural C# mastery:

1.  **Interface Abstraction (IPaymentProcessor):** Defines a strict contract that both Visa and Bitcoin must adhere to, proving the system is scalable.
2.  **Polymorphism:** Demonstrates the ability to hold both the `VisaPayment` and `BitcoinPayment` classes in a single `List<IPaymentProcessor>`, allowing the system to iterate and call `ExecuteTransaction` without knowing the specific underlying type.
3.  **Asynchronous Programming (async/await):** Essential for performance. The `BitcoinPayment` service is treated as a "slow" task, preventing the entire application UI from freezing while waiting for the API to respond.
4.  **Dependency Injection (DI):** The `BitcoinPayment` class does not know *how* to get the exchange rate; it only demands a tool (`IExchangeRateService`). The core program **injects** the specific tool (`CoinDeskApiService`) at runtime, making the code incredibly easy to maintain and test.
5.  **Localization/Culture Handling:** The application correctly uses **`CultureInfo.InvariantCulture`** for parsing data and **`CultureInfo.CreateSpecificCulture("en-US")` / `("en-ZA")`** for unambiguous currency output.

##  How to Run

1.  Clone this repository.
2.  Ensure you are running on **.NET 8.0** or higher.
3.  Execute the project using `dotnet run` or **Ctrl+F5** in Visual Studio.
