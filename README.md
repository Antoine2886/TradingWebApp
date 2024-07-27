# Trading Web Application

## Overview
This project is a comprehensive trading web application built using ASP.NET Core and various third-party APIs and libraries. The application supports user authentication, data fetching from Twelve Data, live data updates, charting with multiple drawing tools, a buy/sell system with order management, and community features such as posting charts, comments, followers, friends, and account management.

## Features
### User Authentication
- **Login**: Secure login system with email and password.
- **Register**: Allows users to create a new account with email verification.
- **Token Generator**: Generates single-use tokens for email verification and password reset, valid for 10 minutes.
- **Email Verification**: Confirms the user's email address with a token sent via email.
- **Forgot Password/Reset Password**: Allows users to reset their password using a token sent via email, valid for 10 minutes.

### API Integration
- **Twelve Data API**: Connects to the Twelve Data API for fetching real-time and historical stock data. Requires at least the first paid tier for full functionality.

### Data Handling
- **Historical Data Fetching**: Retrieves historical stock data and stores it in the database.
- **Live Data Updates**: Continuously fetches live stock data and updates the database every minute.
- **Symbol Manager**: Manages the symbols being tracked and ensures data consistency.

### Charting System
- **Multiple Charts**: Supports up to 3 charts per user.
- **Drawing Tools**: Allows up to 10 drawings per chart, including lines and rectangles.
- **Saving/Loading**: Saves and loads chart configurations and drawings for each user.

### Rate Limiter
- **IP Rate Limiter**: Limits the number of requests per specific IP address.
- **API Rate Limiter**: Limits the number of API requests and queues them if the limit is exceeded.

### Request Logging
- **Execution Time Logging**: Logs the time taken to execute each request for debugging purposes.

### Trading System
- **Buy and Sell Orders**: Supports placing buy and sell orders with take profit, stop loss, and pending orders.
- **Order Management**: Manages open, pending, and completed orders with real-time updates.

### Batch Controller
- **Batch Requests**: Optimizes batch requests for improved performance.

### Money Converter
- **Currency Conversion**: Converts currencies for trading, ensuring accurate transactions.

### Community Features
- **Posting Charts**: Users can post their charts along with comments.
- **Comments**: Users can comment on charts and posts.
- **Followers and Friends**: Users can follow other users and add friends.
- **Settings**: Users can manage their account settings.
- **Deleting Account**: Users can delete their account with email confirmation.

## Prerequisites
- .NET SDK
- SQL Server
- Twelve Data API key (requires at least the first paid tier for full functionality)
- Stripe API keys for payment processing

## Installation

### 1. Clone the Repository
```bash
git clone https://github.com/Antoine2886/TradingWebApp.git
cd TradingWebApp
2. Create a .env File
Create a .env file in the root directory with the following content:

makefile
Copier le code
TWELVE_DATA_API_KEY=your_twelve_data_api_key
TWELVE_DATA_WS_KEY=your_twelve_data_ws_key
DefaultConnection=Server=localhost;Port=your_port;Database=your_database;Uid=your_uid;Pwd=your_pwd;
EMAIL_ADDRESS=your_email_address
EMAIL_PASSWORD=your_email_password
3. Configure appsettings.json
Update the appsettings.json file with your Stripe API keys:

json
Copier le code
"Stripe": {
  "SecretKey": "your_stripe_secret_key",
  "PublishableKey": "your_stripe_publishable_key"
}
4. Database Setup
Run the following commands to set up the database:

bash
dotnet ef migrations add InitialCreate
dotnet ef database update
Usage
Running the Application
To run the application, use the following command:

bash
dotnet run
Fetching Historical Data
To fetch historical data, ensure you have at least the first paid tier of Twelve Data. Uncomment the following line in your ConnectWebSocket method:

csharp
await InitializeDataLoading();
API Endpoints
User Authentication

POST /api/account/register: Register a new user
POST /api/account/login: Login an existing user
POST /api/account/forgot-password: Request a password reset
POST /api/account/reset-password: Reset the user's password
Trading

GET /api/trade/historical: Fetch historical data
GET /api/trade/latest: Fetch the latest data
POST /api/trade/place-order: Place a new order
POST /api/trade/execute-order: Execute an existing order
GET /api/trade/orders: Fetch all orders
Batch Requests

GET /api/batch/batch: Fetch batch data
Community

POST /api/post/create: Create a new post with chart and comments
GET /api/post/{postId}: Get details of a post by ID
POST /api/comment/create: Create a new comment on a post
GET /api/user/followers: Get a list of followers
POST /api/user/follow: Follow a user
POST /api/user/unfollow: Unfollow a user
POST /api/user/add-friend: Add a user as a friend
POST /api/user/remove-friend: Remove a user from friends
POST /api/user/delete-account: Delete the user account with email confirmation
Additional Information
Important Notes
Twelve Data API: You need at least the first paid tier of Twelve Data for full functionality, including fetching historical data.
Stripe: Ensure your Stripe API keys are correctly configured in appsettings.json.
Logs and Debugging
This project includes logging for request execution times and error handling to help with debugging.

Contact
For any inquiries or support, please contact antoineblanger@gmail.com. I may or may not answer depending on the question.

Detailed Description of Implemented Features
1. Login
Secure login functionality with password hashing.
Session management to keep users logged in.
2. Register
User registration with email verification.
Sends a confirmation email with a verification token.
3. Token Generator
Generates tokens for email verification and password reset.
Tokens are single-use and valid for 10 minutes.
4. Email Verification
Validates user email addresses.
Ensures the user receives a verification token.
5. Forgot Password/Reset Password
Allows users to reset their password via email.
Sends a password reset link with a token valid for 10 minutes.
6. Twelve Data API Integration
Connects to Twelve Data API to fetch real-time and historical stock data.
Requires at least the first paid tier for complete functionality.
7. Fetching Historical Data
Retrieves historical stock data and stores it in the database.
Recursive fetching with a reasonable maximum limit.
8. Live Data Updates
Continuously fetches live stock data.
Updates the database every minute with the latest data.
9. Chart System
Supports up to 3 charts per user.
Allows up to 10 drawings per chart.
Save and load chart configurations and drawings.
10. Rate Limiter
Limits the number of requests per specific IP address.
Manages API request limits and queues excess requests.
11. Request Logging
Logs the execution time of each request.
Helps in debugging and performance optimization.
12. Buy and Sell System
Supports placing buy and sell orders with take profit and stop loss.
Manages pending and completed orders.
13. Batch Controller
Optimizes batch requests to improve performance.
Handles multiple requests efficiently.
14. Money Converter
Converts currency for trading purposes.
Ensures accurate transactions based on current exchange rates.
15. Test Project
Includes a test project for testing various functionalities.
Ensures the application works as expected.
16. Community Features
Posting Charts: Users can post their charts along with comments.
Comments: Users can comment on charts and posts.
Followers and Friends: Users can follow other users and add friends.
Settings: Users can manage their account settings.
Deleting Account: Users can delete their account with email confirmation.
License
This project is licensed under the MIT License.
