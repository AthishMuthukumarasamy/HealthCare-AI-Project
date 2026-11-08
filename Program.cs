using Healthcare.API.Services;
using Healthcare.API.Data;
using Microsoft.EntityFrameworkCore;
using Healthcare.API.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HealthcareDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HealthcareConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<HealthcareDbContext>()
    .AddDefaultTokenProviders();        
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IPatientService, PatientService>();

builder.Services.AddScoped<IProviderService, ProviderService>();

builder.Services.AddScoped<IAppointmentService, AppointmentService>();

builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();