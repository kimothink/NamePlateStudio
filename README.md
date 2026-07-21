# NamePlateStudio

NamePlateStudio는 아크릴/책상용 명패 안쪽에 넣을 이름표를 직접 꾸미고, 입력한 실제 mm 크기로 출력할 수 있는 WPF 프로그램입니다.

## 다운로드

- 최신 설치 파일: [NamePlateStudio-Setup-1.1.0-win-x64.exe](https://github.com/kimothink/NamePlateStudio/releases/download/v1.1.0/NamePlateStudio-Setup-1.1.0-win-x64.exe)
- 소개 페이지: [https://kimothink.github.io/NamePlateStudio/](https://kimothink.github.io/NamePlateStudio/)
- SHA256: `01AFD1A560F5FAF14C99BA6D577841367B01E69DBDA86AED815D2D9494AE9822`

## 주요 기능

- 명패 가로/세로를 mm 단위로 입력하고 실제 크기 기준으로 인쇄
- A4 등 선택한 용지 한 장에 여러 명패 자동 배치
- 여러 참석자의 표시명, 소개 문구, 소속/메모를 각각 다르게 입력
- 글꼴, 글자 크기, 색상, 굵게, 기울임, 정렬, 배경색, 테두리 설정
- 항목별 배경 이미지와 로고/이미지 설정
- 확대/축소 가능한 용지 미리보기
- 프린터 출력과 300 DPI PDF 저장
- 이미지가 함께 압축되는 휴대용 `.nps` 양식 저장/불러오기 (기존 JSON 불러오기 지원)
- NSIS 기반 Windows 설치 파일 생성

## 기술 스택

- WPF
- C#
- .NET 8
- MVVM
- CommunityToolkit.Mvvm
- XAML UI
- PrintDialog 인쇄
- JSON 저장/불러오기

## 프로젝트 구조

```text
NamePlateStudio
├─ Models
│  ├─ NamePlateDesign.cs
│  ├─ NamePlateEntry.cs
│  ├─ PaperSizeOption.cs
│  ├─ PrintLayout.cs
│  └─ NamePlatePlacement.cs
├─ ViewModels
│  └─ MainViewModel.cs
├─ Views
│  ├─ MainWindow.xaml
│  └─ MainWindow.xaml.cs
├─ Services
│  ├─ FileService.cs
│  ├─ ColorDialogService.cs
│  ├─ ImageFileService.cs
│  ├─ PrintLayoutService.cs
│  └─ PrintService.cs
├─ Helpers
│  ├─ UnitConverter.cs
│  ├─ ColorHelper.cs
│  ├─ ImageHelper.cs
│  └─ ImagePathToImageSourceConverter.cs
├─ Installer
│  ├─ Build-Installer.ps1
│  ├─ Create-Icons.ps1
│  └─ NamePlateStudio.nsi
└─ Assets
   ├─ NamePlateStudio.ico
   └─ NamePlateStudioClick.ico
```

## mm 변환 공식

WPF는 1 inch를 96 device independent pixels로 계산합니다. 실제 mm 크기로 출력하기 위해 `Helpers/UnitConverter.cs`에서 다음 공식을 사용합니다.

```csharp
pixel = mm / 25.4 * 96
```

## 실행

```powershell
dotnet build .\NamePlateStudio.csproj
dotnet run --project .\NamePlateStudio.csproj
```

## 설치 파일 만들기

NSIS가 설치되어 있으면 다음 명령으로 self-contained Windows 설치 파일을 만들 수 있습니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\Installer\Build-Installer.ps1 -Version 1.1.0
```

생성 위치:

```text
dist\NamePlateStudio-Setup-1.1.0-win-x64.exe
```
