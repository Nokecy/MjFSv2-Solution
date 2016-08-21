using MjFSv2Lib.Database;
using MjFSv2Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib.Meta {
	public interface IMetaProvider {

		HashSet<string> Extensions { get; }
		string TableName { get; }

		TableRow ProcessItem(Item fileItem);
	}
}
