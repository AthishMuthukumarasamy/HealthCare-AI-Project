using Healthcare.API.Services;
using Healthcare.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HealthcareDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HealthcareConnection")));
        
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IPatientService, PatientService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();