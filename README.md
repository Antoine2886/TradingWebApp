# Trading Web Application

## Overview
This project is a comprehensive trading web application built using ASP.NET Core and various third-party APIs and libraries. The application supports user authentication, data fetching from Twelve Data, live data updates, charting with multiple drawing tools, a buy/sell system with order management, and community features such as posting charts, comments, followers, friends, and account management.

## Features
### User Authentication

#### Login
- Secure login functionality with password hashing.
- Session management to keep users logged in.

#### Register
- User registration with email verification.
- Sends a confirmation email with a verification token.

#### Token Generator
- Generates tokens for email verification and password reset.
- Tokens are single-use and valid for 10 minutes.

#### Email Verification
- Validates user email addresses.
- Ensures the user receives a verification token.

#### Forgot Password/Reset Password
- Allows users to reset their password via email.
- Sends a password reset link with a token valid for 10 minutes.

### Twelve Data API Integration

#### Twelve Data API
- Connects to Twelve Data API to fetch real-time and historical stock data.
- Requires at least the first paid tier for complete functionality.

#### Fetching Historical Data
- Retrieves historical stock data and stores it in the database.
- Recursive fetching with a reasonable maximum limit.

#### Live Data Updates
- Continuously fetches live stock data.
- Updates the database every minute with the latest data.

### Chart System
- Supports up to 3 charts per user.
- Allows up to 10 drawings per chart.
- Save and load chart configurations and drawings.

### Rate Limiter
- Limits the number of requests per specific IP address.
- Manages API request limits and queues excess requests.

### Request Logging
- Logs the execution time of each request.
- Helps in debugging and performance optimization.

### Buy and Sell System
- Supports placing buy and sell orders with take profit and stop loss.
- Manages pending and completed orders.

### Batch Controller
- Optimizes batch requests to improve performance.
- Handles multiple requests efficiently.

### Money Converter
- Converts currency for trading purposes.
- Ensures accurate transactions based on current exchange rates.

### Test Project
- Includes a test project for testing various functionalities. (not up to date)
- Ensures the application works as expected. (not up to date)

### Community Features

#### Posting Charts
- Users can post their charts along with comments.

#### Comments
- Users can comment on charts and posts.

#### Followers and Friends
- Users can follow other users and add friends.

#### Settings
- Users can manage their account settings.

#### Deleting Account
- Users can delete their account with email confirmation.

## Getting Started

## Prerequisites
- .NET Core SDK
### SQL Server

This application uses MySQL for the database. Ensure you have the following prerequisites installed:

- **MySQL Server**: Version 8.0 or higher
- **Entity Framework Core**: To interact with the database

In your `Program.cs`, configure the MySQL connection as follows:

```csharp
builder.Services.AddDbContextFactory<Context>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 3)));
});
``` 
You can change the type of database used here 

- Twelve Data API key (requires at least the first paid tier for full functionality)
- Stripe API keys for payment processing (not functionnal)

## Installation

### 1. Clone the Repository
```bash
#### Using Visual Studio

1. Clone the repository:
    ```sh
    git clone https://github.com/Antoine2886/TradingWebApp.git
    cd TradingWebApp
    ```

2. Open the solution file (`.sln`) in Visual Studio.

3. Create a `.env` file in the root directory with the following content:
    ```env
    TWELVE_DATA_API_KEY=xxx
    TWELVE_DATA_WS_KEY=xxx
    DefaultConnection=Server=localhost;Port=xxx;Database=xxx;Uid=xxx;Pwd=xxx;
    EMAIL_ADDRESS=xxx
    EMAIL_PASSWORD=xxx
    ```

4.  Configure `appsettings.json` with your Stripe keys (The app will launch even if you don't put your stripe keys): 
    ```json
    "Stripe": {
      "SecretKey": "xxx",
      "PublishableKey": "xxx"
    }
    ```

5. Open the Package Manager Console in Visual Studio:
    - Go to `Tools` > `NuGet Package Manager` > `Package Manager Console`.

6. Run the following commands in the Package Manager Console to set up the database:
    ```powershell
    Add-Migration InitialCreate
    Update-Database
    ```

7. To start the application, click the play button (green arrow) at the top of Visual Studio.

#### Using Command Line

1. Clone the repository:
    ```sh
    git clone https://github.com/Antoine2886/TradingWebApp.git
    cd TradingWebApp
    ```

2. Create a `.env` file in the root directory with the following content:
    ```env
    TWELVE_DATA_API_KEY=xxx
    TWELVE_DATA_WS_KEY=xxx
    DefaultConnection=Server=localhost;Port=xxx;Database=xxx;Uid=xxx;Pwd=xxx;
    EMAIL_ADDRESS=xxx
    EMAIL_PASSWORD=xxx
    ```

3. Configure `appsettings.json` with your Stripe keys (The app will launch even if you don't put your stripe keys):
    ```json
    "Stripe": {
      "SecretKey": "xxx",
      "PublishableKey": "xxx"
    }
    ```

4. Run the following commands to set up the database:
    ```sh
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

