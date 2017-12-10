using System;
using System.Data;
using System.Linq;
using EmailContacts.ServiceInterface;
using EmailContacts.ServiceModel;
using EmailContacts.ServiceModel.Types;
using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Messaging;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.RabbitMq;
using ServiceStack.Razor;
using ServiceStack.Validation;

namespace EmailContacts
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("Email Contact Services", typeof(ContactsServices).Assembly, typeof(QueryEmails).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new OpenApiFeature());
            Plugins.Add(new RazorFormat());
            Plugins.Add(new RequestLogsFeature());

            Plugins.Add(new MiniProfilerFeature());
            Plugins.Add(new PostmanFeature());
            Plugins.Add(new CorsFeature());

            Plugins.Add(new ValidationFeature());
            container.RegisterValidators(typeof(ContactsServices).Assembly);

            Plugins.Add(new AutoQueryFeature());

            container.Register<IDbConnectionFactory>(
                c => new OrmLiteConnectionFactory("~/db.sqlite".MapHostAbsolutePath(), SqliteDialect.Provider) {
                    ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                });

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Email>();
                db.DropAndCreateTable<Contact>();
                ContactsServices.AddCustomers(db);
            }

            UseDbEmailer(container);
            //UseSmtpEmailer(container); //Uncomment to use SMTP instead

            //ConfigureRabbitMqServer(container); //Uncomment to start accepting requests via Rabbit MQ
        }

        private void ConfigureRabbitMqServer(Container container)
        {
            container.Register<IMessageService>(c => new RabbitMqServer());
            var mqServer = container.Resolve<IMessageService>();

            mqServer.RegisterHandler<EmailContact>(ServiceController.ExecuteMessage);

            mqServer.Start();
        }

        private static void UseDbEmailer(Container container)
        {
            container.RegisterAs<DbEmailer, IEmailer>().ReusedWithin(ReuseScope.Request);
        }

        private static void UseSmtpEmailer(Container container)
        {
            var appSettings = new AppSettings();

            //Use 'SmtpConfig' appSetting in Web.config if it exists otherwise use default config below:
            container.Register(appSettings.Get("SmtpConfig",
                new SmtpConfig {
                    Host = "smtphost",
                    Port = 587,
                    UserName = "ADD_USERNAME",
                    Password = "ADD_PASSWORD"
                }));

            container.RegisterAs<SmtpEmailer, IEmailer>().ReusedWithin(ReuseScope.Request);
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }

        protected void Application_BeginRequest(object src, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object src, EventArgs e)
        {
            Profiler.Stop();
        }
    }
}