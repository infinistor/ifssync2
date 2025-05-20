using System.Data;

namespace IfsSync2Common
{
	public static class DataReaderExtensions
	{
		public static string GetString(this IDataReader reader, string columnName) => reader[columnName]?.ToString() ?? string.Empty;

		public static int GetInt(this IDataReader reader, string columnName) => Convert.ToInt32(reader[columnName]);

		public static long GetLong(this IDataReader reader, string columnName) => Convert.ToInt64(reader[columnName]);

		public static DateTime GetDateTime(this IDataReader reader, string columnName) => Convert.ToDateTime(reader[columnName]);

		public static bool GetBool(this IDataReader reader, string columnName) => Convert.ToBoolean(reader[columnName]);

		public static byte[] GetBytes(this IDataReader reader, string columnName) => (byte[])reader[columnName];

		public static decimal GetDecimal(this IDataReader reader, string columnName) => Convert.ToDecimal(reader[columnName]);

		public static double GetDouble(this IDataReader reader, string columnName) => Convert.ToDouble(reader[columnName]);
	}
}
