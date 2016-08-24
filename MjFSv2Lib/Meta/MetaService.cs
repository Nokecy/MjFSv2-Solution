using MjFSv2Lib.Database;
using MjFSv2Lib.Manager;
using MjFSv2Lib.Meta.Default;
using MjFSv2Lib.Meta.Media;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib.Meta {
	public class MetaService {
		private static Dictionary<string, List<IMetaProvider>> _metaProviderMap = new Dictionary<string, List<IMetaProvider>>(); // Map extension to a list of providers
		private static IMetaProvider _defaultMetaProvider; // Every file at least goes trough this meta provider

		public static object VolumeManager { get; private set; }

		static MetaService() {
			RegisterDefaultProvider(new DefaultMetaProvider());
			RegisterProvider(new DefaultPictureMetaProvider());
			RegisterProvider(new JpegMetaProvider());
			RegisterProvider(new MusicMetaProvider());
		}

		public static void RegisterProvider(IMetaProvider provider) {
			// Register the provider to its supported extensions
			foreach (string ext in provider.Extensions) {
				List<IMetaProvider> currentProviders;
				if (_metaProviderMap.TryGetValue(ext, out currentProviders)) {
					currentProviders.Add(provider);
				} else {
					_metaProviderMap.Add(ext, new List<IMetaProvider>() { provider });
				}
			}

			// TODO: check if meta provider's schema is in the database.
		}

		public static void RegisterDefaultProvider(IMetaProvider provider) {
			_defaultMetaProvider = provider;
		}

		public static void ProcessItem(Item fileItem, DatabaseOperations dbOp) {
			// Set the fileItem's source path. The source path may be used by meta providers.
			fileItem.SourcePath = dbOp.BagLocation + "\\" + fileItem.Id;
			List<IMetaProvider> provOut;
			_metaProviderMap.TryGetValue(fileItem.Extension.ToLower(), out provOut);

			List<IMetaProvider> providers = new List<IMetaProvider>();

			// Add the default provider
			providers.Add(_defaultMetaProvider);

			if (provOut != null) {
				providers.AddRange(provOut);
			} 				
			
			// Iterate over all providers and let them do their thing
			foreach (IMetaProvider provider in providers) {
				TableRow row = provider.ProcessItem(fileItem);
				// Always add an itemId column. This should be the table's key.
				if (row != null) {
					row.AddColumn("itemId", fileItem.Id);
				}
				// Insert the newly obtained metadata in the database
				try {
					dbOp.InsertTableRow(row);
				} catch (SQLiteException ex) {
					DebugLogger.Log(ex.Message);
				}
			}			
		}

	}
}
