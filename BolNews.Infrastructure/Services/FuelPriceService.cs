using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BolNews.Infrastructure.Services
{
    public class FuelPriceService : IFuelPriceService
    {
        private const string CacheKey = "fuel-price-settings";
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FuelPriceService> _logger;
        private readonly IMemoryCache _cache;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public FuelPriceService(
            IWebHostEnvironment environment,
            ILogger<FuelPriceService> logger,
            IMemoryCache cache)
        {
            _environment = environment;
            _logger = logger;
            _cache = cache;
        }

        private string GetFilePath()
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "Config",
                "fuel-prices.json");
        }

        public async Task<FuelPriceSettingsDto> GetFuelPricesAsync(
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(CacheKey, out FuelPriceSettingsDto? cached)
                && cached is not null)
            {
                return cached;
            }

            var filePath = GetFilePath();

            if (!File.Exists(filePath))
            {
                _logger.LogWarning(
                    "Fuel price configuration file was not found: {FilePath}",
                    filePath);

                return new FuelPriceSettingsDto
                {
                    EffectiveDate = DateTime.Today
                };
            }

            try
            {
                await using var stream = File.OpenRead(filePath);

                var settings = await JsonSerializer.DeserializeAsync<FuelPriceSettingsDto>(
                    stream,
                    JsonOptions,
                    cancellationToken) ?? new FuelPriceSettingsDto
                    {
                        EffectiveDate = DateTime.Today
                    };

                _cache.Set(CacheKey, settings);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to read fuel price configuration.");

                return new FuelPriceSettingsDto
                {
                    EffectiveDate = DateTime.Today
                };
            }
        }

        public async Task SaveFuelPricesAsync(
            FuelPriceSettingsDto settings,
            CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath();
            var directory = Path.GetDirectoryName(filePath)!;

            Directory.CreateDirectory(directory);

            var tempFilePath = filePath + ".tmp";

            await using (var stream = File.Create(tempFilePath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    JsonOptions,
                    cancellationToken);
            }

            File.Move(
                tempFilePath,
                filePath,
                overwrite: true);

            _cache.Set(CacheKey, settings);
        }
    }
}
