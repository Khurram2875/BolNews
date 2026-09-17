using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services
{

    public class FuelPriceService : IFuelPriceService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FuelPriceService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public FuelPriceService(
            IWebHostEnvironment environment,
            ILogger<FuelPriceService> logger)
        {
            _environment = environment;
            _logger = logger;
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
                await using var stream =
                    File.OpenRead(filePath);

                var settings =
                    await JsonSerializer.DeserializeAsync<FuelPriceSettingsDto>(
                        stream,
                        JsonOptions,
                        cancellationToken);

                return settings ?? new FuelPriceSettingsDto
                {
                    EffectiveDate = DateTime.Today
                };
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

            await using (var stream =
                File.Create(tempFilePath))
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
        }
    }
}
