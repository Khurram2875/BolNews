using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class WordPressRepairResultDto
    {
        public int Total { get; set; }

        public int Repaired { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public List<string> Errors { get; set; } = new();
        public int ImagesProcessed { get; set; }
    }
}
