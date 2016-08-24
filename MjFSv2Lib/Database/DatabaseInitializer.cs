using MjFSv2Lib.Domain;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MjFSv2Lib.Database {
	class DatabaseInitializer : CreateDatabaseIfNotExists<EntityContext> {
		protected override void Seed(EntityContext dbContext) {
			// seed data


			base.Seed(dbContext);
		}

		
	}
}
