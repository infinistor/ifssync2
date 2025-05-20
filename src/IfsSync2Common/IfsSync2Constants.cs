/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License. See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
namespace IfsSync2Common
{
	/// <summary> IfsSync2 애플리케이션에서 사용되는 모든 상수를 정의하는 클래스 </summary>
	public static class IfsSync2Constants
	{
		#region 회사 및 일반 정보
		/// <summary> 회사 이름 </summary>
		public const string COMPANY_NAME = "PSPACE";

		/// <summary> 활성 확인 메시지 </summary>
		public const string ALIVE_CHECK = "Alive";

		/// <summary> 불리언 참 값 </summary>
		public const int MY_TRUE = 1;

		/// <summary> 불리언 거짓 값 </summary>
		public const int MY_FALSE = 0;

		/// <summary> 알 수 없음 상태 문자열 </summary>
		public const string UNKNOWN = "Unknown";
		#endregion

		#region 파일 경로 및 이름
		/// <summary> 애플리케이션 루트 경로 </summary>
		public const string ROOT = "C:\\PSPACE\\";

		/// <summary> 데이터베이스 디렉토리 경로 </summary>
		public const string DB_DIRECTORY_NAME = ROOT + "DB\\";

		/// <summary> 로그 디렉토리 경로 </summary>
		public const string LOG_DIRECTORY_NAME = ROOT + "LOG\\";

		/// <summary> 데이터베이스 파일 확장자 </summary>
		public const string DB_EXTENSION_NAME = "db";

		/// <summary> 글로벌 뮤텍스 이름 접두사 </summary>
		public const string MUTEX_GLOBAL_NAME = "Global\\";

		/// <summary> 실행 파일 확장자 </summary>
		public const string EXE = ".exe";

		/// <summary> 레지스트리 루트 경로 </summary>
		public const string REGISTRY_ROOT = "Software\\PSPACE\\IfsSync2\\";

		/// <summary> 네트워크 드라이버 레지스트리 경로 </summary>
		public const string NET_DRIVER_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

		/// <summary> 연결된 연결 활성화 레지스트리 값 이름 </summary>
		public const string NET_DRIVER_ENABLE_LINKED_CONNECTIONS = "EnableLinkedConnections";
		#endregion

		#region 네트워크 및 프로토콜
		/// <summary> HTTP 프로토콜 접두사 </summary>
		public const string HTTP = "http://";

		/// <summary> HTTPS 프로토콜 접두사 </summary>
		public const string HTTPS = "https://";

		/// <summary> 포트 번호 </summary>
		public const int PORT_NUMBER = 58443;

		/// <summary> S3 파일 매니저 기본 포트 </summary>
		public const int S3_FILE_MANAGER_DEFAULT_PORT = 5544;

		/// <summary> 연결 실패 메시지 </summary>
		public const string CONNECT_FAILURE = "ConnectFailure";

		/// <summary> 기본 오류 메시지 </summary>
		public const string DEFAULT_ERROR_MESSAGE = "네트워크 연결이 끊어졌습니다.";
		#endregion

		#region 타임아웃 및 지연
		/// <summary> 기본 상태 확인 지연 (밀리초) </summary>
		public const int DEFAULT_STATUS_CHECK_DELAY = 1 * 1000; // 1초

		/// <summary> 기본 작업 타이머 지연 (밀리초) </summary>
		public const int JOB_TIMER_DELAY = 5 * 1000; // 5초

		/// <summary> 기본 스토리지 타이머 지연 (밀리초) </summary>
		public const int STORAGE_TIMER_DELAY = 60 * 1000; // 60초

		/// <summary> 기본 필터 확인 지연 (밀리초) </summary>
		public const int DEFAULT_FILTER_CHECK_DELAY = 5 * 1000; // 5초

		/// <summary> 기본 트레이 아이콘 지연 (밀리초) </summary>
		public const int DEFAULT_TRAY_ICON_DELAY = 5 * 1000; // 5초

		/// <summary> 기본 발신자 지연 (밀리초) </summary>
		public const int DEFAULT_SENDER_DELAY = 5 * 1000; // 5초

