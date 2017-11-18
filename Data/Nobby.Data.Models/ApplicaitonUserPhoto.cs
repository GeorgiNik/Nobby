namespace Nobby.Data.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Nobby.Data.Common.Models;

    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUserPhoto : IAuditInfo, IDeletableEntity
    {

        [Key]
        public int Id { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public int ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedOn { get; set; }
    }
}
