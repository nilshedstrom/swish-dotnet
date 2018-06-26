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
        private readonly byte[] _merchantCertificateDataInPEM;
        private readonly string _merchantPrivateKey;
        private readonly string _merchantCertificatePassword;
        private readonly string _merchantId;

        public IntegrationProdTests()
        {
            _merchantCertificateDataInPEM = System.IO.File.ReadAllBytes("certificates/prod.pem");
            _merchantPrivateKey = System.IO.File.ReadAllText("certificates/private.key");
            _merchantCertificatePassword = "";
            _merchantId = "test";
        }
        
        [Fact]
        public async Task CertificateAcceptedTest2()
        {
            var bytes = CertificateGenerator.GenerateP12(_merchantPrivateKey, _merchantCertificateDataInPEM, "");

            var client = new SwishClient(SwishEnvironment.Production, P12CertificateCollectionBytes: bytes, P12CertificateCollectionPassphrase: "", merchantId: "123");

            var paymentStatus = await client.GetPaymentStatus("test");
        }
        
        [Fact]
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