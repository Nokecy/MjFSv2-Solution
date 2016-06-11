namespace MjFSv2Lib.Model {
	public class Tag {
		public string Id { get; set; }
		public bool RootVisible { get; set; }

		public Tag() {

		}

		public Tag(string id, bool rootVisible) {
			Id = id;
			RootVisible = rootVisible;
		}

		public override string ToString() {
			return "Tag " + Id + " root=" + RootVisible;
		}
	}
}
