﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Thor.Core.Abstractions;
using static Thor.Core.Http.RequestActivityEventSource;

namespace Thor.Core.Http
{
    /// <summary>
    /// A message handler for tracing which especially creates a client HTTP activity and sets the
    /// tracing id so the server is able to connect the server HTTP activity with the client HTTP
    /// activity.
    /// </summary>
    public class TracingHttpMessageHandler
        : DelegatingHandler
    {
        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            HttpResponseMessage response = null;
            string method = request.Method.Method;

            using (ClientRequestActivity activity = ClientRequestActivity.Create(method, request.RequestUri))
            {
                try
                {
                    request.Headers.Add(MessageHeaderKeys.ActivityId, activity.Id.ToString("N"));
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    activity.SetResponse((int)response.StatusCode, Guid.Empty);
                }
                catch (Exception ex)
                {
                    Log.InternalServerErrorOccurred(ex);

                    if (Debugger.IsAttached)
                    {
                        throw;
                    }
                }
            }


            return response;
        }
    }
}