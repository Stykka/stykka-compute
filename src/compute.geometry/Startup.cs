using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Google.Cloud.PubSub.V1;

using Grasshopper.Kernel.Types.Transforms;

using Microsoft.Owin;
using Microsoft.Owin.Cors;

using Nancy;

using Newtonsoft.Json.Linq;

using Owin;

using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;

[assembly: OwinStartup(typeof(compute.geometry.Startup))]

namespace compute.geometry
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.Use<LoggingMiddleware>();
            app.UseNancy();
        }
    }

    /// <summary>
    /// Custom request logging for debugging.
    /// </summary>
    internal class LoggingMiddleware : OwinMiddleware
    {
        public LoggingMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext ctx)
        {
            IOwinRequest req = ctx.Request;

            // invoke the next middleware in the pipeline
            await Next.Invoke(ctx);

            IOwinResponse res = ctx.Response;
            string contentLength = res.ContentLength > -1 ? res.ContentLength.ToString() : "-";

            if (req.Uri.AbsolutePath != "/healthcheck" || req.Uri.AbsolutePath != "/favicon.ico")
            {
                // log request in apache format
                string msg = $"{req.RemoteIpAddress} - [{DateTime.Now:o}] \"{req.Method} {req.Uri.AbsolutePath} {req.Protocol}\" {res.StatusCode} {contentLength}";
                Serilog.Log.Information(msg);

                TopicName topicName = new TopicName("rhino-compute-334513", "compute-solve-topic");

                try
                {
                    // Publish a message to the topic using PublisherClient.
                    PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
                    string messageId = await publisher.PublishAsync(msg);

                    Console.WriteLine(messageId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}
