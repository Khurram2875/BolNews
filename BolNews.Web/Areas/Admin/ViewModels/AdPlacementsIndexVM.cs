using BolNews.Domain.Entities;
using BolNews.Web.Services;

namespace BolNews.Web.Areas.Admin.ViewModels;

public sealed class AdPlacementsIndexVM
{
    public List<AdPlacement> Placements { get; set; } = new();
    public AdPlacement NewPlacement { get; set; } = new();
    public IReadOnlyList<AdPlacementZone> Zones { get; set; } = AdPlacementZones.All;
}
