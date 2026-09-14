using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services
{
    public class EmailOptions
    {
        public string SmtpHost { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        public string SmtpUsername { get; set; } = string.Empty;

        public string SmtpPassword { get; set; } = string.Empty;

        public string FromEmail { get; set; } = string.Empty;

        public string FromName { get; set; } = "BOL News";

        public bool EnableSsl { get; set; } = true;

        public string PublicBaseUrl { get; set; } = string.Empty;
    }
}
