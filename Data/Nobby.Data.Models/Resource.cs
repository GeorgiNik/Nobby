namespace Nobby.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using Nobby.Data.Common.Models;

    public class Resource : BaseModel<int>
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public virtual Culture Culture { get; set; }
    }
}
