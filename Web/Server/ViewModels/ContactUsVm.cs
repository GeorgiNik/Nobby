using System.ComponentModel.DataAnnotations;

namespace Nobby.Web.Client.Server.ViewModels
{
    public class ContactUsVm
    {        
        [Required]
        [StringLength(255, MinimumLength = 5)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(1024, MinimumLength = 5)]
        public string Message { get; set; }

    }

}