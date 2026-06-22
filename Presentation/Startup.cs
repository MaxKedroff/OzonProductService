using Microsoft.AspNetCore.Mvc;
using FluentMigrator.Runner;
using Application.Ports.Input;
using Application.Ports.Output;
using Application.Services;
using Infrastructure.Cache;
using Infrastructure.Messaging;
using Infrastructure;
using Infrastructure.Repositories;
using StackExchange.Redis;
using Domain.Interfaces;
using System.Text.Json.Serialization;
using System.Text.Json;
using Application.Mappings;
using Presentation.Middlewares;
using Presentation.Services;
using Application.Events;
using Infrastructure.Messaging.Handlers;

namespace Presentation
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = false;
            });

            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaxReceiveMessageSize = 2 * 1024 * 1024;
                options.MaxSendMessageSize = 2 * 1024 * 1024;
            });
            services.AddGrpcReflection();



            var mappingAssemblies = new[]
            {
                typeof(MappingProfile).Assembly
            };

            services.AddAutoMapper(mappingAssemblies);


            var connectionString = Configuration.GetConnectionString("PostgreSQL");
            services.AddSingleton<IDbConnectionFactory>(sp => new NpgsqlConnectionFactory(connectionString!));

            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(Infrastructure.Migrations.InitialMigration).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole());

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IStockRepository, StockRepository>();

            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IStockService, StockService>();

            services.AddMemoryCache();
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(Configuration.GetValue<string>("Redis:ConnectionString")!));
            services.AddSingleton<IProductCache, RedisProductCache>();
            services.AddScoped<OrderCancelledEventHandler>();

            services.Configure<KafkaSettings>(Configuration.GetSection("Kafka"));
            services.AddSingleton<IMessageBus, KafkaMessageBus>();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:5173",
                        "http://localhost:8080",          
                        "http://localhost:80",
                        "http://161.104.19.132:8080",        
                        "http://161.104.19.132",              
                        "http://161.104.19.132:5000",     
                        "http://frontend:80",   
                        "http://frontend:8080" 
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });

                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            services.AddResponseCaching();
            services.AddHttpContextAccessor();
            services.AddProblemDetails();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            app.UseMiddleware<ErrorHandlingMiddleware>();
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseResponseCaching();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<ProductGrpcService>();
                endpoints.MapGrpcService<StockGrpcService>();
                endpoints.MapGrpcReflectionService();
            });

            SubscribeToKafkaTopics(app);

            RunMigrations(app, logger);
        }

        private static void RunMigrations(IApplicationBuilder app, ILogger logger)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            try
            {
                runner.MigrateUp();
                logger.LogInformation("Database migrations completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run database migrations");
                if (app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                    throw;
            }
        }

        private void SubscribeToKafkaTopics(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await messageBus.SubscribeAsync<OrderCancelledEvent, OrderCancelledEventHandler>();
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation("Subscribed to OrderCancelledEvent topic");
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                        logger.LogError(ex, "Failed to subscribe to OrderCancelledEvent");
                    }
                });
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                logger.LogError(ex, "Failed to setup Kafka subscriptions");
            }
        }
    }
}
