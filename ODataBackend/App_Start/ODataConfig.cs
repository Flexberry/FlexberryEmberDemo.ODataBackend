namespace FlexberryEmberDemo
{
    using System;
    using System.Reflection;
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.Cors;
    using System.Web.Security;

    using ICSSoft.STORMNET;
    using ICSSoft.Services;
    using ICSSoft.STORMNET.Security;
    using IIS.Caseberry.Logging.Objects;

    using Microsoft.Practices.Unity;

    using NewPlatform.Flexberry;
    using NewPlatform.Flexberry.ORM.ODataService;
    using NewPlatform.Flexberry.ORM.ODataService.Extensions;
    using NewPlatform.Flexberry.ORM.ODataService.Functions;
    using NewPlatform.Flexberry.ORM.ODataService.Model;
    using NewPlatform.Flexberry.Security;
    using NewPlatform.Flexberry.Services;

    /// <summary>
    /// Configure OData Service.
    /// </summary>
    internal static class ODataConfig
    {
        /// <summary>
        /// Configure OData by DataObjects assembly.
        /// </summary>
        /// <param name="config">Http configuration object.</param>
        /// <param name="container">Unity container.</param>
        public static void Configure(HttpConfiguration config, IUnityContainer container)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // Use Unity as WebAPI dependency resolver
            config.DependencyResolver = new UnityDependencyResolver(container);

            // Create EDM model builder
            var assemblies = new[] {
                Assembly.Load("FlexberryEmberDemo.Objects"),
                typeof(ApplicationLog).Assembly,
                typeof(Lock).Assembly,
                typeof(Agent).Assembly
            };
            var builder = new DefaultDataObjectEdmModelBuilder(assemblies);

            // Map OData Service
            var token = config.MapODataServiceDataObjectRoute(builder);

            // User functions
            token.Functions.Register(new Func<QueryParameters, string>(Test));
            token.Functions.Register(new Func<string, string, bool>(Login));
            token.Functions.Register(new Func<bool>(Logout));
            token.Functions.Register(new Func<string>(GetAuthenticatedUser));

            // Event handlers
            token.Events.CallbackAfterCreate = CallbackAfterCreate;
        }

        private static void CallbackAfterCreate(DataObject dataObject)
        {
            // TODO: implement handler
        }

        private static string Test(QueryParameters queryParameters)
        {
            return "Hello world!";
        }

        private static bool Login(string login, string password)
        {
            IUserManager userManager = UnityFactory.GetContainer().Resolve<IUserManager>();

            if (userManager.IsUserExist(login, password))
            {
                FormsAuthentication.SetAuthCookie(login, true);
                return true;
            }

            return false;
        }

        private static bool Logout()
        {
            FormsAuthentication.SignOut();
            return true;
        }

        private static string GetAuthenticatedUser()
        {
            HttpCookie authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null)
                return string.Empty;

            FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null)
                return string.Empty;

            return ticket.Name;
        }
    }
}