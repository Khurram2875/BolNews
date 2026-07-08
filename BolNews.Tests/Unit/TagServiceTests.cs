using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
using Moq;
using Xunit;

namespace BolNews.Tests.Unit
{
    public class TagServiceTests
    {
        [Fact]
        public void ParseTagInput_CommaSeparatedValues_TrimsAndDeduplicates()
        {
            var service = new TagService(Mock.Of<ITagRepository>());

            var tags = service.ParseTagInput("Politics, Economy, politics;  World\nEconomy");

            Assert.Equal(new[] { "Politics", "Economy", "World" }, tags);
        }

        [Fact]
        public void ParseTagInput_JsonArrayValues_TrimsAndDeduplicates()
        {
            var service = new TagService(Mock.Of<ITagRepository>());

            var tags = service.ParseTagInput("[\"AI\", \" ai \", \"Media\"]");

            Assert.Equal(new[] { "AI", "Media" }, tags);
        }

        [Fact]
        public void NormalizeName_CollapsesWhitespaceAndUppercases()
        {
            var service = new TagService(Mock.Of<ITagRepository>());

            var normalized = service.NormalizeName("  breaking   news  ");

            Assert.Equal("BREAKING NEWS", normalized);
        }

        [Fact]
        public async Task ReplaceArticleTagsAsync_ReusesExistingNormalizedTagAndCreatesMissingTag()
        {
            var existing = new Tag
            {
                Id = 7,
                Name = "Politics",
                NormalizedName = "POLITICS",
                Slug = "politics"
            };
            var repository = new Mock<ITagRepository>();
            IReadOnlyCollection<Tag>? replacedTags = null;

            repository
                .Setup(x => x.GetByNormalizedNamesAsync(It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync((IReadOnlyCollection<string> names) =>
                    names.Contains("POLITICS") ? new List<Tag> { existing } : new List<Tag>());
            repository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Tag> { existing });
            repository
                .Setup(x => x.AddTagsAsync(It.IsAny<IEnumerable<Tag>>()))
                .Returns(Task.CompletedTask);
            repository
                .Setup(x => x.ReplaceArticleTagsAsync(12, It.IsAny<IReadOnlyCollection<Tag>>(), "editor"))
                .Callback<int, IReadOnlyCollection<Tag>, string>((_, tags, _) => replacedTags = tags)
                .Returns(Task.CompletedTask);

            var service = new TagService(repository.Object);

            await service.ReplaceArticleTagsAsync(12, "Politics, Economy", "editor");

            Assert.NotNull(replacedTags);
            Assert.Contains(replacedTags!, x => x.Id == 7 && x.Name == "Politics");
            Assert.Contains(replacedTags!, x => x.Name == "Economy" && x.NormalizedName == "ECONOMY" && x.Slug == "economy");
        }
    }
}
