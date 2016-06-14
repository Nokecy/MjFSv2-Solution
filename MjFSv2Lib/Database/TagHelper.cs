using ExifLib;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MjFSv2Lib.Database {
	/// <summary>
	/// Constructs tags for items
	/// </summary>
	class TagHelper {
		private static readonly List<string> _docExt = new List<string> { "doc", "docx", "odp", "odf", "fodt", "fodp", "osd", "fods", "7z", "iso", "ppt", "pptx", "xls", "xlsx", "rar", "zip", "docm", "docxml", "docz", "txt", "pdf", "cvs", "csv", "rtf", "xml", "xaml", "html", "ini", "js", "conf", "css", "info", "inf" };
		private static readonly List<string> _picExt = new List<string> { "jpg", "png", "gif", "bmp", "dng", "raw", "psd", "pdn", "odg", "fodg", "svg", "ico", "tiff", "dxf" };
		private static readonly List<string> _vidExt = new List<string> { "mp4", "flv", "mov", "mpg", "avi", "wmv", "3gp" };
		private static readonly List<string> _musicExt = new List<string> { "mp3", "wav", "aac", "midi", "flac", "wma" };

		public static List<string> ObtainTag(Item fileItem, string bagLocation) {
			List<string> tagList = new List<string>();
			string ext = fileItem.Extension.ToLower();
			fileItem.OriginalPath = bagLocation + "\\" + fileItem.Id;
			if (ext != null) {
				if (_docExt.Contains(ext)) {
					tagList.Add("document");
					tagList.AddRange(ProcessAny(fileItem));
				} else if (_picExt.Contains(ext)) {
					tagList.AddRange(TagHelper.ProcessPicture(fileItem));
				} else if (_vidExt.Contains(ext)) {
					tagList.Add("videos");
				} else if (_musicExt.Contains(ext)) {
					tagList.AddRange(TagHelper.ProcessMusic(fileItem));
				} else {
					tagList.Add("miscellaneous");
					tagList.AddRange(ProcessAny(fileItem));
				}
			}
			return tagList;
		}

		private static List<string> ProcessMusic(Item fileItem) {
			List<string> tagList = new List<string>();
			tagList.Add("music");

			string path = fileItem.OriginalPath;

			if (File.Exists(path)) {
				Thread.Sleep(500); // This file was most likely copied over, just wait half a sec to aviod IOExceptions on the given file.
				TagLib.File file = TagLib.File.Create(fileItem.OriginalPath);

				uint year = file.Tag.Year;
				string album = file.Tag.Album;
				string[] artist = file.Tag.AlbumArtists;

				if (year != null) {
					tagList.Add(year.ToString());
				}

				if (album != null) {
					tagList.Add(album);
				}

				if (artist != null) {
					for(int i = 0; i < artist.Length; i++) {
						tagList.Add(artist[i]);
					}
				}
			}

			return tagList;
		}

		private static List<string> ProcessPicture(Item fileItem) {
			List<string> tagList = new List<string>();
			tagList.Add("picture");

			string path = fileItem.OriginalPath;
			string ext = fileItem.Extension;

			if (File.Exists(path)) {
				try {
					Thread.Sleep(500); // This file was most likely copied over, just wait half a sec to aviod IOExceptions on the given file.
					using (ExifReader reader = new ExifReader(path)) {
						string model = "";
						if (reader.GetTagValue<string>(ExifTags.Model, out model)) {
							tagList.Add(model);
						}
						DateTime datePictureTaken;
						if (reader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out datePictureTaken)) {
							tagList.Add(datePictureTaken.Year.ToString());
						}
					}
				} catch (ExifLibException ex) {
					DebugLogger.Log(ex.Message);
				} catch (IOException ex) {
					DebugLogger.Log(ex.Message);
				}
				
			} else {
				throw new FileNotFoundException("File at '" + path + "' not found.");
			}
			return tagList;
		}

		private static List<string> ProcessAny(Item fileItem) {
			List<string> tagList = new List<string>();
			DateTime creation = fileItem.CreationTime;
			tagList.Add(creation.Year.ToString());
			return tagList;
		}
	}


	
}
