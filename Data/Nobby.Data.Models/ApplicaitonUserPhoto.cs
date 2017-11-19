namespace Nobby.Data.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Nobby.Data.Common.Models;

    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUserPhoto :BaseModel<int>
    {
        public string ContentType { get; set; }

        public byte[] Content { get; set; }

        public string ApplicationUserId { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
    }
}