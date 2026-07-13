using BolNews.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BolNews.Web.Areas.Admin.ViewModels.BreakingNews
{
    public class EditBreakingNewsVM : BreakingNewsFormVM
    {
        public int Id { get; set; }
        public string? ArticleTitle { get; set; }
    }
}
