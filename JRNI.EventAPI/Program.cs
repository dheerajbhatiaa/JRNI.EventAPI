using JRNI.EventAPI;
using JRNI.EventAPI.Implementation;
using JRNI.EventAPI.Interface;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IEventApiService, EventApiService>();

// Add HttpClient with Polly retry policy and configure base address
builder.Services.AddHttpClient(Constants.EventApi, c =>
    {
        c.BaseAddress = new Uri(builder.Configuration[Constants.BaseUrl]);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    })
    .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)))
    .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));

builder.Services.AddLogging(builder => builder.AddConsole());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    app.Urls.Add(builder.Configuration[Constants.DevEndPoint]);
}
else
{
    // Production-specific configurations, including HTTPS
    app.UseHttpsRedirection();
    app.Urls.Add(builder.Configuration[Constants.ProdEndPoint]);
}

app.UseAuthorization();
app.MapControllers();
app.Run();