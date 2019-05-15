using System;

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
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorCode);
    }
}