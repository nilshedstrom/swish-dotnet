using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Swish.IntegrationTests
{
    public class IntegrationTests
    {
        private readonly byte[] _merchantCertificateData;
        private readonly string _merchantCertificatePassword;
        private readonly string _merchantId;

        public IntegrationTests()
        {
            _merchantCertificateData = System.IO.File.ReadAllBytes("certificates/1231181189.p12");
            _merchantCertificatePassword = "swish";
            _merchantId = "1231181189";
        }


        [Fact]
        public async Task ECommerceScenario()
        {
            var client = new SwishClient(SwishEnvironment.Test, _merchantCertificateData, _merchantCertificatePassword, _merchantId);

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
            await Task.Delay(5000);

            // Check payment request status
            var paymentStatus = await client.GetPaymentStatus(paymentResponse.Id);
            Assert.Equal("PAID", paymentStatus.Status);

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
            await Task.Delay(10000);

            // Check refund request status
            var refundStatus = await client.GetRefundStatus(refundResponse.Id);
            Assert.Equal("PAID", refundStatus.Status);
        }


        [Fact]
        public async Task MCommerceScenario()
        {
            var client = new SwishClient(SwishEnvironment.Test, _merchantCertificateData, _merchantCertificatePassword, _merchantId);

            // Make payment
            var mcommercePaymentModel = new MCommercePaymentModel(
                amount: "100",
                currency: "SEK",
                callbackUrl: "https://example.com/api/swishcb/paymentrequests")
            {
                PayeePaymentReference = "0123456789",
                Message = "Kingston USB Flash Drive 8 GB"
            };

            var paymentResponse = await client.MakeMCommercePaymentAsync(mcommercePaymentModel);

            // Wait so that the payment request has been processed
            await Task.Delay(5000);

            // Check payment request status
            var paymentStatus = await client.GetPaymentStatus(paymentResponse.Id);
            Assert.Equal("PAID", paymentStatus.Status);

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
            }; ;
            var refundResponse = await client.MakeRefundAsync(refundModel);

            // Wait so that the refund request has been processed
            await Task.Delay(10000);

            // Check refund request status
            var refundStatus = await client.GetRefundStatus(refundResponse.Id);
            Assert.Equal("PAID", refundStatus.Status);
        }
    }
}
