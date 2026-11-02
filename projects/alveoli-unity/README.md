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

대용량 파일 전송 제한을 피하려면 Node.js로 `node Tools/Prepare-WebGL.cjs ../Alveoli-WebGL`을 실행합니다. 이 도구는 빌드의 데이터·WebAssembly 파일을 gzip 압축 조각으로 바꾸고, 브라우저에서 원본을 복원하는 로더를 적용합니다. 처리한 결과 폴더 전체를 저장소의 `simulations/alveoli-unity/`에 복사하여 배포합니다. 이 로더는 `DecompressionStream`을 지원하는 최신 브라우저가 필요합니다.

Windows 빌드는 `BuildAlveoli.Build`를 사용합니다. `Tools/Check-Compilation.ps1`은 설치된 Unity 어셈블리로 C# 컴파일을 검사합니다.

이 모형은 실제 생리 수치를 계산하는 해석기가 아닙니다. 분자 크기와 색, 조직 구조, 이동 속도는 학습을 위해 단순화했습니다.