		/// <summary> 기본 발신자 확인 지연 (밀리초) </summary>
		public const int DEFAULT_SENDER_CHECK_DELAY = 5 * 1000; // 5초

		/// <summary> 기본 감시자 확인 지연 (밀리초) </summary>
		public const int DEFAULT_WATCHER_CHECK_DELAY = 5 * 60 * 1000; // 5분

		/// <summary> 짧은 스레드 슬립 (밀리초) </summary>
		public const int DEFAULT_SHORT_SLEEP = 100;

		/// <summary> 일반 스레드 슬립 (밀리초) </summary>
		public const int DEFAULT_MEDIUM_SLEEP = 1000;

		/// <summary> S3 타임아웃 (초) </summary>
		public const int S3_TIMEOUT = 3600; // 1시간

		/// <summary> CURL 요청 타임아웃 (밀리초) </summary>
		public const int CURL_TIMEOUT_DELAY = 20 * 1000; // 20초
		#endregion

		#region UI 관련 상수
		/// <summary> UI 모듈 이름 </summary>
		public const string UI_NAME = "IfsSync2UI";

		/// <summary> 기본 스토리지 이름 </summary>
		public const string MAIN_STORAGE_NAME = "Default";

		/// <summary> UI 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_UI = MUTEX_GLOBAL_NAME + "{{" + UI_NAME + "}}";

		/// <summary> UNC 링크 경로 </summary>
		public const string UNC_TYPE_LINK_PATH = "\\target.lnk";
		#endregion

		#region 트레이 아이콘 관련 상수
		/// <summary> 트레이 아이콘 모듈 이름 </summary>
		public const string TRAY_ICON_NAME = "IfsSync2TrayIcon";

		/// <summary> 트레이 아이콘 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_TRAY_ICON = MUTEX_GLOBAL_NAME + "{" + TRAY_ICON_NAME + "}";

		/// <summary> 아이콘 파일 이름 </summary>
		public const string ICON_FILE_NAME = "file";

		/// <summary> 트레이 아이콘 설정 레지스트리 경로 </summary>
		public const string TRAY_ICON_CONFIG_PATH = REGISTRY_ROOT + "TrayIconConfig";
		#endregion

		#region 필터 관련 상수
		/// <summary> 필터 모듈 이름 </summary>
		public const string FILTER_NAME = "IfsSync2Filter";

		/// <summary> 필터 실행 파일 이름 </summary>
		public const string FILTER_EXE = FILTER_NAME + EXE;

		/// <summary> 필터 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_FILTER = MUTEX_GLOBAL_NAME + "{" + FILTER_NAME + "}";

		/// <summary> 필터 설정 레지스트리 경로 </summary>
		public const string FILTER_CONFIG_PATH = REGISTRY_ROOT + "FilterConfig";

		/// <summary> 필터 드라이브 경로 </summary>
		public const string FILTER_DRIVE_PATH = "Lib\\cbfilter.cab";

		/// <summary> 디버그용 가상 고도 값 </summary>
		public const int ALTITUDE_FAKE_VALUE_FOR_DEBUG = 360000;

		/// <summary> 런타임 라이선스 키 </summary>
		public const string RUNTIME_LICENSE_KEY = "43464E4641444E585246323032313035323336314D3935353434000000000000000000000000000046534143594A4D550000424D54304E304D563539524D0000";
		#endregion

		#region 발신자 관련 상수
		/// <summary> 발신자 모듈 이름 </summary>
		public const string SENDER_NAME = "IfsSync2Sender";

		/// <summary> 발신자 실행 파일 이름 </summary>
		public const string SENDER_EXE = SENDER_NAME + EXE;

		/// <summary> 발신자 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_SENDER = MUTEX_GLOBAL_NAME + "{" + SENDER_NAME + "}";

		/// <summary> 발신자 설정 레지스트리 경로 </summary>
		public const string SENDER_CONFIG_PATH = REGISTRY_ROOT + "SenderConfig";

		/// <summary> 발신자 GUID </summary>
		public const string mGuid = "{adf69b11-073c-493b-8dfe-888054f2fda3}";

		/// <summary> 발신자 타임아웃 (초) </summary>
		public const int SENDER_TIMEOUT = 3600;
		#endregion

