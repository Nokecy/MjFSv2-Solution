namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("MusicExtMeta")]
    public partial class MusicExtMeta
    {
        [Key]
        [StringLength(2147483647)]
        public string itemId { get; set; }

        [StringLength(2147483647)]
        public string artist { get; set; }

        [StringLength(2147483647)]
        public string album { get; set; }

        [StringLength(2147483647)]
        public string title { get; set; }

        public virtual MusicMeta MusicMeta { get; set; }
    }
}
