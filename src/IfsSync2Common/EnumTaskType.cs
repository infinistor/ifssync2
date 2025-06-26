namespace IfsSync2Common
{
	public enum EnumTaskType
	{
		None = -1,
		Upload = 0,
		Rename,
		Delete
	}

	public static class EnumTaskTypeExtensions
	{
		public static string ToStr(this EnumTaskType taskType)
		{
			return taskType.ToString();
		}

		public static EnumTaskType ToEnum(this string taskType)
		{
			return (EnumTaskType)Enum.Parse(typeof(EnumTaskType), taskType);
		}
	}
}