		#region 감시자 관련 상수
		/// <summary> 감시자 서비스 이름 </summary>
		public const string WATCHER_SERVICE_NAME = "IfsSync2WatcherService";

		/// <summary> 감시자 서비스 실행 파일 이름 </summary>
		public const string WATCHER_SERVICE_EXE = WATCHER_SERVICE_NAME + EXE;

		/// <summary> 감시자 설정 레지스트리 경로 </summary>
		public const string WATCHER_CONFIG_PATH = REGISTRY_ROOT + "WatcherConfig";

		/// <summary> 감시자 서비스 설정 레지스트리 경로 </summary>
		public const string WATCHER_SERVICE_CONFIG_PATH = REGISTRY_ROOT + "WatcherServiceConfig";

		/// <summary> 감시자 서비스 사용자 정보 조회 URL 경로 </summary>
		public const string WATCHER_SERVICE_GET_USER = "";

		/// <summary> 감시자 서비스 작업 정보 조회 URL 경로 </summary>
		public const string WATCHER_SERVICE_GET_JOBS = "";

		/// <summary> 감시자 서비스 생존 확인 URL 경로 </summary>
		public const string WATCHER_SERVICE_PUT_ALIVE = "CheckAlive/";

		/// <summary> 감시자 서비스 버전 확인 URL 경로 </summary>
		public const string WATCHER_SERVICE_VERSION_CHECK = "CheckUpdate";
		#endregion

		#region 인스턴트 백업 관련 상수
		/// <summary> 인스턴트 백업 작업 이름 </summary>
		public const string INSTANT_BACKUP_NAME = "Instant";

		/// <summary> 인스턴트 백업 레지스트리 루트 경로 </summary>
		public const string INSTANT_REGISTRY_ROOT_NAME = REGISTRY_ROOT + "Instant";
		#endregion

		#region 작업 관련 상수
		/// <summary> 작업 데이터베이스 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_JOB_SQL = MUTEX_GLOBAL_NAME + "{JobDataDB}";

		/// <summary> 기본 호스트 이름 </summary>
		public const string DEFAULT_HOSTNAME_NAME = "Global";

		/// <summary> 기본 작업 이름 </summary>
		public const string DEFAULT_JOB_NAME = "Default";

		/// <summary> 작업 설정 레지스트리 경로 </summary>
		public const string JOB_CONFIG_NAME = "Job\\";

		/// <summary> 기본 블랙리스트 경로 </summary>
		public const string DEFAULT_BLACK_PATH_LIST = @"C:\$WINDOWS.~BT|___allroot___$Recycle.Bin|___allroot___JCK|C:\Program Files (x86)|C:\Program Files|C:\Windows|C:\Users\___alldir___\AppData|C:\ProgramData|C:\Documents and Settings|C:\Users\___alldir___\OneDriveTemp|___allroot___BACKUP|C:\System Volume Information|C:\Users\___alldir___\Dropbox|C:\Users\pspace\eclipse-workspace\IfsSync|";

		/// <summary> 기본 글로벌 작업 이름 </summary>
		public const string DEFAULT_GLOBAL_JOB_NAME = "Global Backup ";

		/// <summary> 작업 데이터베이스 파일 이름 </summary>
		public const string JOB_DB_FILE_NAME = "Job";
		#endregion

		#region 확장자 관련 상수
		/// <summary> 확장자 데이터베이스 이름 </summary>
		public const string EXTENSION_NAME = "Extension";

		/// <summary> 확장자 데이터베이스 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_EXTENSION_NAME = MUTEX_GLOBAL_NAME + "{ExtensionDB}";

		/// <summary> 기본 확장자 목록 </summary>
		public const string DEFAULT_EXTENSION_LIST = "mp3,wav,docx,doc,xlsx,xls,pdf,ppt,pptx,odt,ods,odp,rtf,txt,jpg,png,gif,tiff,ico,svg,webp,csv,json,xml,html,zip,pst,avi,mov,mp4,ogg,wmv,webm";
		#endregion

		#region 사용자 관련 상수
		/// <summary> 사용자 데이터베이스 뮤텍스 이름 </summary>
		public const string MUTEX_NAME_USER_SQL = MUTEX_GLOBAL_NAME + "{UserDataDB}";

