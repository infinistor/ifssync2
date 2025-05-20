using Microsoft.Win32;

namespace IfsSync2Common
{
	public static class RegistryUtility
	{
#pragma warning disable CA1416
		public static int GetIntValue(RegistryKey key, string KeyName, int defaultValue = 0)
		=> int.TryParse(key.GetValue(KeyName)?.ToString(), out int value) ? value : defaultValue;

		public static long GetLongValue(RegistryKey key, string KeyName, long defaultValue = 0)
		=> long.TryParse(key.GetValue(KeyName)?.ToString(), out long value) ? value : defaultValue;

		public static bool GetBoolValue(RegistryKey key, string KeyName, bool defaultValue = false)
		=> int.TryParse(key.GetValue(KeyName)?.ToString(), out int value) && value == IfsSync2Constants.MY_TRUE;

		public static string GetStringValue(RegistryKey key, string KeyName, string defaultValue = "")
		=> key.GetValue(KeyName)?.ToString() ?? defaultValue;
#pragma warning restore CA1416
	}
}
