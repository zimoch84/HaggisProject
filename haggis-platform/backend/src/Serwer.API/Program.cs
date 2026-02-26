var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Serwer.API is running.");

app.Run();

public partial class Program;

