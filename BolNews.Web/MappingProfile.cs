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
             .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.AuthorName))
             .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName));

            CreateMap<ArticleVM, ArticleDto>();

            CreateMap<CategoryDto, CategoryVM>().ReverseMap();
            CreateMap<CategoryDto, CategoryVM2>().ReverseMap();
        }
    }
}
