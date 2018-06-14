using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Swish
{
    class PasswordStore : IPasswordFinder
    {
        private char[] password;

        public PasswordStore(char[] password)
        {
            this.password = password;
        }

        public char[] GetPassword()
        {
            return (char[])password.Clone();
        }

    }

    public class CertificateGenerator
    {
        private const int KEY_BITS = 4096;

        private readonly SecureRandom _random = new SecureRandom();

        public void Generate(string certificateSubjectText = "CN=Magnet, C=NL")
        {
            var keypair = GenerateKeyPair();

            var csr = GenerateCsr(keypair, certificateSubjectText);
        }

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            //Key generation
            var rkpg = new RsaKeyPairGenerator();
            rkpg.Init(new KeyGenerationParameters(_random, KEY_BITS));

            return rkpg.GenerateKeyPair();
        }

        public (string privateKeyAsPem, string csrAsPem) GenerateCsr(AsymmetricCipherKeyPair keypair, string certificateSubjectText)
        {
            //PKCS #10 Certificate Signing Request
            var signatureFactory = new Asn1SignatureFactory("SHA1WITHRSA", keypair.Private, _random);
            var csr = new Pkcs10CertificationRequest(signatureFactory, new X509Name(certificateSubjectText), keypair.Public, null, keypair.Private);

            //Convert BouncyCastle CSR to PEM file
            var csrAsPem = new StringBuilder();
            var csrAsPemWriter = new PemWriter(new StringWriter(csrAsPem));
            csrAsPemWriter.WriteObject(csr);
            csrAsPemWriter.Writer.Flush();

            //Push the CSR text
            var csrText = csrAsPem.ToString();

            //Convert BouncyCastle Private Key to PEM file
            var privateKeyPem = new StringBuilder();
            var privateKeyPemWriter = new PemWriter(new StringWriter(privateKeyPem));
            privateKeyPemWriter.WriteObject(keypair.Private);
            csrAsPemWriter.Writer.Flush();

            //Push the private key text
            var privateKeyText = privateKeyPem.ToString();

            return (privateKeyText, csrText);
        }

        public static byte[] GenerateP12(string privateKey, byte[] pemCertificate, string password)
        {
            var rng = new SecureRandom();
            Pkcs12Store pkcs12Store = new Pkcs12Store();

            var parser = new X509CertificateParser();
            var certChain = parser.ReadCertificates(pemCertificate);

            PemReader pemReader = new PemReader(new StringReader(privateKey));

            AsymmetricKeyParameter kp = (pemReader.ReadObject() as AsymmetricCipherKeyPair).Private;

            X509CertificateEntry[] certEntry = new X509CertificateEntry[certChain.Count];

            int index = 0;
            foreach (var cert in certChain)
            {
                certEntry[index] = new X509CertificateEntry(cert as X509Certificate);
                index++;
            }

            pkcs12Store.SetKeyEntry(certEntry[0].Certificate.SubjectDN.ToString(), new AsymmetricKeyEntry(kp), certEntry);
            
            MemoryStream memoryStream = new MemoryStream();
            pkcs12Store.Save(memoryStream, null, rng);

            return memoryStream.ToArray();
        }
    }
}