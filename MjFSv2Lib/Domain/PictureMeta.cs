namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PictureMeta")]
    public partial class PictureMeta
    {
        [Key]
        [StringLength(2147483647)]
        public string itemId { get; set; }

        public virtual ItemMeta ItemMeta { get; set; }

        public virtual PictureJpegMeta PictureJpegMeta { get; set; }
    }
}
