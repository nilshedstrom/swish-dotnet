using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Swish.UnitTests
{
    public class SwishCsrGeneratorTests
    {
        [Fact]
        public void GenerateCsrTest()
        {
            var certificateSubjectText = "CN=Magnet, C=NL";

            var generator = new CertificateGenerator();

            var keypair = generator.GenerateKeyPair();

            var csr = generator.GenerateCsr(keypair, certificateSubjectText);

            Assert.NotNull(csr.csrAsPem);
            Assert.NotNull(csr.privateKeyAsPem);
        }
    }
}