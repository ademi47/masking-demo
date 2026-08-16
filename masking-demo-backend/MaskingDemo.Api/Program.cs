using MaskingDemo.Api;
using MaskingDemo.Api.Data;
using MaskingDemo.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MemberStore>();
builder.Services.AddFieldMasking(builder.Configuration);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger stays out of production - it publishes your entire API surface.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<UnmaskedAccessAuditMiddleware>();
app.MapControllers();

app.Run();
