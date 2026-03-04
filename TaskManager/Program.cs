using Microsoft.EntityFrameworkCore;
using TaskManager.Context;
using TaskManager.Interfaces.Categories;
using TaskManager.Interfaces.Tasks;
using TaskManager.Services.Categories;
using TaskManager.Utilities.Configurations;

var builder = WebApplication.CreateBuilder(args);

// --- SECCIÓN DE REGISTRO DE SERVICIOS ---

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registro de DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);

// REGISTRO DE SERVICIOS (Clean Architecture)
// Al pedir a ITaskService, se asgina una instancia de TaskService
builder.Services.AddServices();

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddTransient<GlobalErrorHandlerMiddleware>();

// --- FIN DE SECCIÓN DE SERVICIOS ---

// Construcción de la aplicación
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<GlobalErrorHandlerMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();