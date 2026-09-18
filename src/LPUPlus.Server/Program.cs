using LPUPlus.Server.Auth;
using LPUPlus.Server.Signaling;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<PairingStore>();
builder.Services.AddSingleton<JwtTokenService>(sp => 
    new JwtTokenService(builder.Configuration["JwtSecret"])); // Uses generated secret if null
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddTransient<SignalingHandler>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<LPUPlus.Server.AI.AIService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Support dynamic port allocation (e.g. for Render, Heroku)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

app.UseCors("AllowAll");
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15)
});

// WebSocket Endpoint
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<SignalingHandler>();
        
        var connectionId = Guid.NewGuid().ToString("N");
        await handler.HandleConnectionAsync(webSocket, connectionId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapGet("/", () => "LPU+ Signaling Server is running.");

app.Run();
