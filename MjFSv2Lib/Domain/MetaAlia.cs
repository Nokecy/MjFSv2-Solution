namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class MetaAlia
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(2147483647)]
        public string alias { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(2147483647)]
        public string colName { get; set; }

        [Key]
        [Column(Order = 2)]
        [StringLength(2147483647)]
        public string tableName { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string queryType { get; set; }

        [StringLength(2147483647)]
        public string queryStr { get; set; }

        public virtual MetaTable MetaTable { get; set; }
    }
}
