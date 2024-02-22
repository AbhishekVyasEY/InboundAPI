using FetchAccountLead;
using Microsoft.Extensions.Caching.Memory;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using CRMConnect;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IQueryParser, QueryParser>();
builder.Services.AddScoped<ILoggers, Loggers>();
builder.Services.AddScoped<ICommonFunction, CommonFunction>();
builder.Services.AddScoped<IFtdgAccLeadExecution, FtdgAccLeadExecution>();
builder.Services.AddScoped<IFtCustDtlExecution, FtCustDtlExecution>();
builder.Services.AddScoped<IFthInsuranceSRDtlExecution, FthInsuranceSRDtlExecution>();
builder.Services.AddSingleton<IMemoryCache,MemoryCache>();


builder.Services.AddLogging();

var app = builder.Build();

/*
    builder.Host.ConfigureAppConfiguration((context, config) => {
        var settings = new ConfigurationBuilder().AddJsonFile("data/config/appsettings.json").Build();
        var keyVaultURL = settings["KeyVaultConfiguration:KeyVaultURL"];
        var keyVaultClientId = settings["KeyVaultConfiguration:ClientId"];
        var keyVaultClientSecret = settings["KeyVaultConfiguration:ClientSecret"];
        config.AddAzureKeyVault(keyVaultURL, keyVaultClientId, keyVaultClientSecret, new DefaultKeyVaultSecretManager());
    });
*/


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
