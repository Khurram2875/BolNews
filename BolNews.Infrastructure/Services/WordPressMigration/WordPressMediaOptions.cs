using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public sealed class WordPressMediaOptions
    {
        public const string SectionName = "WordPressMedia";

        public string? LocalRoot { get; set; }

        public bool AllowRemoteFallback { get; set; } = false;
    }
}
