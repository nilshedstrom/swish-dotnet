﻿using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Serialization;

namespace Swish
{
    public abstract class PaymentModel
    {
        public string PayeePaymentReference { get; set; }

        public string CallbackUrl { get; protected set; }

        public string Amount { get; protected set; }

        public string Currency { get; protected set; }

        public string Message { get; set; }

        [JsonProperty]
        internal string PayeeAlias { get; set; }
    }

    public class MCommercePaymentModel : PaymentModel
    {
        public MCommercePaymentModel(string callbackUrl, string amount,
            string currency)
        {
            CallbackUrl = callbackUrl;
            Amount = amount;
            Currency = currency;
        }
    }

    public class ECommercePaymentModel : PaymentModel
    {
        public ECommercePaymentModel(string callbackUrl, string amount,
            string currency, string payerAlias)
        {
            CallbackUrl = callbackUrl;
            Amount = amount;
            Currency = currency;
            PayerAlias = payerAlias;
        }

        public string PayerAlias { get; set; }
    }



    public class PaymentStatusModel : PaymentModel
    {
        public string Id { get; set; }
        public string PaymentReference { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DatePaid { get; set; }
        public string ErrorMessage { get; set; }
        public string PayerAlias { get; set; }
        public string ErrorCode { get; set; }
        public string AdditionalInformation { get; set; }

        public new string CallbackUrl
        {
            get { return base.CallbackUrl; }
            set { base.CallbackUrl = value; }
        }

        public new string Amount
        {
            get { return base.Amount; }
            set { base.Amount = value; }
        }

        public new string Currency
        {
            get { return base.Currency; }
            set { base.Currency = value; }
        }

    }

}
