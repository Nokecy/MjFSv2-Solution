using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MjFSv2Lib.Database;
using MjFSv2Lib.Model;
using ExifLib;
using MjFSv2Lib.Util;
using System.IO;

namespace MjFSv2Lib.Meta.Media {
	class JpegMetaProvider : IMetaProvider {
		private HashSet<string> _extensions = new HashSet<string>() { "jpg", "jpeg" };
		private static readonly string TABLE_NAME = "PictureJpegMeta";

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

			try {
				using (ExifReader reader = new ExifReader(path)) {
					string outputStr;
					double outputDouble;
					DateTime outputDate;
					UInt16 outputUInt16;

					if (reader.GetTagValue<string>(ExifTags.Artist, out outputStr)) {
						res.AddColumn("artist", outputStr);
					}

					if (reader.GetTagValue<double>(ExifTags.FNumber, out outputDouble)) {
						res.AddColumn("f-stop", Convert.ToString(outputDouble));
					}

					if (reader.GetTagValue<UInt16>(ExifTags.PhotographicSensitivity, out outputUInt16)) {
						res.AddColumn("iso", Convert.ToString(outputUInt16));
					}

					if (reader.GetTagValue<string>(ExifTags.Model, out outputStr)) {
						res.AddColumn("model", outputStr);
					}
				}
			} catch (ExifLibException ex) {
				DebugLogger.Log(ex.Message);
				return null;
			} catch (IOException ex) {
				DebugLogger.Log(ex.Message);
				return null;
			}

			return res;
		}
	}
}
