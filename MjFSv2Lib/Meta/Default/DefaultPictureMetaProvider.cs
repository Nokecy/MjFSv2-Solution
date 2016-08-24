using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MjFSv2Lib.Database;
using MjFSv2Lib.Model;

namespace MjFSv2Lib.Meta.Default {
	class DefaultPictureMetaProvider : IMetaProvider {
		private HashSet<string> _extensions = new HashSet<string>() { "jpg", "jpeg", "png", "gif", "bmp", "dng", "raw", "psd", "pdn", "odg", "fodg", "svg", "ico", "tiff", "dxf" };
		private static readonly string TABLE_NAME = "PictureMeta";

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
			return new TableRow(TABLE_NAME);
		}
	}
}
