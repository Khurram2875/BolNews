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

            CreateMap<Article, PublicArticleVM>()
     .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.Name))
     .ForMember(dest => dest.AuthorSlug, opt => opt.MapFrom(src => src.Author.Slug))
     .ForMember(dest => dest.AuthorImage, opt => opt.MapFrom(src => src.Author.ProfileImageUrl))
     .ForMember(dest => dest.CategorySlug, opt => opt.MapFrom(src => src.Category.Slug))
     .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));


            CreateMap<CategoryDto, CategoryVM>().ReverseMap();
            CreateMap<CategoryDto, CategoryTreeVM>().ReverseMap();

            CreateMap<AuthorDto, AuthorVM>().ReverseMap();
        }
    }
}
