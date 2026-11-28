# 1개월 미만 대체근로자 인건비 계산기

## 다운로드

[최신 정식판과 변경 내역](https://github.com/isilria/temporary-worker-pay-calculator/releases/latest)에서 Windows 실행 파일을 받으세요. PDF 저장에는 Microsoft Excel이 필요합니다.

## 사용 안내

대상자·계약 → 근무일·주휴 → 임금·공제 → 명세서·신청서 순으로 입력합니다. 계산자료는 **자료 저장**에서 저장할 수 있습니다. 직종명은 입력하면서 부분 검색할 수 있습니다. 공휴일 자료가 바뀌는 방식은 [공휴일 업데이트 안내](docs/공휴일_업데이트_안내.md)를 참고하세요.

## 이 저장소

공개 배포 전용 저장소입니다. 소스는 비공개 저장소 `isilria/temporary-worker-pay-calculator-source`에서 관리합니다. 여러 프로그램이 이 저장소의 공개 주소를 확인하므로 아래 파일은 **이름과 위치를 바꾸지 않습니다.**

| 위치 | 확인하는 프로그램 |
|---|---|
| `latest.json` | 1개월 미만 대체근로자 인건비 계산기 자동 업데이트 |
| `workday/latest.json` | 교육공무직 320일 근무일 편성 도우미 1.12.x 자동 업데이트 |
| `apps/source-builder/latest.json` | 컴파일 및 배포 도우미 자동 업데이트 |
| `data/holidays.json` | 공휴일 자료 (대체근로자 계산기, 320일 편성 도우미) |

| 그 밖의 위치 | 내용 |
|---|---|
| `scripts/`, `.github/workflows/update-holidays.yml` | 공휴일 자료 자동 수집 ([Actions](https://github.com/isilria/temporary-worker-pay-calculator/actions/workflows/update-holidays.yml)) |
| `CHANGELOG.md` | 변경 내역 |
| GitHub Releases | 버전별 실행 파일과 소스 ZIP. 태그 `v*`는 대체근로자 계산기, `workday-v*`는 320일 편성 도우미, `source-builder-v*`는 컴파일 및 배포 도우미입니다. |
