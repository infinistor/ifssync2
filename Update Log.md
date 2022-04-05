## 2.0.0.7
- 성능 개선
    1. 로그 설정을 변경하여 로그레벨에 상관없이 디버깅이 용이 하도록 수정

- 버그 수정
    1. 빌드되지 않는 버그 수정

- 기능 추가
    1. Summary에서 목록을 더블클릭 할 경우 해당 작업으로 이동하는 기능 추가

### 개선 예정
- 성능 개선

- 버그 수정
    1. UI 업데이트가 올바르게 동작하지 않는 문제

- 기능 추가 
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)

### 알려진 이슈
1. 네트워크 드라이브를 백업경로에 포함할 경우 백업이 되지 않을 수 있음.
2. RealTime, Schedule Job에 아웃룩 파일을 백업한다고 설정할 경우 백업이 되지 않을 수 있음
3. UI 업데이트의 동작에 문제가 있어 업데이트가 즉각적으로 반영되지 않음

## 2.0.0.6

### 적용완료
- 성능 개선
    1. CBFS v20.0.7663 으로 업데이트
    2. log4net에 보안 관련 이슈로 인해 v2.0.8에서 v2.0.13으로 업데이트

- 버그 수정
    1. 로그 시간이 24시간이 아닌 12시간 기준으로 프린트 되는 버그 해결

- 기능 추가

### 개선 예정
- 성능 개선

- 버그 수정
    1. UI 업데이트가 올바르게 동작하지 않는 문제

- 기능 추가 
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)

### 알려진 이슈
1. 네트워크 드라이브를 백업경로에 포함할 경우 백업이 되지 않을 수 있음.
2. RealTime, Schedule Job에 아웃룩 파일을 백업한다고 설정할 경우 백업이 되지 않을 수 있음
3. UI 업데이트의 동작에 문제가 있어 업데이트가 즉각적으로 반영되지 않음

## 2.0.0.5

### 적용완료
- 성능 개선
    1. IfsSync2WatcherService에서 서버와의 통신 간격 기본 설정을 1분에서 5분으로 변경
    2. 서버 연결실패로 인스턴트 백업이 실패시 로그에 메시지를 Backup Stop로 변경

- 버그 수정
    1. Filter 업데이트가 올바르게 되지 않는 문제 수정
    2. Filter에서 2개 이상의 글로벌 잡 경로가 재대로 Filter에 등록되지 않는 문제
    3. 로그 시간이 24시간이 아닌 12시간 기준으로 프린트 되는 버그 해결

- 기능 추가
    1. Sender에 Job가 처음 생성시 사용자가 설정한 job의 규칙에 해당하는 파일을 서버에 업로드 하는 기능 추가(초기화)
    2. Fetch Count 적용
    3. Sender Delay 적용
    4. Sender Pause 적용
    5. 현재 날짜로부터 30일 이전 로그 파일 삭제
    6. 트레이메뉴에 일시중지 추가. 일시중지 상태일 경우 재시작으로 메뉴 변경됨
    7. 설치시 재부팅 할 수 있도록 옵션 추가
    8. WatcherService에서 5분 단위로 s3proxy정보를 받아와서 갱신

### 개선 예정
- 성능 개선

- 버그 수정
    1. UI 업데이트가 올바르게 동작하지 않는 문제

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)

### 알려진 이슈
1. 네트워크 드라이브를 백업경로에 포함할 경우 백업이 되지 않을 수 있음.
2. RealTime, Schedule Job에 아웃룩 파일을 백업한다고 설정할 경우 백업이 되지 않을 수 있음
3. UI 업데이트가 동작하지 않을 수 있음

## 2.0.0.4

