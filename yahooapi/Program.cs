var builder = WebApplication.CreateBuilder(args);
// 添加跨域策略服务
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // 允许所有来源
            .AllowAnyMethod()  // 允许所有HTTP方法
            .AllowAnyHeader(); // 允许所有请求头
    });
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll"); 

app.UseAuthorization();

app.MapControllers();

app.Run();