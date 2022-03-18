// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace StripeApi.Controllers
{
    public class BillingController : Controller
    {
        private readonly IOptions<StripeSettings> _options;
        private readonly StripeBiling _stripeBiling;

        public BillingController(IOptions<StripeSettings> options, StripeBiling stripeBiling)
        {
            _options = options;
            StripeConfiguration.ApiKey = options.Value.PrivateKey;
            _stripeBiling = stripeBiling;
        }
        [HttpPost("create-customer-portal-session")]
        public async Task<IActionResult> CustomerPortal(StripeSubscriberRequest req)
        {
            var customer = _stripeBiling.CheckIfCustomerExistsorCreateOneAsync(req);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> {
                    "card",
                },
                Customer = customer.Result,
                SuccessUrl = "https://localhost:44324/swagger/index.html",
                CancelUrl = "https://localhost:44304/",
                Mode = "subscription",
                Locale = "hr",
                LineItems = new List<SessionLineItemOptions>
                    {
                     new SessionLineItemOptions
                      {
                       Price = req.Price,
                       Quantity = 1,
                       },
                    },
                AllowPromotionCodes = true,
            };
            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return Ok(session.Url);
        }

        [HttpPost("cancel-subscription")]
        public async Task<ActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest req)
        {
            var service = new SubscriptionService();
            //var cancelOptions = new SubscriptionCancelOptions
            //{
            //    InvoiceNow = false,
            //    Prorate = false,
            //};
            var cancelOptions = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true,

            };
            var subscription = await service.UpdateAsync(req.Subscription, cancelOptions);

            return Ok(subscription);
        }

        [HttpPost("update-subscription")]
        public async Task<IActionResult> UpdateSubscription([FromBody] UpdateSubscriptionRequest req)
        {
            var service = new SubscriptionService();
            var subscription = await service.GetAsync(req.Subscription);

            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = Environment.GetEnvironmentVariable(req.NewPrice.ToUpper()),
                    }
                }
            };
            var updatedSubscription = await service.UpdateAsync(req.Subscription, options);
            return Ok(updatedSubscription);
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> ListSubscriptions(string email)
        {
            var customerId = _stripeBiling.ListSubscriptionAsync(email);

            var options = new SubscriptionListOptions
            {
                Customer = customerId.Result,
                Status = "active",
            };
            options.AddExpand("data.default_payment_method");
            var service = new SubscriptionService();
            var subscriptions = await service.ListAsync(options);

            return Ok(subscriptions);
        }

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
                    _options.Value.WHSecret
                );
                Console.WriteLine($"Webhook notification with type: {stripeEvent.Type} found for {stripeEvent.Id}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something failed {e}");
                return BadRequest();
            }

            if (stripeEvent.Type == "invoice.payment_succeeded")
            {
                var invoice = stripeEvent.Data.Object as Invoice;

                if (invoice.BillingReason == "subscription_create")
                {

                    // Retrieve the payment intent used to pay the subscription
                    var service = new PaymentIntentService();
                    var paymentIntent = service.Get(invoice.PaymentIntentId);

                    // Set the default payment method
                    var options = new SubscriptionUpdateOptions
                    {
                        DefaultPaymentMethod = paymentIntent.PaymentMethodId,
                    };
                    var subscriptionService = new SubscriptionService();
                    subscriptionService.Update(invoice.SubscriptionId, options);

                    Console.WriteLine($"Default payment method set for subscription: {paymentIntent.PaymentMethodId}");
                }
                Console.WriteLine($"Payment succeeded for invoice: {stripeEvent.Id}");
            }

            if (stripeEvent.Type == "invoice.paid")
            {
                // Used to provision services after the trial has ended.
                // The status of the invoice will show up as paid. Store the status in your
                // database to reference when a user accesses your service to avoid hitting rate
                // limits.
            }
            if (stripeEvent.Type == "invoice.payment_failed")
            {
                // If the payment fails or the customer does not have a valid payment method,
                // an invoice.payment_failed event is sent, the subscription becomes past_due.
                // Use this webhook to notify your user that their payment has
                // failed and to retrieve new card details.
            }
            if (stripeEvent.Type == "invoice.finalized")
            {
                // If you want to manually send out invoices to your customers
                // or store them locally to reference to avoid hitting Stripe rate limits.
            }
            if (stripeEvent.Type == "customer.subscription.deleted")
            {
                // handle subscription cancelled automatically based
                // upon your subscription settings. Or if the user cancels it.
            }
            if (stripeEvent.Type == "customer.subscription.trial_will_end")
            {
                // Send notification to your user that the trial will end
            }

            return Ok();
        }
        [HttpGet("subscribers")]
        public async Task<List<Subscriber>> GetSubscriber()
        {
            return await _stripeBiling.GetSubscribers();
        }
    }
}
