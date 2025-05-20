using System.Text;

namespace IfsSync2Common
{
	/// <summary>
	/// 요일을 비트 플래그로 표현하는 열거형
	/// </summary>
	[Flags]
	public enum WeekDays
	{
		/// <summary>요일 없음</summary>
		None = 0,            // 0b_0000_0000
		/// <summary>토요일</summary>
		Saturday = 1 << 0,   // 0b_0000_0001
		/// <summary>금요일</summary>
		Friday = 1 << 1,     // 0b_0000_0010
		/// <summary>목요일</summary>
		Thursday = 1 << 2,   // 0b_0000_0100
		/// <summary>수요일</summary>
		Wednesday = 1 << 3,  // 0b_0000_1000
		/// <summary>화요일</summary>
		Tuesday = 1 << 4,    // 0b_0001_0000
		/// <summary>월요일</summary>
		Monday = 1 << 5,     // 0b_0010_0000
		/// <summary>일요일</summary>
		Sunday = 1 << 6,     // 0b_0100_0000
		/// <summary>모든 요일</summary>
		Every = 1 << 7,      // 0b_1000_0000

		/// <summary>주중 (월~금)</summary>
		Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,  // 0b_0011_1110
		/// <summary>주말 (토,일)</summary>
		Weekend = Saturday | Sunday                                   // 0b_0100_0001
	}

	/// <summary>
	/// 요일 관련 유틸리티 클래스
	/// </summary>
	public static class DayOfWeekHelper
	{
		/// <summary>요일 값 목록</summary>
		private static readonly WeekDays[] DayOfTheWeekValues = [WeekDays.Every, WeekDays.Sunday, WeekDays.Monday, WeekDays.Tuesday, WeekDays.Wednesday, WeekDays.Thursday, WeekDays.Friday, WeekDays.Saturday];
		/// <summary>요일 이름 목록</summary>
		public static readonly string[] DayOfTheWeekNameList = ["Every", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

		/// <summary>
		/// 요일 이름으로부터 WeekDays 값을 찾습니다.
		/// </summary>
		/// <param name="dayOfTheWeekName">요일 이름 (예: "Monday")</param>
		/// <returns>WeekDays 값 (찾지 못한 경우 None)</returns>
		public static WeekDays GetDayFromName(string dayOfTheWeekName)
		{
			for (int i = 0; i < DayOfTheWeekNameList.Length; i++)
			{
				if (dayOfTheWeekName.StartsWith(DayOfTheWeekNameList[i], StringComparison.OrdinalIgnoreCase))
					return DayOfTheWeekValues[i];
			}
			return WeekDays.None;
		}

		/// <summary>
		/// 특정 요일의 전날을 반환합니다.
		/// </summary>
		/// <param name="day">요일</param>
		/// <returns>전날 요일</returns>
		public static WeekDays GetYesterday(WeekDays day)
		{
			// DayOfTheWeekValues 배열에서 현재 요일의 인덱스를 찾습니다
			int index = Array.IndexOf(DayOfTheWeekValues, day);
			if (index <= 1) return WeekDays.Saturday;  // Every나 Sunday의 경우
			return DayOfTheWeekValues[index - 1];      // 나머지 요일들
		}

		/// <summary>
		/// 특정 요일이 주어진 요일 비트 마스크에 포함되어 있는지 확인합니다.
		/// </summary>
		/// <param name="weeks">요일 비트 마스크</param>
		/// <param name="day">확인할 요일</param>
		/// <returns>포함 여부</returns>
		public static bool CheckDayOfTheWeek(int weeks, WeekDays day)
		{
			if ((weeks & (int)WeekDays.Every) == (int)WeekDays.Every) 
				return true;

			return (weeks & (int)day) == (int)day;
		}

		/// <summary>
		/// 요일 비트 마스크를 문자열로 변환합니다.
		/// </summary>
		/// <param name="weeks">요일 비트 마스크</param>
		/// <returns>요일 문자열 (예: "Mon Wed Fri")</returns>
		public static string GetDayString(int weeks)
		{
			var data = new StringBuilder();
			var weekDays = (WeekDays)weeks;

			for (int i = 0; i < DayOfTheWeekValues.Length; i++)
			{
				if (weekDays.HasFlag(DayOfTheWeekValues[i]))
					data.Append(DayOfTheWeekNameList[i]).Append(' ');
			}

			return data.ToString();
		}
	}
} 