### 적용완료
- 성능 개선
    1. Summary의 Storage정보를 수정하는 창의 크기조절
    2. 로그인 실패시 메시지 변경(S3 로그인시에 실패하는 이유 추가)
        Service Log : 로그인실패! 네트워크 연결이 끊어졌거나 관리자의 접속허가가 필요할 수 있습니다.
    3. + Storage Credential에 AWS S3 추가 가능
        3.1. Full URL OR Region Infomation(S3)에 리전 정보를 넣으면 AWS S3로 인식. 차후에 자동으로 리전정보를 읽어와서 반영하도록 수정예정
        3.2. Full URL OR Region Infomation(S3)에 http://ip:port형식과 http://DNS:port 형식 모두 사용가능
    4. Default Storage정보를 입력하지 않아도 Storage Credential 추가 가능
    5. + Storage Credential에 대한 UI 오타, 단어 수정
- 버그 수정
    1. TrayIcon의 알림창 오타 수정
    2. Service Log의 오타 수정
    3. Select Extension 관리 문제
        3.1. 존재하지 않는 확장자를 추가 할 수 없는 문제 해결
        3.2. 존재하는 확장자를 추가할 수 있는 문제 해결
    4. VSS 활성화메시지 오류 문제 수정
    5. TaryIcon 초기화 문제 수정
    
- 기능 추가

### 개선 예정
- 성능 개선

- 버그 수정

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)

### 알려진 이슈
1. 네트워크 드라이브를 백업경로에 포함할 경우 백업이 되지 않을 수 있음.
2. RealTime, Schedule Job에 아웃룩 파일을 백업한다고 설정할 경우 백업이 되지 않을 수 있음

## 2.0.0.3

### 적용완료
- 성능 개선
    1. Extension List, Seleted Extension List 정렬
    2. Set Schedule테이블 목록의 헤더를 한글로 변경(Weeks => 요일, AtTime = 시작시각, ForHours=> 수행시간)
    3. TrayIcon의 Popup 정보를 보기 편하도록 개선
    4. TrayIcon의 Popup 정보를 PC부팅 시마다 초기화 하도록 변경
    5. PC에 로그인한 사용자별 Job, Storage 정보를 볼 수 있도록 변경
    6. Service Log에 기록되는 파일 용량을 보기 편하도록 수정(KB 고정이었으나 용량에 따라 단위를 변경)
    7. Instant의 Status를 순서에 맞게 조정(VSS => Analysis => Upload 순서로 변경)
    8. Tab의 배경색을 흰색으로 고정
    9. Service Log에 Instant 백업 진행사항 표시
    
- 버그 수정
    1. 다수의 사용자가 같은 job이름을 생성했을 경우 모두 같은 DB를 사용하는 문제 해결(사용자별 폴더아래에 DB 파일 생성)
    2. CBFS Runtime 라이센스 변경
    3. Service Log에 Instnat Start 메시지가 연속적으로 나오는 문제 수정

- 기능 추가
    1. Summary탭의 Job Information 목록중 원하는 Job을 더블클릭하면 해당 Job의 Storage를 S3 File Manager 창에서 확인가능
    2. S3Browser Window에서 F5를 누르면 새로고침 기능 추가
    3. Summary의 Job Information 목록에서 Job 이름과 Storage이름에 각각 마우스를 가져가면 ToolTip으로 해당 이름을 보여주는 기능 추가
    4. Select Source에서 Path 컬럼의 좌우 길이 자동조절 기능 추가
    5. Storage Information의 각각 Storage의 S3 File Manager URL 정보를 입력하고 수정 가능하도록 기능 추가
        1. 기본적으로 Default Stroage의 IPAddress에 고정된 포트(5544)값을 할당하여 IPAddress:5544 값으로 자동 입력
        2. + Storage Credential로 추가할때는 Default Storage의 S3 File Manager URL 정보를 자동으로 입력하며 추후 사용자가 변경가능
    
### 개선 예정
- 성능 개선

- 버그 수정
    1. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음
        1.1. IfsSMB에서 올바르게 동작하지 않음


- 기능 추가

### 개선 예정
- 성능 개선
    1. Default Storage에 주소값 입력시에 DNS이름을 입력해도 동작하도록 기능 추가

- 버그 수정

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)

    
### 알려진 이슈
1. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

## 2.0.0.2

