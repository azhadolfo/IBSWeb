namespace IBS.Utility
{
    public class BrandingOptions
    {
        public const string SectionName = "Branding";

        public string ApplicationName { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CompanyShortName { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string[] AddressLines { get; set; } = [];

        public string TinLabel { get; set; } = string.Empty;

        public string Tin { get; set; } = string.Empty;

        public string NavbarLogoPath { get; set; } = string.Empty;

        public string LoginLogoPath { get; set; } = string.Empty;

        public string DocumentLogoPath { get; set; } = string.Empty;

        public int NavbarLogoWidth { get; set; }

        public int NavbarLogoHeight { get; set; }

        public int LoginLogoWidth { get; set; }

        public int DocumentLogoWidth { get; set; }

        public string FaviconLightPath { get; set; } = string.Empty;

        public string FaviconDarkPath { get; set; } = string.Empty;

        public string PrimaryColor { get; set; } = string.Empty;

        public string PrimaryHoverColor { get; set; } = string.Empty;

        public string SecondaryColor { get; set; } = string.Empty;

        public string AccentColor { get; set; } = string.Empty;

        public static bool IsValid(BrandingOptions options)
        {
            return HasValue(options.ApplicationName)
                   && HasValue(options.CompanyName)
                   && HasValue(options.CompanyShortName)
                   && HasValue(options.LegalName)
                   && options.AddressLines.Any(HasValue)
                   && HasValue(options.TinLabel)
                   && HasValue(options.Tin)
                   && HasValue(options.NavbarLogoPath)
                   && HasValue(options.LoginLogoPath)
                   && HasValue(options.DocumentLogoPath)
                   && options.NavbarLogoWidth > 0
                   && options.NavbarLogoHeight > 0
                   && options.LoginLogoWidth > 0
                   && options.DocumentLogoWidth > 0
                   && HasValue(options.FaviconLightPath)
                   && HasValue(options.FaviconDarkPath)
                   && HasValue(options.PrimaryColor)
                   && HasValue(options.PrimaryHoverColor)
                   && HasValue(options.SecondaryColor)
                   && HasValue(options.AccentColor);
        }

        private static bool HasValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
