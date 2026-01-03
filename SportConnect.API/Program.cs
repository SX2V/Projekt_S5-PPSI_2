using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SportConnect.API.Data;
using SportConnect.API.Models;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.OpenApi.Models;
using SportConnect.API.Services;

namespace SportConnect.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.AddControllers();
            
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                })
                // Facebook Authentication
                .AddFacebook(options =>
                {
                    options.AppId = builder.Configuration["Facebook:AppId"] 
                        ?? throw new InvalidOperationException("Facebook AppId not configured");
                    options.AppSecret = builder.Configuration["Facebook:AppSecret"] 
                        ?? throw new InvalidOperationException("Facebook AppSecret not configured");
                    options.Scope.Add("email");
                    options.Fields.Add("name");
                    options.Fields.Add("email");
                });
            
            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddScoped<IActionLogger, ActionLogger>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            
            // JWT Service
            builder.Services.AddScoped<IJwtService, JwtService>();
            
            // Facebook Auth Service
            builder.Services.AddHttpClient<IFacebookAuthService, FacebookAuthService>();
            
            // Strava Auth Service
            builder.Services.AddHttpClient<IStravaAuthService, StravaAuthService>();
            
            // CACHE

            // Memory Cache (dla UsersController i innych)
            builder.Services.AddMemoryCache();
            
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations(); 
                
                // JWT Bearer authentication w Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token"
                });
                
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            
            var app = builder.Build();
            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            
            app.UseHttpsRedirection();
            app.UseAuthentication(); 
            app.UseAuthorization();
            app.MapControllers();
            
            app.Run();
        }
    }
}