### 적용완료
- 성능 개선
    1. UNC 타입 폴더 Source Selection에서 선택가능

- 버그 수정
    1. VersionUpdateCheck 기능에 문제가 있어 Global Jobs 목록을 받아오지 못한 문제 해결
    2. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
        2.1. Instant업로드를 중단하라는 메시지를 보내고 스토리지 삭제 거부

- 기능 추가
    1. IfsSync2 UI 중복 실행 금지 기능 추가
    2. MD5비교 업로드로 중복 방지
   
### 개선 예정
- 성능 개선

- 버그 수정
    1. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

- 기능 추가

### 개선 예정
- 성능 개선
    1. Default Storage에 주소값 입력시에 DNS이름을 입력해도 동작하도록 기능 추가

- 버그 수정

- 기능 추가
    1. Jobs 목록을 더블클릭하거나, storage의 볼륨이름을 클릭하면 FileManager창을 열어서 자동 로그인 기능 추가
    2. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)

## 2.0.0.1

### 적용완료
- 성능 개선
    1. Service Log개선
        1. Instant 시작, 종료 메시지 추가
        2. Sender Error 메시지 추가
    2. Filter에서 Monitor로 변경
    3. TrayIcon 툴팁은 글자제한(64자)이 있어 클릭하면 Windows알림창에 요약정보를 보여주도록 변경 및 UI 텍스트 변경

- 버그 수정
    1. 네트워크 드라이브 대용량 파일 업로드 실패 문제
        1.1 Samba에서는 20gb파일까지 문제없이 업로드 됨
        1.2 SecurageClient에서는 대용량파일 업로드시 Hang
    2. TrayIcon 용량 표기 오류 수정
    3. Job에서 선택한 Stoarge가 3, 4번일경우 Job Information에 출력되지 않는 문제 수정
    4. 네트워크드라이버만 필터링했음에도 VSS가 켜지는 문제 수정
    5. 엑셀 파일을 수정하여 저장할때 여러번 기록되는 문제 수정
    6. office 프로그램으로 파일을 수정할때 몇몇 확장자에서 upload가 되지 않는 문제 수정
    7. 워드패드로 파일을 수정해도 upload되지 않는 문제 수정

- 기능 추가
    1. 인터넷 연결이 없을경우
        1. Sender State가 Error로 표시되도록 변경 및 재시도
        2. EventLog에서 서버 연결 실패 메시지(S3 Connect Fail)
    2. UNC 타입 드라이브 인지 기능 추가

### 개선 예정
- 성능 개선

- 버그 수정
    1. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음
        1.1. Cut&Paste에서 최상위 폴더를 제외한 하위폴더에서 발생한 이벤트가 전혀 감지되지 않음

- 기능 추가
    1. Jobs 목록을 더블클릭하거나, storage의 볼륨이름을 클릭하면 FileManager창을 열어서 자동 로그인 기능 추가

### 개선 예정
- 성능 개선
    1. Default Storage에 주소값 입력시에 DNS이름을 입력해도 동작하도록 기능 추가

- 버그 수정
    1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
        1. Instant업로드를 중단하고 스토리지를 삭제
        2. Instant업로드를 중단하라는 메시지를 보내고 스토리지 삭제 거부
        3. [Instant업로드를 중단하고 스토리지를 삭제하시겠습니까?] 메시지 창 보여주기

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)
    2. UNC 타입 폴더 인지 기능 추가
    
### 알려진 이슈
1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
2. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

## 2.0.0.0

### 적용완료
- 성능 개선
    1. IfsSync Jobs => Job Information 변경
    2. Job Information의 State => Status 변경
    3. 메이저 버전을 1에서 2로 전환

- 버그 수정
    1. Extension 목록에 중복값 입력가능한 문제 해결
    2. Filter에서 Rename이 올바르게 동작하지 않는 문제 해결
    3. Main Title, Storage Name에서 _가 나오지 않는 문제 해결
    4. 사용자가 추가한 Storage의 이름이 변경불가능한 문제 해결
    5. 필터링 과정에서 Rename 경로 일부를 제거하는 문제 해결
    6. Job생성에서 중복처리가 올바르게 되지 않는 문제 해결

