using CreateLeads;
using Microsoft.Extensions.Caching.Memory;
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
builder.Services.AddScoped<ICreateLeadExecution,CreateLeadExecution>();
builder.Services.AddSingleton<IMemoryCache,MemoryCache>();


builder.Services.AddLogging();

var app = builder.Build();

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
