# 정식 1.5.0 공휴일 갱신 배포

1. 승인한 변경안을 기본 브랜치에 반영합니다. 인증키는 HOLIDAY_API_KEY 비밀 설정에 암호화해 등록하고 HOLIDAY_COLLECTION_ENABLED=true로 수집을 켭니다.
2. Refresh official holiday data를 수동 실행해 성공 여부와 공개 공휴일 자료를 확인합니다. 최신 기본 브랜치를 받아 빌드에 포함할 자료를 갱신합니다.
3. package.ps1 -OutputDirectory <새 빈 경로>로 빌드·프로그램 검사 1068개와 배포 ZIP을 만듭니다. 별도 수집기 검사12개와 정식 주소 실제 EXE 수신·저장·오프라인·UI 검사를 수행합니다.
4. 새 v1.5.0 릴리스를 draft로 만들고 실행파일·Windows ZIP·소스 ZIP·검증결과·체크섬을 업로드합니다. 업로드 자료를 다시 내려받아 SHA256을 확인한 뒤 게시합니다. 기존 v1.4.0 릴리스는 보존합니다.
5. 공개 릴리스에서 다시 받은 파일을 확인한 후에만 latest.json을 1.5.0.1로 갱신합니다. 실제 공개 EXE의 --update-live-test로 매니페스트·파일·검증값을 확인합니다.

표시는 1.5.0, 파일버전은 1.5.0.1, 태그는 v1.5.0입니다. 파일명은 ShortTermPayroll-1.5.exe, ShortTermPayroll-1.5-windows.zip, ShortTermPayroll-1.5-source.zip입니다. 구 1.4.0과 1.5.0.0 테스트판에서 새 버전을 인식합니다.

인증키 값은 공개 소스·EXE·ZIP·로그에 남기지 않습니다. PDF 외관과 실제 인쇄는 이번 패치에서 재검증하지 않습니다.
