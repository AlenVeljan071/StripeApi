using System.ComponentModel.DataAnnotations;

namespace StripeApi.Models
{
    public class Subscriber
    {

        [Key]
        [Required]
        public string ClientID { get; set; }
        [Required]
        public string BillingName { get; set; }
        [Required]
        public string BillingEmail { get; set; }
        [Required]
        public string PaymentMethod { get; set; }

    }
}
