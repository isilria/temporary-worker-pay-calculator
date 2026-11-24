# 대체근로자 인건비 계산기

## 다운로드

[최신 정식판과 변경 내역](https://github.com/isilria/temporary-worker-pay-calculator/releases/latest)에서 Windows 실행 파일을 내려받아 실행하세요. PDF 저장에는 Microsoft Excel이 필요합니다.

## 사용 안내

대상자·계약 → 근무일·주휴 → 임금·공제 → 명세서·신청서 순으로 입력합니다. 계산자료는 **자료 저장**에서 저장할 수 있습니다. 직종명은 입력하면서 부분 검색할 수 있습니다.

[공휴일 업데이트 안내](src/공휴일_업데이트_안내.md) · [이전 버전 안내 및 계산 예시](https://github.com/isilria/temporary-worker-pay-calculator/blob/31c4a5aed6487e83f9ba86b99e433d77c02861c5/README.md)

## 소스와 배포

- [`src/`](src/)에서 소스를 관리합니다. Windows에서 `powershell -File .\src\build.ps1`로 빌드할 수 있습니다.
- [`latest.json`](latest.json)은 프로그램이 확인하는 자동 업데이트 정보입니다.
- 릴리스별 실행 파일과 소스 ZIP은 [Releases](https://github.com/isilria/temporary-worker-pay-calculator/releases)에서 확인할 수 있습니다.
- [`data/`](data/)는 공휴일 자료이며 [Actions](https://github.com/isilria/temporary-worker-pay-calculator/actions/workflows/update-holidays.yml)에서 갱신 작업을 확인할 수 있습니다.
