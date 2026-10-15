# 정식1.4.0 소스 (파일1.4.0.3)

사용법·산식·공휴일 처리 전제는 사용안내.md를 참고한다. 교육자료를 반영한 수정 정식1.4.0 소스다.

## 빌드

Windows PowerShell에서 `./build.ps1`. 결과는 `build/ShortTermPayroll.exe`다. EPPlus와 세 출력 양식이 포함되어 있다. 빌드는 Windows .NET Framework4 컴파일러를 사용한다.

## 검증

- `build-basis.ps1` → `build/WorkPlanTests.exe <결과 폴더 절대경로>`: 교육자료40/8=5의 실제 주휴 지급, 전체 계약 평균, 결근·출근 미확인, 계약·비교일 편집, 기존 자료 이관, 네이티브·엑셀 왕복과 메모·확인 체크가 제거된 설정창 실제 적용을 검사한다.
- `build-logic.ps1` → `build/LogicAudit.exe <결과 폴더 절대경로>`: 계산·금액·세 양식 및 근무편성·당직 회귀검사.
- 위 LogicAudit에 `--excel-only <결과 폴더 절대경로>`:164개 엑셀·신청서 표기 검사.
- `build-ui.ps1` → `build/FlatUiTests.exe <결과 폴더 절대경로>`: 세 창 크기·6개 페이지·단일 정사각형 달력 배치 검사.
- `build-reopen.ps1` → `build/ReopenTests.exe <결과 폴더 절대경로> <test-fixtures/legacy-wage.xlsx 절대경로>`: 저장자료 복원과 종료·재실행·기관 설정 보존.
- 본체 `--basis-test`, `--logic-test`, `--xlsx-test`, `--update-test`로 실제 전달 실행파일의 계산·출력·업데이트 회귀검사를 실행할 수 있다. 공개 배포 후 `--update-live-test`로 실제 업데이트 경로를 확인한다.

기존 회귀자료는 PlanTestData가 기존 날짜 목록을 날짜별 약정으로 옮기는 방식으로 준비한다. WorkPlanTests와 제품 실행 경로는 이 어댑터를 사용하지 않는다. 예전의 주별 누적 금액을 정답으로 유지하지 않고, 독립 산식으로 바뀐 기대값을 검증한다.

화면 검사는 가상자료·격리된 설정과 활성화하지 않는 작업영역 밖 검사창을 사용한다. 바탕화면 전체를 캡처하지 않고 프로그램 자체를 렌더링한다. 과거 `--test`·`--holiday-test`는 PDF와 예전 화면 실험을 포함하는 경로이며 이번 전용 검사에 사용하지 않았다.

## 주요 변경 파일

- Engine.cs: 날짜별4주 이하 전체 계약 산정, 4주 상한, 주별 개근·7일 조건과 지급 기준시간 분리.
- WorkPlan.cs·WorkPlanUi.cs: 메모·확인 서명/체크 제거, 즉시 재계산, 축소된 설정창.
- App.cs·FlatUi.cs: 새 계산 근거와 달력 안내.
- WageWorkbook.cs·SessionAndTheme.cs: 자료 버전7과 이전 입력 이관, 재계산 안내.
- ExcelReports.cs: 삭제한 기준 메모를 출력하지 않음.
- WorkPlanTests.cs·LogicAuditTests.cs: 교육자료 예시 및 변경된 산정기간 회귀검사.

검증결과.txt는 실제 검사 완료 후 패키지 작성 시 갱신한다. 검사 통과가 모든 개별 계약에 관한 법적 판단을 의미하지 않는다. PDF 외관·실인쇄는 이번에 재검증하지 않았다.
