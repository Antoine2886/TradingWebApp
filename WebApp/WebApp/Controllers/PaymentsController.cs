using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace Bd.Infrastructure;

/// <summary>
/// Author: Sphero
/// Description: Controller for handling payments and subscription management using Stripe.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : Controller
{
    private readonly IOptions<StripeOptions> options;
    private readonly IStripeClient client;
    private readonly UserManager<AppUser> _userManager;
    private readonly Context _context;

    public PaymentsController(IOptions<StripeOptions> options, Context context, UserManager<AppUser> userManager)
    {
        this.options = options;
        this.client = new StripeClient(this.options.Value.SecretKey);
        _context = context;
        _userManager = userManager;
    }
    /// <summary>
    /// Provides the Stripe configuration needed for client-side setup.
    /// </summary>
    [HttpGet("config")]
    public ConfigResponse Setup()
    {
        return new ConfigResponse
        {
            ProPrice = this.options.Value.ProPrice,
            BasicPrice = this.options.Value.BasicPrice,
            PublishableKey = this.options.Value.PublishableKey,
        };
    }

    /// <summary>
    /// Creates a checkout session for the user.
    /// </summary>
    [HttpPost("create-checkout-session")]
    public ActionResult Create()
    {
        StripeConfiguration.ApiKey = "sk_test_51PR4Sp1P2nEddnYBxPkmh5FsMJ8YE76vXupWsUO0Wah3AVW7iaUlA5gWy8irbeG98sbgTM1hYTPcsdemqnbL2WQJ00YrIG1tfO";

        var domain = "https://localhost:7183";

        var lookupKey = Request.Form["lookup_key"];

        if (string.IsNullOrEmpty(lookupKey))
        {
            return BadRequest("lookup_key is required.");
        }

        var service2 = new PriceService();
        var hi = service2.Get(lookupKey);
        var correct_price = hi.UnitAmount / 100.0;

        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = lookupKey,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = $"{domain}/api/Payments/success?session_id={{CHECKOUT_SESSION_ID}}",  // for now sessionId will be equal to the price
            CancelUrl = $"{domain}/api/Payments/cancel.html",
        };
        var service = new SessionService();
        Session session = service.Create(options);

        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }
    /// <summary>
    /// Retrieves details of a specific checkout session.
    /// </summary>
    [HttpGet("checkout-session")]
    public async Task<IActionResult> CheckoutSession(string sessionId)
    {
        var service = new SessionService(this.client);
        var session = await service.GetAsync(sessionId);
        return Ok(session);
    }

    /// <summary>
    /// Handles successful subscription creation.
    /// </summary>
    [HttpGet("success")]
    public async Task<IActionResult> Success(string session_id) // identifying subscription based on the price
    {
        var service = new SessionService();
        Session session = service.Get(session_id);

        AppUser user = await getUser();
        /*
        var customer_options = new CustomerCreateOptions
        {
            Name = user.VisibleName,
            Email = user.UserName,   
        };

        var customer_service = new CustomerService();
        var x = customer_service.Create(customer_options);    

        var options = new SubscriptionCreateOptions
        {
            Customer = x.Id,
            Items = new List<SubscriptionItemOptions>
            {
        new SubscriptionItemOptions { Price = session.data.price.Id },
            },
            DefaultPaymentMethod = "card"
        };
        var service = new SubscriptionService();
        service.Create(options);

        var price_service = new PriceService();
        var hi = price_service.Get(priceId);
        var price = (double)(hi.UnitAmount / 100.0);
        */
        Subscription subscription = null;
        var price = 5;
        if (price == 5)
        {
            subscription = new Subscription
            {
                Name = "basic",
                Description = "the basic tier with some access",
                Price = price,
                StartDate = DateTime.Now,
                User = await getUser(),
            };
        }
        else if (price == 10)
        {
            subscription = new Subscription
            {
                Name = "middle",
                Description = "the middle tier with some access",
                Price = price,
                StartDate = DateTime.Now,
                User = await getUser(),
            };
        }
        else
        {
            subscription = new Subscription
            {
                Name = "pro",
                Description = "the pro tier with some access",
                Price = price,
                StartDate = DateTime.Now,
                User = user
            };
        }

        _context.Subscriptions.Add(subscription);
        
        user.Subscription = subscription;

        _context.Users.Update(user);

        await _context.SaveChangesAsync();
           

        return RedirectToAction("Index", "Settings");
    }

    /// <summary>
    /// Creates a billing portal session for the user.
    /// </summary>
    [HttpPost("customer-portal")]
    public async Task<IActionResult> CustomerPortal(string sessionId)
    {
        var checkoutService = new SessionService(this.client);
        var checkoutSession = await checkoutService.GetAsync(sessionId);

        var returnUrl = this.options.Value.Domain;

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = checkoutSession.CustomerId,
            ReturnUrl = returnUrl,
        };
        var service = new Stripe.BillingPortal.SessionService(this.client);
        var session = await service.CreateAsync(options);

        return Redirect(session.Url);
    }

    /// <summary>
    /// Webhook endpoint to handle Stripe events.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                this.options.Value.WebhookSecret
            );
            Console.WriteLine($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Something failed {e}");
            return BadRequest();
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            Console.WriteLine($"Session ID: {session.Id}");
        }

        return Ok();
    }
    /// <summary>
    /// Cancels the user's subscription.
    /// </summary>
    public async Task<IActionResult> CancelSubscription(string subscriptionID)
    {
        var options = new SubscriptionUpdateOptions { CancelAtPeriodEnd = true };
        var service = new SubscriptionService();
        service.Update("{{SUBSCRIPTION_ID}}", options);


        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

        AppUser user = await _context.Users.Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Id == userId);
        user.Subscription = null; 
        _context.Users.Update(user);

        return RedirectToAction("Index", "Setting");
    }

    public async Task<AppUser> getUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

        AppUser user = await _userManager.FindByIdAsync(userId.ToString());

        return user;
    }
}

