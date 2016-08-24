namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PictureJpegMeta")]
    public partial class PictureJpegMeta
    {
        [Key]
        [StringLength(2147483647)]
        public string itemId { get; set; }

        [StringLength(2147483647)]
        public string model { get; set; }

        [StringLength(2147483647)]
        public string iso { get; set; }

        [Column("f-stop")]
        [StringLength(2147483647)]
        public string f_stop { get; set; }

        [StringLength(2147483647)]
        public string artist { get; set; }

        public virtual PictureMeta PictureMeta { get; set; }
    }
}
