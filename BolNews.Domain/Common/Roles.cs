using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Domain.Common
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Editor = "Editor";
        public const string SubEditor = "SubEditor";
        public const string Author = "Author";
        public const string User = "User";
        public const string Factchecker = "Factchecker";

        public static IReadOnlyList<string> SupportedRoleNames { get; } = Array.AsReadOnly(new[]
        {
            Admin,
            Editor,
            SubEditor,
            Author,
            Factchecker,
            User
        });
    }
}
