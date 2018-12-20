﻿using System;
using System.Threading.Tasks;
using HotChocolate.Execution;

#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Subscriptions;
using HttpContext = Microsoft.Owin.IOwinContext;
using RequestDelegate = Microsoft.Owin.OwinMiddleware;
#else
using HotChocolate.AspNetCore.Subscriptions;
using Microsoft.AspNetCore.Http;
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public class SubscriptionMiddleware
#if ASPNETCLASSIC
        : RequestDelegate
#endif
    {
        public SubscriptionMiddleware(
            RequestDelegate next,
            IQueryExecuter queryExecuter,
            QueryMiddlewareOptions options)
#if ASPNETCLASSIC
                : base(next)
#endif
        {
#if !ASPNETCLASSIC
            Next = next;
#endif
            QueryExecuter = queryExecuter ??
                throw new ArgumentNullException(nameof(queryExecuter));
            Options = options ??
                throw new ArgumentNullException(nameof(options));
        }

#if !ASPNETCLASSIC
        protected RequestDelegate Next { get; }
#endif

        protected QueryMiddlewareOptions Options { get; }

        protected IQueryExecuter QueryExecuter { get; }

#if ASPNETCLASSIC
        /// <inheritdoc />
        public override async Task Invoke(HttpContext context)
#else
        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
#endif
        {
            if (context.IsWebSocketRequest() &&
                context.IsValidPath(Options.SubscriptionPath))
            {
                OnConnectWebSocketAsync onConnect =
                    Options.OnConnectWebSocket ??
                    GetService<OnConnectWebSocketAsync>(context);
                OnCreateRequestAsync onRequest =
                    Options.OnCreateRequest ??
                    GetService<OnCreateRequestAsync>(context);

                WebSocketSession session = await WebSocketSession
                    .TryCreateAsync(
                        context,
                        QueryExecuter,
                        onConnect,
                        onRequest)
                    .ConfigureAwait(false);

                if (session != null)
                {
                    await session
                        .StartAsync(context.GetCancellationToken())
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
        }

#if ASPNETCLASSIC
        protected T GetService<T>(HttpContext context)
        {
            if (context.Environment.TryGetValue(
                EnvironmentKeys.ServiceProvider, out var value) &&
                    value is IServiceProvider serviceProvider)
            {
                return (T)serviceProvider.GetService(typeof(T));
            }

            return default;
        }
#else
        protected T GetService<T>(HttpContext context) =>
            (T)context.RequestServices.GetService(typeof(T));
#endif
    }
}
