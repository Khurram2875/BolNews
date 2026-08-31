using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressArticleReader : IWordPressArticleReader
    {
        private readonly string _connectionString;

        public WordPressArticleReader(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("WordPressConnection")
                ?? throw new InvalidOperationException(
                    "WordPressConnection is not configured.");
        }

        public async Task<List<WordPressArticleImportDto>> GetArticlesAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
            SELECT
                p.ID AS post_id,

                -- Dates
                p.post_date AS post_date,
                p.post_modified AS post_modified_date,

                -- Basic post information
                p.post_title AS post_title,
                p.post_excerpt AS post_summary,
                p.post_content AS post_content,
                p.post_name AS slug,

                -- Status / type
                p.post_status AS post_status,
                p.post_type AS post_type,

                -- Author
                u.ID AS author_id,
                u.display_name AS author_name,

                -- Primary Category
                primary_category.term_id AS category_id,
                primary_category.name AS category_name,
                primary_category.slug AS category_slug,

                -- Primary Reporter
                (
                    SELECT reporter.term_id
                    FROM bolnews_2023_english.wp_postmeta AS primary_reporter_meta

                    INNER JOIN bolnews_2023_english.wp_terms AS reporter
                        ON reporter.term_id =
                           CAST(primary_reporter_meta.meta_value AS UNSIGNED)

                    INNER JOIN bolnews_2023_english.wp_term_taxonomy AS reporter_tt
                        ON reporter_tt.term_id = reporter.term_id
                        AND reporter_tt.taxonomy = 'reporters'

                    WHERE
                        primary_reporter_meta.post_id = p.ID
                        AND primary_reporter_meta.meta_key =
                            '_yoast_wpseo_primary_reporters'

                    LIMIT 1
                ) AS primary_reporter_id,

                (
                    SELECT reporter.name
                    FROM bolnews_2023_english.wp_postmeta AS primary_reporter_meta

                    INNER JOIN bolnews_2023_english.wp_terms AS reporter
                        ON reporter.term_id =
                           CAST(primary_reporter_meta.meta_value AS UNSIGNED)

                    INNER JOIN bolnews_2023_english.wp_term_taxonomy AS reporter_tt
                        ON reporter_tt.term_id = reporter.term_id
                        AND reporter_tt.taxonomy = 'reporters'

                    WHERE
                        primary_reporter_meta.post_id = p.ID
                        AND primary_reporter_meta.meta_key =
                            '_yoast_wpseo_primary_reporters'

                    LIMIT 1
                ) AS primary_reporter_name,

                (
                    SELECT reporter.slug
                    FROM bolnews_2023_english.wp_postmeta AS primary_reporter_meta

                    INNER JOIN bolnews_2023_english.wp_terms AS reporter
                        ON reporter.term_id =
                           CAST(primary_reporter_meta.meta_value AS UNSIGNED)

                    INNER JOIN bolnews_2023_english.wp_term_taxonomy AS reporter_tt
                        ON reporter_tt.term_id = reporter.term_id
                        AND reporter_tt.taxonomy = 'reporters'

                    WHERE
                        primary_reporter_meta.post_id = p.ID
                        AND primary_reporter_meta.meta_key =
                            '_yoast_wpseo_primary_reporters'

                    LIMIT 1
                ) AS primary_reporter_slug,

                -- All Reporters
                (
                    SELECT GROUP_CONCAT(
                        DISTINCT CONCAT(
                            reporter.term_id,
                            '|||',
                            reporter.name,
                            '|||',
                            reporter.slug
                        )
                        ORDER BY reporter.name
                        SEPARATOR '###'
                    )
                    FROM bolnews_2023_english.wp_term_relationships AS reporter_tr

                    INNER JOIN bolnews_2023_english.wp_term_taxonomy AS reporter_tt
                        ON reporter_tt.term_taxonomy_id =
                           reporter_tr.term_taxonomy_id
                        AND reporter_tt.taxonomy = 'reporters'

                    INNER JOIN bolnews_2023_english.wp_terms AS reporter
                        ON reporter.term_id = reporter_tt.term_id

                    WHERE reporter_tr.object_id = p.ID
                ) AS reporters,

                -- Featured image
                thumb.ID AS featured_image_id,
                thumb.guid AS featured_image_path,
                thumb.post_title AS featured_image_title,

                -- Post tags
                GROUP_CONCAT(
                    DISTINCT CONCAT(
                        tag.name,
                        '|||',
                        tag.slug
                    )
                    ORDER BY tag.name
                    SEPARATOR '###'
                ) AS post_tags,

                -- Featured image tags
                GROUP_CONCAT(
                    DISTINCT CONCAT(
                        image_tag.name,
                        '|||',
                        image_tag.slug
                    )
                    ORDER BY image_tag.name
                    SEPARATOR '###'
                ) AS featured_image_tags

            FROM bolnews_2023_english.wp_posts AS p

            -- Author
            LEFT JOIN bolnews_2023_english.wp_users AS u
                ON u.ID = p.post_author

            -- Featured image ID
            LEFT JOIN bolnews_2023_english.wp_postmeta AS thumbnail_meta
                ON thumbnail_meta.post_id = p.ID
                AND thumbnail_meta.meta_key = '_thumbnail_id'

            -- Featured image attachment
            LEFT JOIN bolnews_2023_english.wp_posts AS thumb
                ON thumb.ID = CAST(thumbnail_meta.meta_value AS UNSIGNED)
                AND thumb.post_type = 'attachment'

            -- Primary Category ID from Yoast
            LEFT JOIN bolnews_2023_english.wp_postmeta AS primary_category_meta
                ON primary_category_meta.post_id = p.ID
                AND primary_category_meta.meta_key =
                    '_yoast_wpseo_primary_category'

            -- Primary Category
            LEFT JOIN bolnews_2023_english.wp_terms AS primary_category
                ON primary_category.term_id =
                   CAST(primary_category_meta.meta_value AS UNSIGNED)

            -- Post tag relationships
            LEFT JOIN bolnews_2023_english.wp_term_relationships AS post_tr
                ON post_tr.object_id = p.ID

            LEFT JOIN bolnews_2023_english.wp_term_taxonomy AS post_tt
                ON post_tt.term_taxonomy_id =
                   post_tr.term_taxonomy_id
                AND post_tt.taxonomy = 'post_tag'

            LEFT JOIN bolnews_2023_english.wp_terms AS tag
                ON tag.term_id = post_tt.term_id

            -- Featured image tag relationships
            LEFT JOIN bolnews_2023_english.wp_term_relationships AS image_tr
                ON image_tr.object_id = thumb.ID

            LEFT JOIN bolnews_2023_english.wp_term_taxonomy AS image_tt
                ON image_tt.term_taxonomy_id =
                   image_tr.term_taxonomy_id
                AND image_tt.taxonomy = 'post_tag'

            LEFT JOIN bolnews_2023_english.wp_terms AS image_tag
                ON image_tag.term_id = image_tt.term_id

            WHERE
                p.post_type = 'post'
                AND p.post_status = 'publish'

                -- Require a valid WordPress primary category
                AND primary_category.term_id IS NOT NULL

                -- Date range
                AND p.post_date >= @fromDate
                AND p.post_date < @toDate

            GROUP BY
                p.ID,
                p.post_date,
                p.post_modified,
                p.post_title,
                p.post_excerpt,
                p.post_content,
                p.post_name,
                p.post_status,
                p.post_type,
                u.ID,
                u.display_name,
                primary_category.term_id,
                primary_category.name,
                primary_category.slug,
                thumb.ID,
                thumb.guid,
                thumb.post_title

            ORDER BY
                p.post_date ASC;
            """;

            var articles = new List<WordPressArticleImportDto>();

            await using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@fromDate",
                fromDate);

            command.Parameters.AddWithValue(
                "@toDate",
                toDate);

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                articles.Add(new WordPressArticleImportDto
                {
                    // Post
                    WordPressPostId =
                        reader.GetInt32(
                            reader.GetOrdinal("post_id")),

                    PostDate =
                        reader.GetDateTime(
                            reader.GetOrdinal("post_date")),

                    PostModifiedDate =
                        reader.GetDateTime(
                            reader.GetOrdinal("post_modified_date")),

                    PostTitle =
                        reader.GetString(
                            reader.GetOrdinal("post_title")),

                    PostSummary =
                        GetNullableString(
                            reader,
                            "post_summary"),

                    PostContent =
                        GetNullableString(
                            reader,
                            "post_content"),

                    Slug =
                        reader.GetString(
                            reader.GetOrdinal("slug")),

                    PostStatus =
                        reader.GetString(
                            reader.GetOrdinal("post_status")),

                    PostType =
                        reader.GetString(
                            reader.GetOrdinal("post_type")),

                    // Author
                    WordPressAuthorId =
                        GetNullableInt(
                            reader,
                            "author_id"),

                    AuthorName =
                        GetNullableString(
                            reader,
                            "author_name"),

                    // Primary Category
                    WordPressCategoryId =
                        GetNullableInt(
                            reader,
                            "category_id"),

                    CategoryName =
                        GetNullableString(
                            reader,
                            "category_name"),

                    CategorySlug =
                        GetNullableString(
                            reader,
                            "category_slug"),

                    // Primary Reporter
                    WordPressPrimaryReporterId =
                        GetNullableInt(
                            reader,
                            "primary_reporter_id"),

                    PrimaryReporterName =
                        GetNullableString(
                            reader,
                            "primary_reporter_name"),

                    PrimaryReporterSlug =
                        GetNullableString(
                            reader,
                            "primary_reporter_slug"),

                    // All reporters
                    Reporters =
                        GetNullableString(
                            reader,
                            "reporters"),

                    // Featured image
                    FeaturedImageId =
                        GetNullableInt(
                            reader,
                            "featured_image_id"),

                    FeaturedImagePath =
                        GetNullableString(
                            reader,
                            "featured_image_path"),

                    FeaturedImageTitle =
                        GetNullableString(
                            reader,
                            "featured_image_title"),

                    // Tags
                    PostTags =
                        GetNullableString(
                            reader,
                            "post_tags"),

                    FeaturedImageTags =
                        GetNullableString(
                            reader,
                            "featured_image_tags")
                });
            }

            return articles;
        }

        private static string? GetNullableString(
            MySqlDataReader reader,
            string column)
        {
            var ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static int? GetNullableInt(
            MySqlDataReader reader,
            string column)
        {
            var ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetInt32(ordinal);
        }
    }
}