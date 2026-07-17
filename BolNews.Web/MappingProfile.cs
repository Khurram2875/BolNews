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
             .ForMember(dest => dest.ReporterId, opt => opt.MapFrom(src => src.ReporterId))
             .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId));

            CreateMap<ArticleVM, ArticleDto>();
            CreateMap<ArticleDto, ArticleVM>();
            CreateMap<Article, ArticleVM>()
     .ForMember(dest => dest.AuthorName,
         opt => opt.MapFrom(src => src.Author.Name))
     .ForMember(dest => dest.ReporterName,
         opt => opt.MapFrom(src => src.Reporter != null ? src.Reporter.Name : null))
     .ForMember(dest => dest.ReporterSourceName,
         opt => opt.MapFrom(src => src.Reporter != null ? src.Reporter.SourceName : null))
     .ForMember(dest => dest.CategoryId,
         opt => opt.MapFrom(src => src.Category.ParentCategoryId));

            CreateMap<Article, PublicArticleVM>()
     .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author.Name))
     .ForMember(dest => dest.AuthorSlug, opt => opt.MapFrom(src => src.Author.Slug))
     .ForMember(dest => dest.AuthorImage, opt => opt.MapFrom(src => src.Author.ProfileImageUrl))
     .ForMember(dest => dest.ReporterName, opt => opt.MapFrom(src => src.Reporter != null ? src.Reporter.Name : null))
     .ForMember(dest => dest.ReporterSourceName, opt => opt.MapFrom(src => src.Reporter != null ? src.Reporter.SourceName : null))
     .ForMember(dest => dest.CategorySlug, opt => opt.MapFrom(src => src.Category.Slug))
     .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
     .ForMember(dest => dest.FeaturedImageAltText, opt => opt.MapFrom(src => src.FeaturedImageMetadata != null ? src.FeaturedImageMetadata.AltText : null))
     .ForMember(dest => dest.FeaturedImageCaption, opt => opt.MapFrom(src => src.FeaturedImageMetadata != null ? src.FeaturedImageMetadata.Caption : null))
     .ForMember(dest => dest.FeaturedImageCredit, opt => opt.MapFrom(src => src.FeaturedImageMetadata != null ? src.FeaturedImageMetadata.Credit : null))
     .ForMember(dest => dest.ArticleTags, opt => opt.Ignore())
     .ForMember(dest => dest.FeaturedImageTags, opt => opt.Ignore())
     .AfterMap((src, dest) =>
     {
         dest.ArticleTags = src.ArticleTags
             .Where(at => at.Tag != null)
             .Select(at => new TagDto { Id = at.Tag.Id, Name = at.Tag.Name, Slug = at.Tag.Slug })
             .OrderBy(t => t.Name)
             .ToList();

         dest.FeaturedImageTags = src.FeaturedImageMetadata?.FeaturedImageTags
             .Where(ft => ft.Tag != null)
             .Select(ft => new TagDto { Id = ft.Tag.Id, Name = ft.Tag.Name, Slug = ft.Tag.Slug })
             .OrderBy(t => t.Name)
             .ToList() ?? new List<TagDto>();
     });


            CreateMap<CategoryDto, CategoryVM>().ReverseMap();
            CreateMap<CategoryDto, CategoryTreeVM>().ReverseMap();

            CreateMap<AuthorDto, AuthorVM>().ReverseMap();
        }
    }
}
