namespace MjFSv2Lib.Domain {
	using System;
	using System.Data.Entity;
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Linq;
	using System.Data.Common;
	using System.Data.Entity.Infrastructure;
	public partial class EntityContext : DbContext {
		public EntityContext()
			: base("name=EntityModel") {
		}

		
		public EntityContext(DbConnection existingConnection) : base(existingConnection, true) {
		}

		public virtual DbSet<DocumentMeta> DocumentMetas { get; set; }
		public virtual DbSet<ItemMeta> ItemMetas { get; set; }
		public virtual DbSet<MetaAlia> MetaAlias { get; set; }
		public virtual DbSet<MetaTable> MetaTables { get; set; }
		public virtual DbSet<MiscMeta> MiscMetas { get; set; }
		public virtual DbSet<MusicExtMeta> MusicExtMetas { get; set; }
		public virtual DbSet<MusicMeta> MusicMetas { get; set; }
		public virtual DbSet<PictureJpegMeta> PictureJpegMetas { get; set; }
		public virtual DbSet<PictureMeta> PictureMetas { get; set; }
		public virtual DbSet<VideoMeta> VideoMetas { get; set; }
		public virtual DbSet<Config> Configs { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder) {
			modelBuilder.Entity<ItemMeta>()
				.HasOptional(e => e.DocumentMeta)
				.WithRequired(e => e.ItemMeta);

			modelBuilder.Entity<ItemMeta>()
				.HasOptional(e => e.MiscMeta)
				.WithRequired(e => e.ItemMeta);

			modelBuilder.Entity<ItemMeta>()
				.HasOptional(e => e.MusicMeta)
				.WithRequired(e => e.ItemMeta);

			modelBuilder.Entity<ItemMeta>()
				.HasOptional(e => e.PictureMeta)
				.WithRequired(e => e.ItemMeta);

			modelBuilder.Entity<ItemMeta>()
				.HasOptional(e => e.VideoMeta)
				.WithRequired(e => e.ItemMeta);

			modelBuilder.Entity<MetaTable>()
				.HasMany(e => e.MetaAlias)
				.WithRequired(e => e.MetaTable)
				.WillCascadeOnDelete(false);

			modelBuilder.Entity<MetaTable>()
				.HasMany(e => e.MetaTables1)
				.WithOptional(e => e.MetaTable1)
				.HasForeignKey(e => e.extends);

			modelBuilder.Entity<MusicMeta>()
				.HasOptional(e => e.MusicExtMeta)
				.WithRequired(e => e.MusicMeta);

			modelBuilder.Entity<PictureMeta>()
				.HasOptional(e => e.PictureJpegMeta)
				.WithRequired(e => e.PictureMeta);
		}
	}
}
