namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ItemMeta")]
    public partial class ItemMeta
    {
        [Key]
        [StringLength(2147483647)]
        public string itemId { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string name { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string ext { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string size { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string attr { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string lat { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string lwt { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string ct { get; set; }

        public virtual DocumentMeta DocumentMeta { get; set; }

        public virtual MiscMeta MiscMeta { get; set; }

        public virtual MusicMeta MusicMeta { get; set; }

        public virtual PictureMeta PictureMeta { get; set; }

        public virtual VideoMeta VideoMeta { get; set; }
    }
}
