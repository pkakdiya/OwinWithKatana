using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin;
using System.IO;

namespace Owin.Katana
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    class Program
    {
        static void Main(string[] args)
        {
            WebApp.Start<StartUp>("http://localhost:8991");
            Console.WriteLine("Server is Started, Press Enter to Quite");
            Console.ReadLine();
        }
    }

    public class StartUp
    {
        public void Configuration(IAppBuilder builder)
        {
            var middleware = new Func<AppFunc, AppFunc>(MiddleWare);
            var othermiddleware = new Func<AppFunc, AppFunc>(MySecondMiddleWare);
            builder.Use<LoggingMiddleWare>();
            builder.Use<AuthenticationMiddleWare>();
            //builder.Use(middleware);
            //builder.Use(othermiddleware);
            MiddleWareOptions _option = new MiddleWareOptions("Hello", "John");
            builder.Use<MyMiddleWare>(_option);
        }

        public AppFunc MiddleWare(AppFunc next)
        {
            AppFunc func = async (IDictionary<string, object> environment) =>
            {

                /*Normal Way to Directly Use the Envionment Dictionary
                var response = environment["owin.ResponseBody"] as Stream;
                using (var writer = new StreamWriter(response))
                {
                    await writer.WriteAsync("<div>Hello from Katana Owin First MiddleWare</div>");
                }*/

                // More Meaningful way to represent environment. Using Katana Abstraction
                IOwinContext context = new OwinContext(environment);
                await context.Response.WriteAsync("<div>Hello from Katana Owin First MiddleWare</div>");
                await next.Invoke(environment);
            };

            return func;
        }

        public AppFunc MySecondMiddleWare(AppFunc next)
        {
            AppFunc func = async (IDictionary<string, object> environment) =>
            {

                /*Normal Way to Directly Use the Envionment Dictionary
                var response = environment["owin.ResponseBody"] as Stream;
                using (var writer = new StreamWriter(response))
                {
                    await writer.WriteAsync("<div>Hello from Katana Owin Second MiddleWare</div>");
                }*/

                // More Meaningful way to represent environment. Using Katana Abstraction
                IOwinContext context = new OwinContext(environment);
                await context.Response.WriteAsync("<div>Hello from Katana Owin Second MiddleWare</div>");
                await next.Invoke(environment);

            };

            return func;
        }
    }

    public class LoggingMiddleWare
    {
        AppFunc _next;
        public LoggingMiddleWare(AppFunc next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> envionment)
        {
            await _next.Invoke(envionment);
            IOwinContext owinContext = new OwinContext(envionment);
            Console.WriteLine($"URI: {owinContext.Request.Uri} Status Code: {owinContext.Response.StatusCode}");
        }
    }

    public class AuthenticationMiddleWare
    {
        AppFunc _next;

        public AuthenticationMiddleWare(AppFunc next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IOwinContext context = new OwinContext(environment);

            var isAuthorized = context.Request.QueryString.Value == "John";

            if (!isAuthorized)
            {
                context.Response.StatusCode = 401;
                context.Response.ReasonPhrase = "Not Authorized";
                await context.Response.WriteAsync($"<h1> Error: {context.Response.StatusCode}-{context.Response.ReasonPhrase}");
            }
            else
            {
                context.Response.StatusCode = 200;
                context.Response.ReasonPhrase = "OK";
                await _next.Invoke(environment);
            }
        }
    }

    public class MyMiddleWare
    {
        AppFunc _next;
        MiddleWareOptions _options;

        public MyMiddleWare(AppFunc next, MiddleWareOptions options)
        {
            _next = next;
            _options = options;
        }


        public async Task Invoke(IDictionary<string, Object> environment)
        {
            await _next.Invoke(environment);
            IOwinContext context = new OwinContext(environment);
            await context.Response.WriteAsync($"<h1>{_options.GetGreetingsfromUser()}</h1>");
            context.Response.StatusCode = 200;
            context.Response.ReasonPhrase = "OK";
        }
    }

    public class MiddleWareOptions
    {
        public string Greetings { get; private set; }

        public string Greeter { get; private set; }

        public MiddleWareOptions(string greetings, string greeter)
        {
            Greetings = greetings;
            Greeter = greeter;
        }

        public string GetGreetingsfromUser()
        {
            return $"{Greetings} from the User: {Greeter}";
        }
    }
}
