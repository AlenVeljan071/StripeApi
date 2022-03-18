namespace StripeApi.StripeBilingManager
{
    public class StripeBiling
    {

        private readonly DbInteractor _context;
        public StripeBiling(IOptions<StripeSettings> options, DbInteractor context)
        {
            StripeConfiguration.ApiKey = options.Value.PrivateKey;
            _context = context;
        }
        public async Task<string> CheckIfCustomerExistsorCreateOneAsync(StripeSubscriberRequest subReq)
        {
            Subscriber existingCustomer = await _context.Subscribers.FirstOrDefaultAsync(i => i.BillingEmail == subReq.Email);
            if (existingCustomer == null)
            {
                Subscriber newCustomer = new Subscriber
                {
                    ClientID = await CreateCustomer(subReq),
                    BillingEmail = subReq.Email,
                    BillingName = subReq.Name,
                    PaymentMethod = "card",
                };
                _context.Subscribers.Add(newCustomer);
                _context.SaveChanges();

                return newCustomer.ClientID;
            }

            return existingCustomer.ClientID;
        }
        public async Task<string> CreateCustomer(StripeSubscriberRequest req)
        {
            var options = new CustomerCreateOptions
            {
                Name = req.Name,
                Email = req.Email,
            };
            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            return customer.Id;
        }
        public async Task<string> ListSubscriptionAsync(string email)
        {
            Subscriber existingCustomer = await _context.Subscribers.FirstOrDefaultAsync(i => i.BillingEmail == email);
            var customerID = existingCustomer.ClientID;
            return customerID;
        }
        public Task<List<Subscriber>> GetSubscribers()
        {
            return _context.Subscribers.ToListAsync();
        }

        #region StripeBillingPortal
        public async Task<SubscriptionCreateResponse> CreateSubscription(StripeSubscriberRequest req, string customerId)
        {

            var subscriptionOptions = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = req.Price,
                    },
                },
                PaymentBehavior = "default_incomplete",
            };
            subscriptionOptions.AddExpand("latest_invoice.payment_intent");
            var subscriptionService = new SubscriptionService();

            Subscription subscription = await subscriptionService.CreateAsync(subscriptionOptions);

            return new SubscriptionCreateResponse
            {
                SubscriptionId = subscription.Id,
                ClientSecret = subscription.LatestInvoice.PaymentIntent.ClientSecret,
            };

        }

        public async Task<string> ActivateSubscriptionAsync(StripeSubscriberRequest subReq)
        {
            string customerID = await CheckIfCustomerExistsorCreateOneAsync(subReq);
            await CreateSubscription(subReq, customerID);

            return customerID;
        }
        #endregion
    }
}
