using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Swish.IntegrationTests
{
    public class IntegrationProdTests
    {
        private readonly byte[] _merchantPFXForTest;
        private readonly byte[] _merchantCertificateDataInPEM;
        private readonly string _merchantPrivateKey;
        private readonly string _merchantCertificatePassword;
        private readonly string _merchantId;

        public IntegrationProdTests()
        {
            _merchantPFXForTest = System.IO.File.ReadAllBytes("certificates/1231181189.p12");
            _merchantCertificateDataInPEM = System.IO.File.ReadAllBytes("certificates/prod.pem");
            _merchantPrivateKey = System.IO.File.ReadAllText("certificates/private.key");
            _merchantCertificatePassword = "";
            _merchantId = "test";
        }

        [Fact(Skip="Prod integration tests does not work")]
        public async Task CertificateAcceptedTest2()
        {
            var bytes = CertificateGenerator.GenerateP12(_merchantPrivateKey, _merchantCertificateDataInPEM, "");

            var client = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: bytes,
                P12CertificateCollectionPassphrase: _merchantCertificatePassword,
                merchantId: _merchantId);

            var paymentStatus = await client.GetPaymentStatus("anything");

            // should be "not found" in the error message, which states that connection is established but obviously transaction was not found
            Assert.NotNull(paymentStatus.ErrorMessage);

            // now test misconfigured client (should throw exception)
            var clientWithWrongPfx = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: _merchantPFXForTest,
                P12CertificateCollectionPassphrase: "swish",
                merchantId: _merchantId);

            try
            {
                var _ = await clientWithWrongPfx.GetPaymentStatus("anything");
            }
            catch (Exception ex)
            {

            }

            // now create working client to check if any of the certificates sticked from last one
            var client2 = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: bytes,
                P12CertificateCollectionPassphrase: _merchantCertificatePassword,
                merchantId: _merchantId);

            var paymentStatus2 = await client.GetPaymentStatus("anything");

            // should be "not found" in the error message, which states that connection is established but obviously transaction was not found
            Assert.NotNull(paymentStatus.ErrorMessage);
        }

        [Fact(Skip= "Prod integration tests does not work")]
        public async Task CertificateAcceptedTest23()
        {
            var bytes = CertificateGenerator.GenerateP12(_merchantPrivateKey, _merchantCertificateDataInPEM, "");

            var client = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: bytes,
                P12CertificateCollectionPassphrase: "",
                merchantId: "123");

            var paymentStatus = await client.GetPaymentStatus("test");

            var client2 = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: bytes,
                P12CertificateCollectionPassphrase: "",
                merchantId: "123");

            var paymentStatus2 = await client2.GetPaymentStatus("test");
        }
    }
}