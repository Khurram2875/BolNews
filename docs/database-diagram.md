# BolNews Database Diagram

This diagram is based on the current EF Core model in `AppDbContext`, entity classes, and persistence configurations.

```mermaid
erDiagram
    AspNetUsers {
        string Id PK
        string UserName
        string Email
        string FullName
        string ProfileImage
    }

    AspNetRoles {
        string Id PK
        string Name
        string NormalizedName
    }

    AspNetUserRoles {
        string UserId PK,FK
        string RoleId PK,FK
    }

    AspNetUserClaims {
        int Id PK
        string UserId FK
    }

    AspNetUserLogins {
        string LoginProvider PK
        string ProviderKey PK
        string UserId FK
    }

    AspNetUserTokens {
        string UserId PK,FK
        string LoginProvider PK
        string Name PK
    }

    AspNetRoleClaims {
        int Id PK
        string RoleId FK
    }

    Authors {
        int Id PK
        string UserId FK
        string Name
        string Slug
        string Bio
        string ProfileImageUrl
        bool IsDeleted
    }

    Reporters {
        int Id PK
        string Name
        string SourceName
        string Slug
        bool IsDeleted
    }

    Categories {
        int Id PK
        int ParentCategoryId FK
        string Name
        string Slug
        string MetaTitle
        string MetaDescription
        bool IsDeleted
    }

    Articles {
        int Id PK
        int CategoryId FK
        int AuthorId FK
        int ReporterId FK
        string ReviewerUserId FK
        string FactCheckerUserId FK
        string LockedByUserId
        string Title
        string Slug
        bool IsPublished
        datetime PublishedAt
        int WorkflowStatus
        int EditorialPriority
        bool IsDeleted
    }

    ArticleRevisions {
        int Id PK
        int ArticleId FK
        int RevisionNumber
        string ChangedByUserId
        string WorkflowState
        bool IsDeleted
    }

    ArticleAnalytics {
        int Id PK
        int ArticleId FK
        int Impressions
        int Clicks
        datetime Date
    }

    ArticleDiscussionComments {
        int Id PK
        int ArticleId FK
        string UserId FK
        string Message
        datetime CreatedAt
        bool IsDeleted
    }

    Tags {
        int Id PK
        string Name
        string NormalizedName
        string Slug
        bool IsDeleted
    }

    ArticleTags {
        int ArticleId PK,FK
        int TagId PK,FK
        datetime CreatedAt
        string CreatedBy
    }

    FeaturedImageMetadata {
        int Id PK
        int ArticleId FK
        string AltText
        string Caption
        string Credit
        bool IsDeleted
    }

    FeaturedImageTags {
        int FeaturedImageMetadataId PK,FK
        int TagId PK,FK
        datetime CreatedAt
        string CreatedBy
    }

    EditorialPlacements {
        int Id PK
        int ArticleId FK
        string PlacementKey
        int SortOrder
        bool IsDeleted
    }

    BreakingNews {
        int Id PK
        int ArticleId FK
        string CreatedByUserId FK
        string UpdatedByUserId FK
        string Text
        bool IsActive
        int DisplayOrder
        int TickerStyle
        bool IsPinned
        bool IsDeleted
    }

    Notifications {
        int Id PK
        string UserId FK
        string Title
        string Message
        bool IsRead
        string Url
        bool IsDeleted
    }

    AspNetUsers ||--o{ AspNetUserRoles : has
    AspNetRoles ||--o{ AspNetUserRoles : has
    AspNetUsers ||--o{ AspNetUserClaims : has
    AspNetUsers ||--o{ AspNetUserLogins : has
    AspNetUsers ||--o{ AspNetUserTokens : has
    AspNetRoles ||--o{ AspNetRoleClaims : has

    AspNetUsers ||--o| Authors : profile
    Authors ||--o{ Articles : writes
    Reporters ||--o{ Articles : reported_by
    Categories ||--o{ Articles : contains
    Categories ||--o{ Categories : parent_of

    AspNetUsers ||--o{ Articles : reviews
    AspNetUsers ||--o{ Articles : fact_checks

    Articles ||--o{ ArticleRevisions : has
    Articles ||--o{ ArticleAnalytics : tracks
    Articles ||--o{ ArticleDiscussionComments : has
    AspNetUsers ||--o{ ArticleDiscussionComments : comments

    Articles ||--o{ ArticleTags : tagged
    Tags ||--o{ ArticleTags : used_by

    Articles ||--o| FeaturedImageMetadata : has
    FeaturedImageMetadata ||--o{ FeaturedImageTags : tagged
    Tags ||--o{ FeaturedImageTags : image_tag

    Articles ||--o{ EditorialPlacements : placed
    Articles ||--o{ BreakingNews : linked
    AspNetUsers ||--o{ BreakingNews : created
    AspNetUsers ||--o{ BreakingNews : updated
    AspNetUsers ||--o{ Notifications : receives
```

## Relationship Notes

- `Authors.UserId` is a one-to-one profile link to `AspNetUsers.Id`.
- `Articles.AuthorId` is required and points to `Authors.Id`.
- `Articles.CategoryId` is required and points to `Categories.Id`.
- `Articles.ReporterId` is optional and points to `Reporters.Id`; deleting a reporter sets article `ReporterId` to `NULL`.
- `Articles.ReviewerUserId` and `Articles.FactCheckerUserId` point to `AspNetUsers.Id` by convention.
- `ArticleTags` is the article/tag join table.
- `FeaturedImageTags` is the featured-image/tag join table.
- `FeaturedImageMetadata.ArticleId` is unique, making it a one-to-one article image metadata link.
- `EditorialPlacements.ArticleId` links homepage placement records to articles.
- `BreakingNews.ArticleId` is optional and points to articles.
- `Notifications.UserId` points to `AspNetUsers.Id` by convention.
- Identity tables are included because `AppDbContext` inherits from `IdentityDbContext<ApplicationUser>`.
