namespace IfsSync2Common
{
	public sealed class Schedule : IEquatable<Schedule>
	{
		#region Constants
		/// <summary>하루의 최대 시간(시)</summary>
		public static readonly int MaxHours = 24;
		/// <summary>한 시간의 최대 시간(분)</summary>
		public static readonly int MaxMins = 60;
		/// <summary>하루의 총 분 수 (24시간 * 60분)</summary>
		public static readonly int ONE_DAY = MaxHours * MaxMins;
		#endregion

		#region Properties
		/// <summary>스케줄 ID</summary>
		public int Id { get; set; }
		/// <summary>작업 ID</summary>
		public int JobId { get; set; }
		/// <summary>요일 비트 마스크 (WeekDays 열거형 값의 조합)</summary>
		public int Weeks { get; set; }
		/// <summary>시작 시간 (분 단위, 예: 8시 30분 = 8*60+30 = 510)</summary>
		public int AtTime { get; set; }
		/// <summary>작업 지속 시간 (시간 단위)</summary>
		public int ForHours { get; set; }

		/// <summary>
		/// 설정된 요일을 문자열로 반환
		/// </summary>
		public string StrWeek => DayOfWeekHelper.GetDayString(Weeks);

		/// <summary>
		/// 시작 시간을 HH:MM 형식의 문자열로 반환
		/// </summary>
		public string StrAtTime => $"{AtTime / MaxMins:D02}:{AtTime % MaxMins:D02}";

		/// <summary>
		/// 현재 설정된 요일을 WeekDays 열거형으로 반환
		/// </summary>
		public WeekDays EnabledDays => (WeekDays)Weeks;
		#endregion

		/// <summary>
		/// 스케줄 객체 생성자
		/// </summary>
		public Schedule()
		{
			Id = 0;
			JobId = 0;
			Weeks = 0;
			AtTime = 0;
			ForHours = 0;
		}

		/// <summary>
		/// 요일을 추가합니다.
		/// </summary>
		/// <param name="Week">추가할 요일 비트 마스크</param>
		/// <returns>추가 성공 여부</returns>
		public bool AddWeek(int Week)
		{
			WeekDays currentDays = (WeekDays)Weeks;
			WeekDays newDay = (WeekDays)Week;

			// 이미 해당 요일이 설정되어 있지 않은 경우에만 추가
			if (!currentDays.HasFlag(newDay))
			{
				Weeks = (int)(currentDays | newDay); // 비트 OR 연산으로 요일 추가
				return true;
			}
			return false;
		}

		/// <summary>
		/// 요일을 추가합니다. (열거형 버전)
		/// </summary>
		/// <param name="day">추가할 요일</param>
		/// <returns>추가 성공 여부</returns>
		public bool AddDay(WeekDays day)
		{
			return AddWeek((int)day);
		}

		/// <summary>
		/// 요일을 제거합니다.
		/// </summary>
		/// <param name="Week">제거할 요일 비트 마스크</param>
		/// <returns>제거 성공 여부</returns>
		public bool DelWeek(int Week)
		{
			WeekDays currentDays = (WeekDays)Weeks;
			WeekDays dayToRemove = (WeekDays)Week;

			// 해당 요일이 설정되어 있는 경우에만 제거
			if (currentDays.HasFlag(dayToRemove))
			{
				Weeks = (int)(currentDays & ~dayToRemove); // 비트 AND NOT 연산으로 요일 제거
				return true;
			}
			return false;
		}

		/// <summary>
		/// 요일을 제거합니다. (열거형 버전)
		/// </summary>
		/// <param name="day">제거할 요일</param>
		/// <returns>제거 성공 여부</returns>
		public bool DelDay(WeekDays day)
		{
			return DelWeek((int)day);
		}

		/// <summary>
		/// 시작 시간을 설정합니다.
		/// </summary>
		/// <param name="Hours">시간 (0-23)</param>
		/// <param name="Mins">분 (0-59)</param>
		public void SetAtTime(int Hours, int Mins)
		{
			AtTime = Hours * 60 + Mins; // 시간을 분 단위로 변환하여 저장
		}

		/// <summary>
		/// 현재 시간이 스케줄에 포함되는지 확인합니다.
		/// </summary>
		/// <param name="DayOfTheWeekName">요일 이름</param>
		/// <param name="Hours">시간 (0-23)</param>
		/// <param name="Mins">분 (0-59)</param>
		/// <returns>스케줄 포함 여부</returns>
		public bool IsExistSchedule(string DayOfTheWeekName, int Hours, int Mins)
		{
			int Now = Hours * 60 + Mins;             // 현재 시간
			int EndTimes = AtTime + (ForHours * 60); // 작업 종료시간
			int NextDayTimes = 0;                    // 추가 작업시간

			if (ForHours == 0) EndTimes = ONE_DAY;   // 0이라면 그날까지

			var day = DayOfWeekHelper.GetDayFromName(DayOfTheWeekName);     // 오늘 요일
			var yesterday = DayOfWeekHelper.GetYesterday(day); // 어제 요일

			if (day < 0) return false;

			if (EndTimes > ONE_DAY) NextDayTimes = EndTimes - ONE_DAY; // 24시간을 넘었을 경우 추가작업시간에 할당

			// case 1 : 정해진 요일, 작업시간인지 확인
			if (Now >= AtTime) return DayOfWeekHelper.CheckDayOfTheWeek(Weeks, day);
			// case 2 : 24시간을 초과하여 추가 작업시간인지 확인
			else if (Now <= NextDayTimes) return DayOfWeekHelper.CheckDayOfTheWeek(Weeks, yesterday);
			return false;
		}

		/// <summary>
		/// 현재 시간이 스케줄에 포함되는지 확인합니다.
		/// </summary>
		/// <param name="now">현재 시간</param>
		/// <returns>스케줄 포함 여부</returns>
		public bool ScheduleCheck(DateTime now)
		{
			var dayOfWeek = now.DayOfWeek.ToString();
			var hour = now.Hour;
			var minute = now.Minute;

			return IsExistSchedule(dayOfWeek, hour, minute);
		}

		/// <summary>
		/// 두 스케줄 객체가 동일한지 비교합니다.
		/// </summary>
		/// <param name="Data">비교할 스케줄 객체</param>
		/// <returns>동일 여부</returns>
		public bool Equals(Schedule? Data)
		{
			if (Data == null) return false;
			if (Weeks != Data.Weeks) return false;
			if (AtTime != Data.AtTime) return false;
			if (ForHours != Data.ForHours) return false;
			return true;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Schedule);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Weeks, AtTime, ForHours);
		}
	}
}