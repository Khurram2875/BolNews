namespace BolNews.Web.ViewModels
{
    public class FuelPriceWidgetVM
    {
        public DateTime EffectiveDate { get; set; }

        public decimal PetrolPrice { get; set; }
        public decimal PetrolChange { get; set; }

        public decimal DieselPrice { get; set; }
        public decimal DieselChange { get; set; }

        public bool PetrolPriceIncreased => PetrolChange > 0;
        public bool PetrolPriceDecreased => PetrolChange < 0;

        public bool DieselPriceIncreased => DieselChange > 0;
        public bool DieselPriceDecreased => DieselChange < 0;
    }
}
