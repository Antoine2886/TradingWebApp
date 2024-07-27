using Bd.Enums;
using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Community
{
    public class CreatePostVM
    {
        [Required(ErrorMessage = "The Title field is required.")]
        [StringLength(100, ErrorMessage = "The Title must be at least {2} and at most {1} characters long.", MinimumLength = 3)]
        public string Title { get; set; }

        [Required(ErrorMessage = "The Content field is required.")]
        public string Content { get; set; }

        // Chart settings
        public bool IncludeChart { get; set; } // Flag to determine if chart should be included

        // Nullable ChartSettings entity
        public string? ChartSettingsJson { get; set; } // JSON string for chart settings

        public string? PostType { get; set; } // Handle as string
    }
}
