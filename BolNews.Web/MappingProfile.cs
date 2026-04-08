using AutoMapper;
using BolNews.Application.DTOs;
using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ArticleVM, ArticleDto>()
    .ForMember(dest => dest.PublishedAt, opt => opt.MapFrom(src => src.PublishedAt ?? DateTime.Now));

            CreateMap<ArticleDto, ArticleVM>()
                .ForMember(dest => dest.AuthorName, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryName, opt => opt.Ignore());
        }
    }
}