		/// <summary> 사용자 데이터베이스 파일 이름 </summary>
		public const string USER_DB_FILE_NAME = "UserData";
		#endregion

		#region CURL 관련 상수
		/// <summary> S3 볼륨 크기 정보 엔드포인트 </summary>
		public const string CURL_GET_S3_VOLUME_SIZE = "/ifss30";

		/// <summary> 총 볼륨 크기 키 </summary>
		public const string CURL_GET_S3_VOLUME_TOTAL_SIZE = "Total";

		/// <summary> 사용된 볼륨 크기 키 </summary>
		public const string CURL_GET_S3_VOLUME_USED_SIZE = "Used";

		/// <summary> 컨텐츠 타입 헤더 값 </summary>
		public const string CURL_CONTENT_TYPE = "application/json";

		/// <summary> POST 메서드 문자열 </summary>
		public const string CURL_POST_METHOD = "POST";

		/// <summary> GET 메서드 문자열 </summary>
		public const string CURL_GET_METHOD = "GET";

		/// <summary> PUT 메서드 문자열 </summary>
		public const string CURL_PUT_METHOD = "PUT";
		#endregion

		#region 로그 관련 상수
		/// <summary> 기본 로그 삭제 기간 (일) </summary>
		public const int DEFAULT_DELETE_DATE = 30;

		/// <summary> 기본 바이너리 사이즈 </summary>
		public const int DEFAULT_BINARY_SIZE = 1024;
		#endregion

		#region AWS 관련 상수
		/// <summary> AWS 플래그 기호 </summary>
		public const string AWS_FLAG = "-";
		#endregion

		#region 파일 처리 관련 상수
		/// <summary> 기본 버퍼 크기 </summary>
		public const int BUFFER_SIZE = 16 * 1024; // 16KB

		/// <summary> 작은 버퍼 크기 </summary>
		public const int SMALL_BUFFER_SIZE = 4 * 1024; // 4KB

		/// <summary> 크기 임계값 </summary>
		public const long SIZE_THRESHOLD = 100 * CapacityUnit.MB; // 100MB

		/// <summary> 기본 멀티파트 업로드 파일 크기 </summary>
		public const long DEFAULT_MULTIPART_UPLOAD_FILE_SIZE = 100 * CapacityUnit.MB; // 100MB

		/// <summary> 기본 멀티파트 업로드 파트 크기 </summary>
		public const long DEFAULT_MULTIPART_UPLOAD_PART_SIZE = 10 * CapacityUnit.MB; // 10MB
		#endregion

		#region 데이터베이스 쿼리 관련 상수
		/// <summary> 기본 조회 제한 </summary>
		public const int DEFAULT_LIMIT = 3000;

		/// <summary> 대용량 조회 제한 </summary>
		public const int LARGE_LIMIT = 10000;

		/// <summary> 최대 조회 제한 </summary>
		public const int MAX_LIMIT = 50000;

		/// <summary> 기본 가져오기 개수 </summary>
		public const int DEFAULT_FETCH_COUNT = 1000;

		/// <summary> 기본 삭제 개수 </summary>
		public const int DEFAULT_DELETE_COUNT = 1000;

		/// <summary> 기본 스레드 개수 </summary>
		public const int DEFAULT_THREAD_COUNT = 10;

		/// <summary> 기본 로그 보존 기간 </summary>
		public const int DEFAULT_LOG_RETENTION = 0; // 무제한
		#endregion

		#region 날짜 형식
		/// <summary> 기본 날짜 형식 </summary>
		public const string DEFAULT_DATE_FORMAT = "yyyy-MM-dd-HH:mm:ss";
		#endregion

		#region 데이터베이스 테이블 이름
		/// <summary> 확장자 목록 테이블 이름 </summary>
		public const string DB_TABLE_EXTENSION_LIST = "ExtensionList";

		/// <summary> 작업 목록 테이블 이름 </summary>
		public const string DB_TABLE_JOB_LIST = "JobList";

		/// <summary> 스케줄 목록 테이블 이름 </summary>
		public const string DB_TABLE_SCHEDULE_LIST = "ScheduleList";

