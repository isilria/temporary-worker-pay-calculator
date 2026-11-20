# 320일 근무 편성기 배포

1. `src/AppUpdates.cs`의 `DisplayVersion`과 `FileVersion`을 함께 올리고 `RELEASE-NOTES.md`를 갱신합니다.
2. `workday` 폴더의 변경을 main에 반영하거나 GitHub Actions의 Release Workday를 실행합니다.
3. Windows 빌드·검증 통과 후 `workday-v버전` 릴리스에 EXE·소스 ZIP·검증 기록·SHA256SUMS.txt를 게시합니다.
4. 게시 EXE를 다시 받아 SHA-256을 확인한 뒤 `workday/latest.json`을 갱신합니다.

같은 버전은 덮어쓰지 않습니다. 다른 계산기의 latest.json과 최신 릴리스 지정은 변경하지 않습니다.
