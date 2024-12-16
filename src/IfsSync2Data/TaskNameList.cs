namespace IfsSync2Data
{
	public enum TaskNameList
	{
		None = -1,
		Upload = 0,
		Rename,
		Delete
	}

	// TaskNameList 확장 클래스
	public static class TaskNameListExtension
	{
		public static string ToString(this TaskNameList taskName)
		{
			return taskName.ToStringValue();
		}

		public static string ToStringValue(this TaskNameList taskName)
		{
			return taskName switch
			{
				TaskNameList.Upload => "Upload",
				TaskNameList.Rename => "Rename",
				TaskNameList.Delete => "Delete",
				_ => "None",
			};
		}

		public static TaskNameList ToTaskNameList(this string taskName)
		{
			return taskName switch
			{
				"Upload" => TaskNameList.Upload,
				"Rename" => TaskNameList.Rename,
				"Delete" => TaskNameList.Delete,
				_ => TaskNameList.None,
			};
		}
	}
}