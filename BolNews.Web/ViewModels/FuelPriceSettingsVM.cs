using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.ViewModels
{
    public class FuelPriceSettingsVM
    {
        [DataType(DataType.Date)]
        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; }

        [Range(0, 9999)]
        [Display(Name = "Petrol Price")]
        public decimal PetrolPrice { get; set; }

        [Display(Name = "Petrol Change")]
        public decimal PetrolChange { get; set; }

        [Range(0, 9999)]
        [Display(Name = "Diesel Price")]
        public decimal DieselPrice { get; set; }

        [Display(Name = "Diesel Change")]
        public decimal DieselChange { get; set; }
    }
}