		/// <summary> 일반 사용자 목록 테이블 이름 </summary>
		public const string DB_TABLE_NORMAL_USER_LIST = "NormalUserList";

		/// <summary> 글로벌 사용자 목록 테이블 이름 </summary>
		public const string DB_TABLE_GLOBAL_USER_LIST = "GlobalUserList";

		/// <summary> SQLite 시퀀스 테이블 이름 </summary>
		public const string DB_SQLITE_SEQUENCE = "sqlite_sequence";
		#endregion

		#region 데이터베이스 필드 이름
		/// <summary> ID 필드 이름 </summary>
		public const string DB_FIELD_ID = "Id";

		/// <summary> 시퀀스 필드 이름 </summary>
		public const string DB_FIELD_SEQ = "seq";

		#region 확장자 테이블 필드
		/// <summary> 확장자 필드 이름 </summary>
		public const string DB_FIELD_EXTENSION = "Extension";

		/// <summary> 그룹 필드 이름 </summary>
		public const string DB_FIELD_GROUP = "Group";
		#endregion

		#region 작업 테이블 필드
		/// <summary> 호스트 이름 필드 </summary>
		public const string DB_FIELD_HOSTNAME = "HostName";

		/// <summary> 작업 이름 필드 </summary>
		public const string DB_FIELD_JOBNAME = "JobName";

		/// <summary> 글로벌 여부 필드 </summary>
		public const string DB_FIELD_GLOBAL = "Global";

		/// <summary> 글로벌 사용자 여부 필드 </summary>
		public const string DB_FIELD_IS_GLOBAL_USER = "IsGlobalUser";

		/// <summary> 사용자 ID 필드 </summary>
		public const string DB_FIELD_USER_ID = "UserID";

		/// <summary> 정책 이름 필드 </summary>
		public const string DB_FIELD_POLICY_NAME = "PolicyName";

		/// <summary> 경로 필드 </summary>
		public const string DB_FIELD_PATH = "Path";

		/// <summary> 블랙 경로 필드 </summary>
		public const string DB_FIELD_BLACK_PATH = "BlackPath";

		/// <summary> 블랙 파일 필드 </summary>
		public const string DB_FIELD_BLACK_FILE = "BlackFile";

		/// <summary> 블랙 파일 확장자 필드 </summary>
		public const string DB_FIELD_BLACK_FILE_EXT = "BlackFileExt";

		/// <summary> 화이트 파일 필드 </summary>
		public const string DB_FIELD_WHITE_FILE = "WhiteFile";

		/// <summary> 화이트 파일 확장자 필드 </summary>
		public const string DB_FIELD_WHITE_FILE_EXT = "WhiteFileExt";

		/// <summary> VSS 파일 확장자 필드 </summary>
		public const string DB_FIELD_VSS_FILE_EXT = "VSSFileExt";

		/// <summary> 삭제 여부 필드 </summary>
		public const string DB_FIELD_REMOVE = "Remove";

		/// <summary> 초기화 여부 필드 </summary>
		public const string DB_FIELD_IS_INIT = "IsInit";

		/// <summary> 필터 업데이트 필드 </summary>
		public const string DB_FIELD_FILTER_UPDATE = "FilterUpdate";

		/// <summary> 발신자 업데이트 필드 </summary>
		public const string DB_FIELD_SENDER_UPDATE = "SenderUpdate";
		#endregion

		#region 스케줄 테이블 필드
		/// <summary> 스케줄 작업 ID 필드 </summary>
		public const string DB_FIELD_SCHEDULE_JOB_ID = "JobID";

		/// <summary> 스케줄 주간 필드 </summary>
		public const string DB_FIELD_SCHEDULE_WEEKS = "Weeks";

		/// <summary> 스케줄 시작 시간 필드 </summary>
		public const string DB_FIELD_SCHEDULE_AT_TIME = "AtTime";

		/// <summary> 스케줄 실행 시간 필드 </summary>
		public const string DB_FIELD_SCHEDULE_FOR_HOURS = "ForHours";
		#endregion

