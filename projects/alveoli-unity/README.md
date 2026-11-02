# 허파꽈리 기체 교환 Unity 프로젝트

[웹에서 실행](https://ryowoo2726-hue.github.io/sci-simulation/simulations/alveoli-unity/)

허파꽈리·모세혈관·적혈구와 산소/이산화 탄소 이동을 관찰하는 교육용 3D 시뮬레이션입니다. 단면 보기, 장벽 확대, 기체 표시, 분압 평형 비교와 재생 속도 조절을 지원합니다.

## 프로젝트 열기

Unity Hub에서 이 폴더를 프로젝트로 추가하고 Unity 6000.3.27f1로 엽니다. URP 17.3.0 패키지를 사용합니다. 처음 임포트하면 씬이 생성됩니다. 필요하면 `Alveoli > Create or rebuild simulation scene` 메뉴로 다시 생성합니다.

자세한 조작법과 과학적 단순화 범위는 [사용방법.md](사용방법.md)를 참고하세요.

## WebGL 빌드

Unity 명령줄에서 다음 옵션을 지정합니다.

```text
-batchmode -quit -projectPath "프로젝트 절대 경로" -buildTarget WebGL -executeMethod BuildAlveoli.BuildWebGL -logFile "빌드 로그 경로"
```

프로젝트 상위 폴더의 `Alveoli-WebGL/`에 결과가 생성됩니다. GitHub Pages에 맞춰 Unity 자체 파일 압축을 끄고, 코드에서 찾는 생체 조직 셰이더를 빌드에 포함합니다.

Node.js로 `node Tools/Prepare-WebGL.cjs ../Alveoli-WebGL ../../simulations/alveoli-unity`를 실행해 배포 폴더를 생성합니다. 이 도구는 데이터·WebAssembly 파일을 gzip 압축 조각으로 바꾸고, 다운로드 진행률·시간 제한·자동 재시도를 제공하는 로더를 생성합니다. 동시에 최대 4개만 다운로드하며, Unity의 파일 해시 이름으로 이전 빌드와의 캐시 혼합을 방지합니다. 별도의 배포 폴더를 사용해야 하며, 해당 폴더의 `Build/` 파일은 생성 시 교체됩니다. `DecompressionStream`을 지원하는 최신 브라우저가 필요합니다.

`node Tools/Check-WebGL.cjs ../../simulations/alveoli-unity`로 로더의 복원 결과·WebAssembly 유효성·재시도·동시 다운로드 수·진행률·오류 화면을 검사합니다.

현재 실행 파일은 같은 저장소의 `alveoli-web-assets` 브랜치에도 게시하며, jsDelivr의 고정 커밋 주소에서 다운로드합니다. CDN을 사용하려면 준비 명령의 마지막 인자로 `https://cdn.jsdelivr.net/gh/ryowoo2726-hue/sci-simulation@실행파일커밋SHA/Build/`를 전달합니다. 새 빌드마다 실행 파일 브랜치를 먼저 갱신하고 해당 커밋 SHA를 사용하세요. 다운로드 실패 시 GitHub Pages의 같은 파일로 재시도합니다.

Windows 빌드는 `BuildAlveoli.Build`를 사용합니다. `Tools/Check-Compilation.ps1`은 설치된 Unity 어셈블리로 C# 컴파일을 검사합니다.

이 모형은 실제 생리 수치를 계산하는 해석기가 아닙니다. 분자 크기와 색, 조직 구조, 이동 속도는 학습을 위해 단순화했습니다.
