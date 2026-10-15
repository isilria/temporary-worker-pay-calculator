# 정식1.4.0 소스 · 파일1.4.0.6

`build.ps1`로 Windows .NET Framework 실행파일을 만듭니다. 루트의 `package.ps1 -OutputDirectory <새 경로>`는 빌드·검사·패키지를 함께 수행합니다.

WorkPlanUi.cs에 CalendarModeTabs·CalendarModeTab과 CalendarColors가 있습니다. 내부 모드는 근무상황부0·계약1·통상2이며 화면 순서는 계약→근무상황부→통상입니다. Controls3.cs의 DayButton.CalendarMode가 모드별 색을 렌더링하고 FlatUi.cs가 탭·달력 배치와 범례를 관리합니다. 근무상황부는 Palette.Accent, 계약은 파스텔 하늘, 통상은 회색입니다.

실제 본체의 --basis-test는122개 교육자료·계약·UI·6개 테마 검사를 실행합니다. --logic-test는 계산40·편성145, --xlsx-test는164개, --update-test는20개를 검사합니다. build-ui.ps1의 FlatUiTests.exe는 화면60개, build-reopen.ps1의 ReopenTests.exe는 복원419·종료25개를 검사합니다. 전체995개 통과 시 패키지를 작성합니다.

화면 검사는 가상자료·격리 설정과 비활성 작업영역 밖 자체 렌더링을 사용합니다. 사용자 바탕화면을 캡처하지 않습니다. PDF 외관·실인쇄는 재검증하지 않았습니다. 주휴 계산·저장 형식은 이전 정식1.4.0과 동일합니다. 상세 적용 범위는 사용안내.md, 실제 결과는 검증결과.txt를 참고하세요.
