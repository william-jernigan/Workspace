using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Claims;

namespace ExchangeReservationFunction.Model.Common
{
    [ExcludeFromCodeCoverage]
    public class FakeHttpRequestData(FunctionContext context) : HttpRequestData(context)
    {
        private string _method = "POST";
        public override Stream Body { get; } = new MemoryStream();
        public override HttpHeadersCollection Headers { get; } = [];
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = [];
        public override Uri Url { get; } = new Uri("https://localhost");
        public override IEnumerable<ClaimsIdentity> Identities => [];
        public override string Method => _method;
        public override HttpResponseData CreateResponse()
        => new FakeHttpResponseData(FunctionContext);
        public void SetMethod(string method) => _method = method;
    }
}
