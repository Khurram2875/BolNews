using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BolNews.Application.Common.Helpers
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string phrase)
        {
            string str = phrase.ToLower();

            // Remove invalid chars
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");

            // Convert multiple spaces into one space
            str = Regex.Replace(str, @"\s+", " ").Trim();

            // Replace spaces with hyphens
            str = str.Replace(" ", "-");

            return str;
        }
    }
}
