using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MjFSv2Lib.Database;
using MjFSv2Lib.Model;

namespace MjFSv2Lib.Meta.Media {
	class MusicMetaProvider : IMetaProvider {
		private HashSet<string> _extensions = new HashSet<string>() { "mp3", "wav", "aac", "midi", "flac", "wma" };
		private static readonly string TABLE_NAME = "MusicExtMeta";

		public HashSet<string> Extensions {
			get {
				return _extensions;
			}
		}

		public string TableName {
			get {
				return TABLE_NAME;
			}
		}

		public TableRow ProcessItem(Item fileItem) {
			TableRow res = new TableRow(TABLE_NAME);
			string path = fileItem.SourcePath;

			TagLib.File file = TagLib.File.Create(path);

			uint year = file.Tag.Year;
			string album = file.Tag.Album;
			string[] artist = file.Tag.AlbumArtists;
			string title = file.Tag.Title;

			res.AddColumn("artist", artist[0]);
			res.AddColumn("album", album);
			res.AddColumn("title", title);

			return res;
		}
	}
}
