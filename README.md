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
# Sample implementation
A sample implementation of Swish payments using ASP.NET Core 2.2 is included in the repo (SwishTestWebAppCore).

The Swish servers are calling the callbackUrl specified when making the payment request. So the the sample implementation must be reachable from the Internet (if you want callbacks to work).

There are two options for this
### Deploy to cloud
Deploy the sample implementation (SwishTestWebAppCore) to favourite cloud. It will be harder to debug the application this way.
### Ngrok
If you download [ngrok](https://dashboard.ngrok.com/get-started) you will be able to create an endpoint that is available on the Internet and is connected to your local web server.
After you downloaded ngrok connect it to your ngrok account with
```
ngrok authtoken xxxxxxxxxxxxxxxxxxxxxxxxx
```
Then you can create a tunnel to your local web server with 
```
ngrok http https://localhost:44375 -host-header=localhost
```
In the output from ngrok you should look for the second line starting with "Forwarding"
```
Forwarding                    https://ffffffff.ngrok.io -> https://localhost:44375
```
Start the project SwishTestWebAppCore and try surfing to the ngrok url (https://ffffffff.ngrok.io). If everything works you should see the same site as if you access (https://localhost:44375/).

Copy that url and right click on the project SwishTestWebAppCore and select "Manage User Secrets". Copy the following json to that window (secrets.json)
```
{
  "Swish": {
    "CallbackBaseUrl": "https://ffffffff.ngrok.io"
  }
}
```
Change the CallbackBaseUrl value to your ngrok url or the url to your deployed application.
## Certificate in Keyvault
The default configuration (in appsettings.json) reads the swish certificate from file (App_Data/1231181189.p12).

There is also support for getting the swish certificate from an [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/).

All configuration changes can be done in appsettings.json, appsettings.Development.json or User Secrets.

If you want to get your secrets from the KeyVault change the KeyVault->BaseUrl like this
```
{
  "KeyVault": {
    "BaseUrl": "https://mykeyvault.vault.azure.net/"
  },
  "Swish": {
    "CallbackBaseUrl": "https://fffffff.ngrok.io",
    "CertificateName": "MySwishCertificate",
    "MerchantId": "1231181189",
    "Environment": "Production"
  }
}
```
Where 
* CertificateName is the name of your swish cerfiticate in the Key Vault.
* Environment is the swish environment to use (Production or Test)
* CertificatePassword is not needed when getting certificates from Key Vault

