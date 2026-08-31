using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressAuthorResolver : IWordPressAuthorResolver
    {
        private const string DefaultPassword = "B@l123456789";
        private const string EmailDomain = "bolnetwork.com";

        private readonly IAuthorRepository _authorRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public WordPressAuthorResolver(
            IAuthorRepository authorRepository,
            UserManager<ApplicationUser> userManager)
        {
            _authorRepository = authorRepository;
            _userManager = userManager;
        }

        public async Task<Author?> ResolveAsync(
            int? wordpressAuthorId,
            string? wordpressAuthorName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(wordpressAuthorName))
                return null;

            var authorName = wordpressAuthorName.Trim();

            // Use the same normalization convention as AuthorService.
            var slug = NormalizeName(authorName);

            // 1. Try to find an existing CMS author by slug.
            var existingAuthor =
                await _authorRepository.FindBySlugAsync(slug);

            if (existingAuthor != null)
                return existingAuthor;

            // 2. Build Identity username/email.
            var emailLocalPart = CreateEmailLocalPart(authorName);

            var email = $"{emailLocalPart}@{EmailDomain}";

            // 3. Check whether the Identity user already exists.
            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                var existingAuthorByUser =
                    await _authorRepository.FindByUserIdAsync(existingUser.Id);

                if (existingAuthorByUser != null)
                    return existingAuthorByUser;
            }

            // 4. Create Identity user.
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = authorName,
                EmailConfirmed = true,
                
            };

            var identityResult =
    await _userManager.CreateAsync(
        user,
        DefaultPassword);

            if (!identityResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    identityResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Unable to create Identity user for WordPress " +
                    $"author '{authorName}': {errors}");
            }

            // Assign the Author role to the newly created Identity user.
            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Author");

            if (!roleResult.Succeeded)
            {
                // Remove the Identity user if role assignment fails.
                await _userManager.DeleteAsync(user);

                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Unable to assign Author role to WordPress " +
                    $"author '{authorName}': {errors}");
            }

            if (!identityResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    identityResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Unable to create Identity user for WordPress " +
                    $"author '{authorName}': {errors}");
            }

            // 5. Create CMS Author linked to Identity user.
            var author = new Author
            {
                Name = authorName,
                Slug = slug,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddHours(5),
                IsDeleted = false,
                Bio="Test Bio"
            };

            await _authorRepository.AddAsync(author);

            return author;
        }

        private static string NormalizeName(string name)
        {
            return name
                .Replace("-", " ")
                .Trim()
                .ToLower();
        }

        private static string CreateEmailLocalPart(string name)
        {
            var value = name.Trim().ToLowerInvariant();

            value = value.Replace(" ", ".");
            value = value.Replace("-", ".");

            value = new string(
                value
                    .Where(c => char.IsLetterOrDigit(c) || c == '.')
                    .ToArray());

            while (value.Contains(".."))
                value = value.Replace("..", ".");

            return value.Trim('.');
        }
    }
}
