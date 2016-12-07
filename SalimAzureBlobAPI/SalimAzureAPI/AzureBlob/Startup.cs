using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.Swagger.Model;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.Http;
using AuthorizationLab;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BlogStorage
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add authentication services
            services.AddAuthentication(
                options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
            // create a policy based on role in token and put it on every action
            services.AddAuthorization(options =>
            {
                //options.AddPolicy("SpecificEmployerIDOnly", policy => policy.RequireClaim("EmployerID", "1", "2", "3", "4", "5"));
                options.AddPolicy("AdministratorOnly", policy => policy.RequireRole("admin"));
                options.AddPolicy("UserOnly", policy => policy.RequireRole("user"));
            });

            // Add framework services.
            services.AddMvc();

            // Add functionality to inject IOptions<T>
            services.AddOptions();

            // Add the Auth0 Settings object so it can be injected
            services.Configure<Auth0Settings>(Configuration.GetSection("Auth0"));
            services.Configure<AzureBlobSettings>(Configuration.GetSection("AzureBlob"));

            services.Configure<AzureBlobSettings>(myAzureBlobOptions =>
            {
                new AzureBlobSettings
                {
                    AzureBlobConnectionString = Configuration["AzureBlob:AzureBlobConnectionString"],
                    BlobContainerName = Configuration["AzureBlob:BlobContainerName"],
                    BlobFileDownloadLocation = Configuration["AzureBlob:BlobFileDownloadLocation"],
                };
            });

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddSingleton<IDocumentRepository, DocumentRepository>();
            services.AddSingleton<IAuthorizationHandler, DocumentEditHandler>();
            services.AddSingleton<IAuthorizationRequirement, PermissionsAuthorizationRequirement>();
            services.AddSingleton<IAuthorizationRequirement, ProjectAccessRequirement>();
            services.AddSingleton<IAuthorizationHandler, ProjectAccessRequirementHandler>();
            services.AddSingleton<RequiresPermissionAttribute>();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            // Inject an implementation of ISwaggerProvider with defaulted settings applied
            services.AddSwaggerGen();
            services.ConfigureSwaggerGen(options =>
            {
                options.SingleApiVersion(new Info
                {
                    Version = "v1",
                    Title = "BlogStorage",

                });

                //Determine base path for the application.
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                //Set the comments path for the swagger json and ui.
                options.IncludeXmlComments(basePath + "\\BlogStorage.xml");
            });

            //Inject each repository for each interface 
            services.AddScoped<IanyRepository, anyRepository>();
          

            //  services.AddScoped<ISoftwareVersionRepository, SoftwareVersionRepository>();
            services.AddLogging();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Auth0Settings> auth0Settings, ApplicationDbContext context)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            //// in case of error in api , redirec tthe user for demo
            // app.UseStatusCodePagesWithRedirects("/Account/Forbidden/");

            app.UseStaticFiles();

            // Add the cookie middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                //Added for Document
                LoginPath = new PathString("/Account/Login/"),
                AccessDeniedPath = new PathString("/Account/Forbidden/"),

                AutomaticAuthenticate = true,
                AutomaticChallenge = true,

                ExpireTimeSpan = new TimeSpan(7, 0, 0, 0), // 7 days expiration in this example,
                SlidingExpiration = true, // you can even configure sliding expiration    

                Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = CookieAuthenticationEventsContext =>
                    {
                        if (CookieAuthenticationEventsContext.Request.Path.StartsWithSegments("/api") && !(CookieAuthenticationEventsContext.Response.StatusCode == (int)HttpStatusCode.OK))
                        {
                            var method = CookieAuthenticationEventsContext.Request.Method;
                            var responsecode = CookieAuthenticationEventsContext.Response.StatusCode;
                            switch (method)
                            {
                                case "GET":
                                    if (responsecode.ToString() == "401")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                        //CookieAuthenticationEventsContext.Response.Headers.Add("Response",HttpStatusCode.Unauthorized.ToString());
                                        // In UI case if we want to show what the error is
                                        //CookieAuthenticationEventsContext.Response.WriteAsync(HttpStatusCode.Unauthorized.ToString());
                                    }
                                    if (responsecode.ToString() == "404")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                                        //CookieAuthenticationEventsContext.Response.WriteAsync(HttpStatusCode.NotFound.ToString());
                                    }
                                    return Task.FromResult(0);

                                case "POST":
                                    if (responsecode.ToString() == "401")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                        //CookieAuthenticationEventsContext.Response.WriteAsync(HttpStatusCode.Unauthorized.ToString());
                                    }
                                    if (responsecode.ToString() == "201")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.Created;
                                        //CookieAuthenticationEventsContext.Response.WriteAsync("Created Successfully");
                                    }
                                    return Task.FromResult(0);

                                case "PUT":
                                    if (responsecode.ToString() == "401")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                        // CookieAuthenticationEventsContext.Response.WriteAsync(HttpStatusCode.Unauthorized.ToString());
                                    }
                                    if (responsecode.ToString() == "204")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
                                        //CookieAuthenticationEventsContext.Response.WriteAsync("Updated Successfully");
                                    }
                                    return Task.FromResult(0);

                                case "DELETE":
                                    if (responsecode.ToString() == "401")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                        // CookieAuthenticationEventsContext.Response.WriteAsync(HttpStatusCode.Unauthorized.ToString());
                                    }
                                    if (responsecode.ToString() == "204")
                                    {
                                        CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
                                        // CookieAuthenticationEventsContext.Response.WriteAsync("Deleted Successfully");
                                    }
                                    return Task.FromResult(0);

                                default:
                                    CookieAuthenticationEventsContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                    // You can use the default case.
                                    return Task.FromResult(0);
                            }
                        }
                        else
                        {
                            CookieAuthenticationEventsContext.Response.Redirect(CookieAuthenticationEventsContext.RedirectUri);
                        }
                        return Task.FromResult(0);

                    }
                }
            });

            // Add the OIDC middleware
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions("Auth0")
            {
                // Set the authority to your Auth0 domain
                Authority = $"https://{auth0Settings.Value.Domain}",

                // Configure the Auth0 Client ID and Client Secret
                ClientId = auth0Settings.Value.ClientId,
                ClientSecret = auth0Settings.Value.ClientSecret,

                // Do not automatically authenticate and challenge
                AutomaticAuthenticate = false,
                AutomaticChallenge = false,

                // Set response type to code
                ResponseType = "code",

                // Saves tokens to the AuthenticationProperties
                SaveTokens = true,

                // Set the callback path, so Auth0 will call back to http://localhost:60856/signin-auth0 
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard 
                CallbackPath = new PathString("/signin-auth0"),

                // Configure the Claims Issuer to be Auth0
                ClaimsIssuer = "Auth0",
                // Added this code to fill name claim in case user comes directly without login to /document/edit/1 and name claim is missing it will error out.
                // At https://auth0.com/docs/quickstart/webapp/aspnet-core/05-user-profile
                //One remaining issue is that the User.Identity.Name property used in the Navbar snippet above will look for a claim of type http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name on the user, but hat claim will not be set and the property will therefor be null.

                Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = OpenIdConnectEventsOnTokenValidatedContext =>
                    {
                        var identity = OpenIdConnectEventsOnTokenValidatedContext.HttpContext.User.Identity as ClaimsIdentity;
                        if (identity != null)
                        {
                            if (!String.IsNullOrEmpty(OpenIdConnectEventsOnTokenValidatedContext.TokenEndpointResponse.AccessToken))
                                identity.AddClaim(new Claim("access_token", OpenIdConnectEventsOnTokenValidatedContext.TokenEndpointResponse.AccessToken));
                            if (!String.IsNullOrEmpty(OpenIdConnectEventsOnTokenValidatedContext.TokenEndpointResponse.IdToken))
                                identity.AddClaim(new Claim("id_token", OpenIdConnectEventsOnTokenValidatedContext.TokenEndpointResponse.IdToken));
                            if (!String.IsNullOrEmpty(OpenIdConnectEventsOnTokenValidatedContext.TokenEndpointResponse.RefreshToken))
                                identity.AddClaim(new Claim("refresh_token", OpenIdConnectEventsOnTokenValidatedContext.TokenEndpointResponse.RefreshToken));



                            //Auth0BaseApiController a = new Auth0BaseApiController();
                            //var b = a.CreateAdminUserUsingAuth0ManagementAPI();

                        }
                        return Task.FromResult(0);
                    },

                    OnTicketReceived = OpenIdConnectEventsOnTicketReceivedContext =>
                    {
                        // Get the ClaimsIdentity
                        var identity = OpenIdConnectEventsOnTicketReceivedContext.Principal.Identity as ClaimsIdentity;
                        if (identity != null)
                        {
                            // Add the Name ClaimType. This is required if we want User.Identity.Name to actually return something!
                            if (!OpenIdConnectEventsOnTicketReceivedContext.Principal.HasClaim(c => c.Type == ClaimTypes.Name) &&
                                identity.HasClaim(c => c.Type == "name"))
                                identity.AddClaim(new Claim(ClaimTypes.Name, identity.FindFirst("name").Value));

                            // Check if token names are stored in Properties
                            if (OpenIdConnectEventsOnTicketReceivedContext.Properties.Items.ContainsKey(".TokenNames"))
                            {
                                // Token names a semicolon separated
                                string[] tokenNames = OpenIdConnectEventsOnTicketReceivedContext.Properties.Items[".TokenNames"].Split(';');

                                // Add each token value as Claim
                                foreach (var tokenName in tokenNames)
                                {
                                    // Tokens are stored in a Dictionary with the Key ".Token.<token name>"
                                    string tokenValue = OpenIdConnectEventsOnTicketReceivedContext.Properties.Items[$".Token.{tokenName}"];

                                    identity.AddClaim(new Claim(tokenName, tokenValue));
                                }
                            }
                        }

                        return Task.FromResult(0);
                    }
                }
            });

            var options = new JwtBearerOptions
            {
                Audience = Configuration["auth0:clientId"],
                Authority = $"https://{Configuration["auth0:domain"]}/",
                // SaveToken = 
                Events = new JwtBearerEvents
                {
                    OnTokenValidated = JwtBearerEventsContext =>
                    {
                        // If you need the user's information for any reason at this point, you can get it by looking at the Claims property
                        // of context.Ticket.Principal.Identity
                        var claimsIdentity = JwtBearerEventsContext.Ticket.Principal.Identity as ClaimsIdentity;
                        if (claimsIdentity != null)
                        {
                            // Get the user's ID
                            string userId = claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
                            // Get the name
                            string name = claimsIdentity.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                        }

                        return Task.FromResult(0);
                    }
                }
            };

            app.UseJwtBearerAuthentication(options);

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();

            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
            app.UseSwaggerUi();

            app.UseMvcWithDefaultRoute();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "AzureBlob",
                    template: "{controller=AzureBlob}/{action=GetBlobDownload}/{filetoDownload?}");
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "AzureBlob",
                    template: "{controller=AzureBlob}/{action=delete}/{filetoDelete?}");
            });




            DbInitializer.Initialize(context);
        }
    }
}
