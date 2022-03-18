public class SubscriptionCreateResponse
{
    [JsonProperty("subscriptionId")]
    public string SubscriptionId { get; set; }

    [JsonProperty("clientSecret")]
    public string ClientSecret { get; set; }
}