- 기능 추가
    1. 버전 정보를 서버에 전송하는 기능 추가
    2. 버전정보를 타이틀에 보여주는 기능 추가(임시)
    3. 텍스트를 입력할 수 있는 모든 항목에서 Enter로 입력하는 기능 추가

### 개선 예정
- 성능 개선

- 버그 수정
    1. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음
    2. 로컬 드라이브 디스크 내에서 Cut & Paste 가 올바르게 동작하지 않음
        1. Cut&Paste에서 최상위 폴더를 제외한 하위폴더에서 발생한 이벤트가 전혀 감지되지 않음

- 기능 추가
    1. Jobs 목록을 더블클릭하거나, storage의 볼륨이름을 클릭하면 FileManager창을 열어서 자동 로그인 기능 추가
    2. S3 업로드가 불가능할 경우
        1. Sender State가 Error로 표시되도록 변경 및 재시도
        2. EventLog에서 서버 연결 실패 메시지

### 개선 예정
- 성능 개선
    1. Default Storage에 주소값 입력시에 DNS이름을 입력해도 동작하도록 기능 추가

- 버그 수정
    1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
        1. Instant업로드를 중단하고 스토리지를 삭제
        2. Instant업로드를 중단하라는 메시지를 보내고 스토리지 삭제 거부
        3. [Instant업로드를 중단하고 스토리지를 삭제하시겠습니까?] 메시지 창 보여주기

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)
    2. UNC 타입 드라이브 인지 기능 추가
    
### 알려진 이슈
1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
2. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

## 1.0.4

### 적용완료
- 성능 개선
    1. Instant의 버튼 이름을 Stop => Quit 변경(기능에 알맞는 이름)
    2. Instant의 State 이름을 Monitor => Scanner
    3. Jobs 목록에 State 추가
    4. Tray Icon 클릭시 디테일 정보를 보여줄 수 있도록 변경(Job마다 정보 출력)
    5. Summary의 정보를 한번에 업데이트 하는것이 아닌 각각 업데이트 하는 방식으로 변경
        5.1. Default : Jobs 목록을 5초에 한번 업데이트
        5.2. Default : Storage 목록을 1분에 한번 업데이트
    6. Source Selection 새로고침 버튼 추가

- 버그 수정
    1. 삭제했었던 Job 이름을 재사용 했을 경우, 이전 detail log 가 따라오는 문제 해결
    2. Instant에서 Analysis버튼을 눌러 Analysis가 동작중인 상태에서 UI를 종료하면 UI를 재실행할때 Analysis가 종료되지 않는 문제 해결
    3. Instant에서 Quit버튼을 눌러도 멈추지 않는 문제 해결
    4. Source Selection에서 디렉토리 목록과 선택한 디렉토리 목록이 동기화 되지 않는 문제 해결
    5. MianWindow에서 Focus를 벗어난 뒤 Popup창을 클릭하면 Focus를 회복하지 못하는 문제 해결
    6. Storage 자동업데이트 기능이 서버가 동작하지 않을때 UI도 같이 동작하지 않게 되는 문제 해결(자동업데이트 기능 off)
    7. 로컬 드라이브 디스크 내에서 Cut & Paste 가 올바르게 동작하지 않음
        1. 동작방식을 Filter Control After Event에서 Filter Notify Event로 변경(사용자가 파일을 옮길때 해당 이벤트에 대해 간섭하지 않기 위함)
        2. Filter에서 해당 Directory의 모든 이벤트를 가져와서 확장자는 직접 코딩으로 필터링(기존 방식으로는 폴더의 이동에 대한 필터링이 되지 않음)
    8. TrayIcon이 CPU를 과다하게 사용하는 문제 해결
    
- 기능 추가
    1. S3 로그인 실패시
        1. Summary의 Sob State가 Error로 표시되도록 변경
        2. EventLog에서 서버 연결 실패 메시지
        2. Instnat에서 실패시에 Backup자체를 취소

