using System;
using System.IO;

namespace MjFSv2Lib.Model {
	public class Item {
		public string Id { get; set; }
		public string Name { get; set; }
		public long Size { get; set; }
		public string Extension { get; set; }
		public DateTime LastAccesTime { get; set; }
		public DateTime LastWriteTime { get; set; }
		public DateTime CreationTime { get; set; }
		public FileAttributes Attributes { get; set; }

		public Item(string id, string name, string extension, long size, DateTime lat, DateTime lwt, DateTime ct, FileAttributes attr) {
			Id = id;
			Name = name;
			Extension = extension;
			Size = size;
			LastAccesTime = lat;
			LastWriteTime = lwt;
			CreationTime = ct;
			Attributes = attr;
		}

		public override string ToString() {
			return "Item " + Id + " | " + Name + " | " + Extension + " size=" + Size;
		}
	}
}
