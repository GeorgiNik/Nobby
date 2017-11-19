namespace Nobby.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Nobby.Data.Common.Models;

    public class Culture : BaseModel<int>
    {
        public string Name { get; set; }

        public virtual ICollection<Resource> Resources { get; set; }
    }
}