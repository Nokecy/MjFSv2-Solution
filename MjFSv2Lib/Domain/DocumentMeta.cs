namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("DocumentMeta")]
    public partial class DocumentMeta
    {
        [Key]
        [StringLength(2147483647)]
        public string itemId { get; set; }

        public virtual ItemMeta ItemMeta { get; set; }
    }
}
