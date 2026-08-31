using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class WordPressImportResultDto
    {
        public int Total { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
