namespace PaymentStripeEngine
{
    public class StripeSubscriberRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("priceId")]
        public string Price { get; set; }
    }
}