5. Build and run the application:
    ```sh
    dotnet build
    dotnet run
    ```


Fetching Historical Data
To fetch historical data, ensure you have at least the first paid tier of Twelve Data (will still kinda work with the free tier). Uncomment the following line in your TradeService file -> ConnectWebSocket method:

//await InitializeDataLoading();

```
## Available API Endpoints

### Account Management
- POST /api/account/register: Register a new user
- POST /api/account/login: Login an existing user
- POST /api/account/forgot-password: Request a password reset
- POST /api/account/reset-password: Reset the user's password

### Trading
- GET /api/trade/historical: Fetch historical data
- GET /api/trade/latest: Fetch the latest data
- POST /api/trade/place-order: Place a new order
- POST /api/trade/execute-order: Execute an existing order
- GET /api/trade/orders: Fetch all orders
- GET /api/trade/symbols: Get the list of available trading symbols
- GET /api/trade/bid-ask: Get bid and ask prices for a given symbol
- GET /api/trade/balance: Get the balance for the logged-in user

### Batch Requests
- GET /api/batch/batch: Fetch batch data including orders, bid-ask data, and balance

### Community
- POST /api/post/create: Create a new post with chart and comments
- GET /api/post/{postId}: Get details of a post by ID
- POST /api/comment/create: Create a new comment on a post
- GET /api/user/followers: Get a list of followers
- POST /api/user/follow: Follow a user
- POST /api/user/unfollow: Unfollow a user
- POST /api/user/add-friend: Add a user as a friend
- POST /api/user/remove-friend: Remove a user from friends
- POST /api/user/delete-account: Delete the user account with email confirmation
- POST /api/post/handle-likes: Handle liking and unliking of posts

### Home
- GET /home/index: Retrieves and displays the home page for the logged-in user
- GET /home/profile: Displays the profile of the user with the specified visible name
- GET /home/privacy: Displays the privacy policy page
- GET /home/error: Displays the error page with the request ID
- POST /home/follow: Handles the follow action, allowing the logged-in user to follow another user
- POST /home/sendfriendrequest: Sends a friend request from the logged-in user to another user
- GET /home/acceptfriendrequest: Accepts a friend request from another user
- POST /home/ignorefriendrequest: Ignores a friend request from another user

### Settings
- GET /settings/index: Displays the user settings page
- POST /settings/makechanges: Handles user settings updates including username and profile picture
- POST /settings/updateusername: Updates the username of the logged-in user
- POST /settings/updateprofilepicture: Updates the profile picture of the logged-in user
- POST /settings/deleteaccount: Initiates account deletion process for the logged-in user
- POST /settings/confirmdeleteaccount: Confirms account deletion for the user
- GET /settings/billing: Displays the billing information page

### Payments
- GET /api/payments/config: Provides the Stripe configuration needed for client-side setup
- POST /api/payments/create-checkout-session: Creates a checkout session for the user
- GET /api/payments/checkout-session: Retrieves details of a specific checkout session
- GET /api/payments/success: Handles successful subscription creation
- POST /api/payments/customer-portal: Creates a billing portal session for the user
- POST /api/payments/webhook: Webhook endpoint to handle Stripe events
- POST /api/payments/cancel-subscription: Cancels the user's subscription



### Additional Information
Important Notes
Twelve Data API: You need at least the first paid tier of Twelve Data for full functionality, including fetching historical data.
Stripe: Ensure your Stripe API keys are correctly configured in appsettings.json.
Logs and Debugging
This project includes logging for request execution times and error handling to help with debugging.


## Collaboration

### Contributions

Currently, I am not accepting contributions directly as I do not have the time to review and manage all the pull requests. However, if someone is interested and willing to manage the contributions, including reviewing pull requests and ensuring the quality of the code, I would be open to considering it. 

If you are interested in taking on this role, please email me at [antoineblanger@gmail.com](mailto:antoineblanger@gmail.com) to discuss further.

Thank you for your understanding and interest in contributing to the project!


### Acknowledgements
- **TradingView Charting Library:** This project uses the TradingView Charting Library for chart functionalities. You can learn more about it [here](https://www.tradingview.com/HTML5-stock-forex-bitcoin-charting-library/).
- **Twelve Data API:** Used for fetching real-time and historical stock data. More information can be found [here](https://twelvedata.com/).
- **Stripe:** Used for handling payments. More information can be found [here](https://stripe.com/).
- **ASP.NET Core:** The web application framework used for this project. More information can be found [here](https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0).

### Contact
For any inquiries or support, please contact [antoineblanger@gmail.com](mailto:antoineblanger@gmail.com). I may or may not answer depending on the question.

### License
This project is licensed under the MIT License.
