using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 添加跨域策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// 添加控制器
builder.Services.AddControllers();

// 配置Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger（开发环境启用）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// 添加自定义ApiKey验证中间件
app.Use(async (context, next) =>
{
    var apiKeysString = builder.Configuration["ApiKey"];
    var validApiKeys = apiKeysString?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(k => k.Trim())
        .ToList() ?? new List<string>();

    if (context.Request.Headers.TryGetValue("Authorization", out var incomingApiKeys))
    {
        var incomingKeys = incomingApiKeys.ToString()
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim());

        if (incomingKeys.Any(k => validApiKeys.Contains(k)))
        {
            await next();
            return;
        }
    }

    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Unauthorized: Invalid ApiKey");
});

// 继续请求
app.UseAuthorization();

app.MapControllers();

app.Run();