### 개선 예정
- 성능 개선

- 버그 수정
    1. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

- 기능 추가

### 개선 예정
- 성능 개선
    1. Default Storage에 주소값 입력시에 DNS이름을 입력해도 동작하도록 기능 추가

- 버그 수정
    1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
        1. Instant업로드를 중단하고 스토리지를 삭제
        2. Instant업로드를 중단하라는 메시지를 보내고 스토리지 삭제 거부
        3. [Instant업로드를 중단하고 스토리지를 삭제하시겠습니까?] 메시지 창 보여주기

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)
    2. UNC 타입 드라이브 인지 기능 추가
    3. Jobs 목록을 더블클릭하거나, storage의 볼륨이름을 클릭하면 FileManager창을 열어서 자동 로그인 기능 추가
    
### 알려진 이슈
1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
2. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

## 1.0.3
### 적용완료
- 성능 개선
    1. 타이틀 제목 상하 가운데 정렬, 크기 조절(35 => 30)
    2. 새로고침 버튼 이미지 변경 (저작권자 : Dave Gandy / 주소 : https://www.flaticon.com/kr/authors/dave-gandy)
    3. Selection Extension UI 변경
        1. Selection Extension의 맨위의 텍스트 입력부분에 검색, 추가기능 구현
        2. Selection Extension의 ListBox를 2개로 분리
            1. Extension List는 기본적인 확장자 목록을 가지고 있으며 사용자가 추가가능
            2. Seleted Extension List는 사용자가 백업을 위해 선택한 확장자 목록
            3. 모두제거, 선택한 항목만 제거, 선택한 항목만 추가, 모두추가 버튼 기능 구현
        3.3. Selection Extension 버튼 이미지 추가 (저작권자 : Dave Gandy / 주소 : https://www.flaticon.com/kr/authors/dave-gandy)
    4. Drivers => Lib로 폴더 이름 변경

- 버그 수정
    1. 대용량 단일 파일 업로드 실패
        => 업로드 Timeout을 1시간으로 변경.
        => 1GB이상의 파일일 경우 멀티파츠 업로드로 변경

- 기능 추가

### 개선 예정
- 성능 개선
    1. service Log 구현
    2. Selection Extension UI 변경
        1. Extension List에서 삭제버튼 구현

- 버그 수정
    1. 로컬 드라이브 디스크 내에서 Cut & Paste 가 올바르게 동작하지 않음
    2. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음
    3. 삭제했었던 Job 이름을 재사용 했을 경우, 이전 detail log 가 따라오는 문제
        원인 : 기존 db가 삭제되지 않아 발생.
        해결방법
            1. Job이 삭제될떄 기존 DB를 여러번 지우는 로직 추가.
            2. 새로운 Job이 생성될때 DB 초기화 로직 추가.

- 기능 추가

### 개선 예정
- 성능 개선

- 버그 수정
    1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
        1. Instant업로드를 중단하고 스토리지를 삭제
        2. Instant업로드를 중단하라는 메시지를 보내고 스토리지 삭제 거부
        3. [Instant업로드를 중단하고 스토리지를 삭제하시겠습니까?] 메시지 창 보여주기
    2. Storage 자동업데이트 기능이 서버가 동작하지 않을때 UI도 같이 동작하지 않게 되는 문제

- 기능 추가
    1. Pending 기능 추가(Upload, Rename, Delete 실패후 재시도 할 목록)
    2. UNC 타입 드라이브 인지 기능 추가

    
### 알려진 이슈
1. Instant 업로드 도중 해당 스토리지를 삭제하면 문제 발생
2. 로컬 드라이브 디스크 내에서 Cut & Paste 가 올바르게 동작하지 않음
3. 네트워크 드라이브에서 필터가 올바르게 동작하지 않음

## 1.0.2
### 적용완료
- 성능 개선
    1. WatcherService에서 감시하던 TrayIcon을 작업스캐쥴러 등록으로 변경
    2. 프로그램 삭제시 실행중인 IfsSync2 관련 프로그램 모두 종료후 삭제
    3. 기본 스토리지가 설정될때까지 새로운 스토리지를 추가 할 수 없도록 변경

- 버그 수정
    1. Job 추가시 중복체크가 올바르게 동작하지 않는 문제 해결
    2. GlobalUser가 2명이상 생성되는 문제 해결
    3. Analysis와 Backup 동기화 작업 문제 해결

- 기능 추가
    1. 프로그램 설치후 UI를 바로 실행할 수 있도록 기능 추가

### 개선 예정
- 성능 개선
    1. 프로그램 삭제시 모든 정보(DB포함)를 삭제하도록 변경
        => 프로그램 삭제시 선택할 수 있도록 변경예정

## 1.0.1
### 적용완료
- 성능 개선
    1. Default Storage에서 URL을 잘못 입력했을 경우 예외처리
    2. Default Storage에서 PCName을 자동으로 입력
    3. Install에서 바탕화면에 바로가기 생성 체크박스 추가
    4. Windows10 앱등록(검색으로 실행가능 IfsSyn2)

- 기능 추가
    1. Summary가 5초에 한번 새로고침되도록 기능 추가
    2. 각각의 Job에 Select Storage 옆에 새로고침 기능 추가
## 1.0.0
### 적용완료
- 버그 수정
    1. Storage UI 기능 오류 해결

- 기능 추가
    1. Instant중 정지 버튼 기능 구현
    2. Setup Installer로 프로그램 설치기능 구현

## 1.0.0.9
1. Schdeule에서 ForHours가 재대로 동작하지 않는 문제 해결
2. Summary UI 수정(Selected Extension을 재대로 볼 수 있게 변경. Storage 정보를 보기 쉽도록 글자 크기 변경)
3. Job이 동작중에도 수정가능하도록 변경.
   처음 저장할때 => Register Job   |    2번째부터 => Update Job
   값을 변경하지 않으면 Update Job버튼이 활성화 되지 않음
   저장을 누르더라도 즉시 반영되지 않음. 최대 5초의 지연 발생
   (Filter Update Delay = 5sec, Sender Update Delay = 60sec)
4. Storage가 4개 이상인데도 추가 버튼이 활성화 되는 문제 해결

## 1.0.0.8
1. RealTime에서 업로드가 안되는 문제해결
2. UI업데이트 완료

## 1.0.0.2
1. Guid값 변경(SecurageClient 충돌 이슈)
2. LogView 에서 Ctrl + C 로 로그 정보 복사 가능(다중선택 지원)
3. instant 백업에서 선택된 경로가 삭제된 상태로 analysis를 실행할 경우 프로그램이 강제 종료되는 현상 해결

## 1.0.0.3
UI 변경 (80% 완료)
  스토리지 정보를 추가할 수 있지만 수정이나 삭제기능이 구현되지 않았습니다.
  Instant 백업에서 저장기능이 삭제되고 BackupStart가 그 기능을 대신합니다.
  메뉴얼은 ui에 맞춰 업데이트 되어 있지 않았습니다. 완성되고 변경할 예정입니다.

## 1.0.0.4
- 설치관련 런타임 라이센스 문제를 해결하였습니다.

## 1.0.0.5
- LogView에서 선택된 로그정보를 csv파일로 저장할 수 있는 기능을 추가하였습니다.

## 1.0.0.6
1. 스케쥴이 올바르게 동작하지 않는 문제를 해결
2. UI 95% 완료. 스토리지 정보 확인 및 삭제 가능하나 변경은 불가능

## 1.0.0.7
1. IfsSync2UI에서 스케쥴을 재대로 등록하지 못하는 문제 해결
2. Instant에서 analysis가 한번 실행되면 락 걸리는 문제 해결
3. Job 생성시 이름에 사용불가능한 특수문자 예외처리 기능 추가
## 1.0.0.1
버킷 생성관련 에러를 수정하였습니다.