		#region 사용자 테이블 필드
		/// <summary> 사용자 이름 필드 </summary>
		public const string DB_FIELD_USERNAME = "UserName";

		/// <summary> URL 필드 </summary>
		public const string DB_FIELD_URL = "URL";

		/// <summary> 액세스 키 필드 </summary>
		public const string DB_FIELD_ACCESS_KEY = "AccessKey";

		/// <summary> 시크릿 키 필드 </summary>
		public const string DB_FIELD_SECRET_KEY = "AccessSecret";

		/// <summary> 스토리지 이름 필드 </summary>
		public const string DB_FIELD_STORAGE_NAME = "StorageName";

		/// <summary> S3 파일 매니저 URL 필드 </summary>
		public const string DB_FIELD_S3_FILE_MANAGER_URL = "S3FileManagerURL";

		/// <summary> 디버그 여부 필드 </summary>
		public const string DB_FIELD_DEBUG = "Debug";

		/// <summary> 업데이트 플래그 필드 </summary>
		public const string DB_FIELD_UPDATE_FLAG = "UpdateFlag";
		#endregion
		#endregion

		#region 레지스트리 키 이름
		/// <summary> 분석 키 </summary>
		public const string REG_KEY_ANALYSIS = "Analysis";

		/// <summary> 실행 중 키 </summary>
		public const string REG_KEY_RUNNING = "Running";

		/// <summary> 총 개수 키 </summary>
		public const string REG_KEY_TOTAL_COUNT = "TotalCount";

		/// <summary> 업로드 개수 키 </summary>
		public const string REG_KEY_UPLOAD_COUNT = "UploadCount";

		/// <summary> 진행률 키 </summary>
		public const string REG_KEY_PERCENT_COUNT = "PercentCount";

		/// <summary> 진행률 표시 형식 </summary>
		public const string PERCENT_FORMAT = "P2";

		/// <summary> 남은 파일 수 키 </summary>
		public const string REG_KEY_REMAINING = "Remaining";

		/// <summary> 남은 파일 크기 키 </summary>
		public const string REG_KEY_REMAINING_SIZE = "RemainingSize";

		/// <summary> 업로드 실패 개수 키 </summary>
		public const string REG_KEY_UPLOAD_FAIL_COUNT = "UploadFailCount";

		/// <summary> 파일 크기 키 </summary>
		public const string REG_KEY_FILE_SIZE = "FileSize";

		/// <summary> 업데이트 지연 키 </summary>
		public const string REG_KEY_DELAY = "UpdateDelay";

		/// <summary> 아이콘 경로 키 </summary>
		public const string REG_KEY_ICON_PATH = "IconPath";

		/// <summary> 루트 경로 키 </summary>
		public const string REG_KEY_ROOT_PATH = "RootPath";
		#endregion

		#region 감시자 관련 레지스트리 키
		/// <summary> 감시자 확인 지연 키 </summary>
		public const string REG_KEY_WATCHER_CHECK_DELAY = "WatcherCheckDelay";

		/// <summary> 감시자 IP 키 </summary>
		public const string REG_KEY_WATCHER_IP = "IP";

		/// <summary> 감시자 포트 키 </summary>
		public const string REG_KEY_WATCHER_PORT = "Port";

		/// <summary> 감시자 PC 이름 키 </summary>
		public const string REG_KEY_WATCHER_PC_NAME = "PcName";

		/// <summary> 감시자 이메일 키 </summary>
		public const string REG_KEY_WATCHER_EMAIL = "Email";
		#endregion

		#region 서비스 관련 상수
		/// <summary> 서비스 이름 접두사 </summary>
		public const string SERVICE_NAME_PREFIX = "IfsSync2";
		/// <summary> 트레이 아이콘 서비스 이름 </summary>
		public const string SERVICE_NAME_TRAY_ICON = SERVICE_NAME_PREFIX + "TrayIconService";
		/// <summary> 필터 서비스 이름 </summary>
		public const string SERVICE_NAME_FILTER = SERVICE_NAME_PREFIX + "FilterService";
		/// <summary> 발신자 서비스 이름 </summary>
		public const string SERVICE_NAME_SENDER = SERVICE_NAME_PREFIX + "SenderService";
		#endregion
	}
} 