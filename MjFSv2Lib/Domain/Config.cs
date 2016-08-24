namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Config")]
    public partial class Config
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(2147483647)]
        public string location { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long version { get; set; }

        [StringLength(2147483647)]
        public string hash { get; set; }
    }
}
