namespace Swish
{
    public class MCommercePaymentResponse : SwishApiResponse
    {
        public string Token { get; set; }
    }

    public class ECommercePaymentResponse : SwishApiResponse
    {
    }

    public class SwishApiResponse
    {
        public string Id { get; set; }

        public string Location { get; set; }
    }
}