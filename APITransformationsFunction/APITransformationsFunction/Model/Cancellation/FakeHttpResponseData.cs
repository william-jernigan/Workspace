using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;

namespace APITransformationsFunction.Model.Cancellation
{
    [ExcludeFromCodeCoverage]
    public class FakeHttpResponseData(FunctionContext context) : HttpResponseData(context)
    {
        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; } = [];
        public override Stream Body { get; set; } = new MemoryStream();
        public override HttpCookies Cookies { get; }
    }
}
