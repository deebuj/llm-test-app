using OllamaChatApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient and OllamaService
builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddScoped<OllamaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

// Simple root endpoint
app.MapGet("/", () => "Ollama Chat API is running!");

app.Run();
