using System;
using Microsoft.AspNetCore.Http;

namespace VerySimple.Models
{
    public class IndexViewModel
    {
        public IndexViewModel(HttpRequest request, ISession session)
        {
            IsHttps = request.IsHttps;
            Scheme = request.Scheme;
            XForwardedFor = request.Headers["X-Forwarded-For"];
            XForwardedProto = request.Headers["X-Forwarded-Proto"];
            XForwardedPort = request.Headers["X-Forwarded-Port"];
            Host = request.Headers["Host"];
            ServerHostName = Environment.MachineName;
            SessionValue = session.GetString("sessionValue");
        }

        public bool IsHttps { get; private set; }
        public string Scheme { get; set; }
        public string XForwardedFor { get; set; }
        public string XForwardedPort { get; set; }
        public string XForwardedProto { get; set; }
        public string Host { get; set; }
        public string ServerHostName { get; set; }
        public string SessionValue { get; set; }
    }
}