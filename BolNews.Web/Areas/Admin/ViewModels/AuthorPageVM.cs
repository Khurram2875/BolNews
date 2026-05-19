using BolNews.Domain.Entities;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class AuthorPageVM
    {
        public string Name { get; set; }
        public string Bio { get; set; }
        public string ProfileImage { get; set; }
        public string Slug { get; set; }

        public List<Article> Articles { get; set; }

        public string BaseUrl { get; set; }

        public string SchemaJson { get; set; }
    }
}
