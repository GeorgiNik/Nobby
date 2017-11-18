namespace Nobby.Data.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Nobby.Data.Common.Models;

    public class ContactUs : IAuditInfo, IDeletableEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255, MinimumLength = 5)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(1024, MinimumLength = 5)]
        public string Message { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedOn { get; set; }
    }

}
