using AutoMapper;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;
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
             .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.AuthorId))
             .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId));

            CreateMap<ArticleVM, ArticleDto>();
            CreateMap<Article, ArticleVM>()
     .ForMember(dest => dest.AuthorName,
         opt => opt.MapFrom(src => src.Author.Name))
     .ForMember(dest => dest.CategoryId,
         opt => opt.MapFrom(src => src.Category.ParentCategoryId));

            CreateMap<Article, PublicArticleVM>().ReverseMap();
     

            CreateMap<CategoryDto, CategoryVM>().ReverseMap();
            CreateMap<CategoryDto, CategoryVM2>().ReverseMap();

            CreateMap<AuthorDto, AuthorVM>().ReverseMap();
        }
    }
}
