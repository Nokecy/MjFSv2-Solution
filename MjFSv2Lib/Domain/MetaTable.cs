namespace MjFSv2Lib.Domain
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class MetaTable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MetaTable()
        {
            MetaAlias = new HashSet<MetaAlias>();
            MetaTables1 = new HashSet<MetaTable>();
        }

        [Key]
        [StringLength(2147483647)]
        public string tableName { get; set; }

        [StringLength(2147483647)]
        public string friendlyName { get; set; }

        public long rootVisible { get; set; }

        [StringLength(2147483647)]
        public string extends { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<MetaAlias> MetaAlias { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<MetaTable> MetaTables1 { get; set; }

        public virtual MetaTable MetaTable1 { get; set; }
    }
}
