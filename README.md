# A .Net Swish Client

Swish (https://www.getswish.se/) client library written in .NET Core (.NET Standard 2.0).

## Usage

Test scenario works without installing any certificates, works in Linux aswell.
Production use is not yet fully tested.

### Initializing the client
```C#
var merchantCertificateData = System.IO.File.ReadAllBytes("certificates/1231181189.p12");
var merchantCertificatePassword = "swish";
var merchantId = "1231181189";

var client = new SwishClient(SwishEnvironment.Test, merchantCertificateData, merchantCertificatePassword, merchantId);
```

### Making a payment request
```C#
// Make payment
var ecommercePaymentModel = new ECommercePaymentModel(
    amount: "100",
    currency: "SEK",
    callbackUrl: "https://example.com/api/swishcb/paymentrequests",
    payerAlias: "1231234567890")
{
    PayeePaymentReference = "0123456789",
    Message = "Kingston USB Flash Drive 8 GB"
};

var paymentResponse = await client.MakeECommercePaymentAsync(ecommercePaymentModel);

// Wait so that the payment request has been processed
Thread.Sleep(5000);

// Check payment request status
var paymentStatus = await client.GetPaymentStatus(paymentResponse.Id);
```

### Making a refund request
```C#
// Make refund
var refundModel = new RefundModel(
    originalPaymentReference: paymentStatus.PaymentReference,
    callbackUrl: "https://example.com/api/swishcb/refunds",
    payerAlias: "1231181189",
    amount: "100",
    currency: "SEK")
{
    PayerPaymentReference = "0123456789",
    Message = "Refund for Kingston USB Flash Drive 8 GB"
};
var refundResponse = await client.MakeRefundAsync(refundModel);

// Wait so that the refund request has been processed
Thread.Sleep(10000);

// Check refund request status
var refundStatus = await client.GetRefundStatus(refundResponse.Id);
```
