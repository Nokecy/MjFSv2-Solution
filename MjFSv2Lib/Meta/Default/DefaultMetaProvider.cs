using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MjFSv2Lib.Database;
using MjFSv2Lib.Model;

namespace MjFSv2Lib.Meta.Default {
	class DefaultMetaProvider : IMetaProvider {
		private HashSet<string> _extensions = new HashSet<string>() { "" };
		private static readonly string TABLE_NAME = "ItemMeta";

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
			TableRow row = new TableRow(TABLE_NAME);
			row.AddColumn("name", fileItem.Name);
			row.AddColumn("ext", fileItem.Extension.ToLower());
			row.AddColumn("size", fileItem.Size.ToString());
			row.AddColumn("attr", ((int)fileItem.Attributes).ToString());
			row.AddColumn("lat", fileItem.LastAccesTime.ToString());
			row.AddColumn("lwt", fileItem.LastWriteTime.ToString());
			row.AddColumn("ct", fileItem.CreationTime.ToString());
			return row;
		}
	}
} 
