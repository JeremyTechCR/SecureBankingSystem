var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        service = "SecureBankingSystem",
        timestampUtc = DateTimeOffset.UtcNow
    }))
    .WithName("GetHealth");

app.Run();

public partial class Program;
