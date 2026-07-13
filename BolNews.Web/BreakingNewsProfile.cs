using AutoMapper;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels.BreakingNews;

namespace BolNews.Web
{
    public class BreakingNewsProfile : Profile
    {
        public BreakingNewsProfile()
        {
            CreateMap<CreateBreakingNewsVM, BreakingNews>()
                .ReverseMap();

            CreateMap<EditBreakingNewsVM, BreakingNews>()
                .ReverseMap();

            CreateMap<BreakingNews, BreakingNewsVM>()
                .ForMember(dest => dest.ArticleTitle,
                    opt => opt.MapFrom(src =>
                        src.Article != null
                            ? src.Article.Title
                            : string.Empty))

                .ForMember(dest => dest.CreatedBy,
                    opt => opt.MapFrom(src =>
                        src.CreatedByUser != null
                            ? src.CreatedByUser.FullName
                            : string.Empty))

                .ForMember(dest => dest.UpdatedBy,
                    opt => opt.MapFrom(src =>
                        src.UpdatedByUser != null
                            ? src.UpdatedByUser.FullName
                            : string.Empty));
        }
    }
}