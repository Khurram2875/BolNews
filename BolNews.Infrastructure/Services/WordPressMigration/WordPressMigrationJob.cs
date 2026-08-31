using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public enum WordPressMigrationOperation
    {
        Import,
        RepairFeaturedImages,
        NormalizeContent,
        ProcessInlineMedia
    }

    public sealed record WordPressMigrationJob(
    WordPressMigrationOperation Operation,
    DateTime FromDate,
    DateTime ToDate,
    string? CurrentUserId = null,
    int? Take = null);
}
