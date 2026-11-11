
using Healthcare.API.Data;
using Healthcare.API.DTOs;
using Healthcare.API.Models;
using Healthcare.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Healthcare.API.Tests
{
    public class ProviderServiceTests
    {
        private static (HealthcareDbContext Context, ProviderService Service) CreateService()
        {
            var options = new DbContextOptionsBuilder<HealthcareDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new HealthcareDbContext(options);
            var logger = new Mock<ILogger<ProviderService>>();

            var service = new ProviderService(context, logger.Object);

            return (context, service);
        }

        private static ProviderCreateDto CreateProviderDto()
        {
            return new ProviderCreateDto
            {
                FirstName = "Rahul",
                LastName = "Kumar",
                Specialization = "Cardiology",
                Email = "rahul.test@example.com",
                PhoneNumber = "9876543210",
                YearsOfExperience = 10
            };
        }

        [Fact]
        public void GetAllProviders_ReturnsAllProviders()
        {
            using var context = CreateService().Context;
            var service = new ProviderService(
                context,
                Mock.Of<ILogger<ProviderService>>());

            context.Providers.AddRange(
                new Provider
                {
                    FirstName = "Rahul",
                    LastName = "Kumar",
                    Specialization = "Cardiology",
                    Email = "rahul@example.com",
                    YearsOfExperience = 10
                },
                new Provider
                {
                    FirstName = "Priya",
                    LastName = "Sharma",
                    Specialization = "Dermatology",
                    Email = "priya@example.com",
                    YearsOfExperience = 8
                });

            context.SaveChanges();

            var result = service.GetAllProviders();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.FirstName == "Rahul");
            Assert.Contains(result, p => p.FirstName == "Priya");
        }

        [Fact]
        public void GetProviderById_ExistingProvider_ReturnsProvider()
        {
            using var context = CreateService().Context;
            var service = new ProviderService(
                context,
                Mock.Of<ILogger<ProviderService>>());

            var provider = new Provider
            {
                FirstName = "Rahul",
                LastName = "Kumar",
                Specialization = "Cardiology",
                Email = "rahul@example.com",
                YearsOfExperience = 10
            };

            context.Providers.Add(provider);
            context.SaveChanges();

            var result = service.GetProviderById(provider.Id);

            Assert.NotNull(result);
            Assert.Equal(provider.Id, result.Id);
            Assert.Equal("Rahul", result.FirstName);
            Assert.Equal("Cardiology", result.Specialization);
        }

        [Fact]
        public void GetProviderById_MissingProvider_ReturnsNull()
        {
            var (_, service) = CreateService();

            var result = service.GetProviderById(999);

            Assert.Null(result);
        }

        [Fact]
        public void CreateProvider_ValidData_CreatesProvider()
        {
            using var context = CreateService().Context;
            var service = new ProviderService(
                context,
                Mock.Of<ILogger<ProviderService>>());

            var result = service.CreateProvider(CreateProviderDto());

            Assert.True(result.Id > 0);
            Assert.Equal("Rahul", result.FirstName);
            Assert.Equal("Kumar", result.LastName);
            Assert.Equal("Cardiology", result.Specialization);
            Assert.Equal("rahul.test@example.com", result.Email);
            Assert.Equal(10, result.YearsOfExperience);
            Assert.Single(context.Providers);
        }

        [Fact]
        public void UpdateProvider_ExistingProvider_UpdatesDetails()
        {
            using var context = CreateService().Context;
            var service = new ProviderService(
                context,
                Mock.Of<ILogger<ProviderService>>());

            var provider = new Provider
            {
                FirstName = "Rahul",
                LastName = "Kumar",
                Specialization = "Cardiology",
                Email = "rahul@example.com",
                PhoneNumber = "9876543210",
                YearsOfExperience = 5
            };

            context.Providers.Add(provider);
            context.SaveChanges();

            var updateDto = new ProviderCreateDto
            {
                FirstName = "Rahul Updated",
                LastName = "Kumar",
                Specialization = "Neurology",
                Email = "rahul.updated@example.com",
                PhoneNumber = "9999999999",
                YearsOfExperience = 12
            };

            var result = service.UpdateProvider(provider.Id, updateDto);

            Assert.True(result);

            var updatedProvider = context.Providers.Find(provider.Id);

            Assert.NotNull(updatedProvider);
            Assert.Equal("Rahul Updated", updatedProvider.FirstName);
            Assert.Equal("Neurology", updatedProvider.Specialization);
            Assert.Equal("rahul.updated@example.com", updatedProvider.Email);
            Assert.Equal("9999999999", updatedProvider.PhoneNumber);
            Assert.Equal(12, updatedProvider.YearsOfExperience);
        }

        [Fact]
        public void UpdateProvider_MissingProvider_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var result = service.UpdateProvider(999, CreateProviderDto());

            Assert.False(result);
        }

        [Fact]
        public void DeleteProvider_ExistingProvider_DeletesProvider()
        {
            using var context = CreateService().Context;
            var service = new ProviderService(
                context,
                Mock.Of<ILogger<ProviderService>>());

            var provider = new Provider
            {
                FirstName = "Rahul",
                LastName = "Kumar",
                Specialization = "Cardiology",
                Email = "rahul.delete@example.com",
                YearsOfExperience = 10
            };

            context.Providers.Add(provider);
            context.SaveChanges();

            var result = service.DeleteProvider(provider.Id);

            Assert.True(result);
            Assert.Empty(context.Providers);
        }

        [Fact]
        public void DeleteProvider_MissingProvider_ReturnsFalse()
        {
            var (_, service) = CreateService();

            var result = service.DeleteProvider(999);

            Assert.False(result);
        }
    }
}
