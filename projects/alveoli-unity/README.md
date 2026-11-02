# 폐포 기체 교환 Unity 에셋 준비

기존 심장 프로젝트에서 재사용 가능한 파일만 복사했습니다. 공용 파일이므로 심장 프로젝트의 원본은 유지했습니다.

## 사용 방법
- Unity 프로젝트를 만든 뒤 이 폴더의 Assets/LungGasExchange 폴더를 새 프로젝트의 Assets 안에 넣으세요.
- 원본 환경: Unity 6000.3.27f1, Universal RP 17.3.0. 기존 설정을 재사용하려면 같은 환경에서 시작하세요.
- Rendering/HeartURP.asset과 HeartRenderer.asset은 한 쌍입니다. 프로젝트의 Graphics 및 Quality 설정에서 렌더 파이프라인을 지정해야 합니다.
- 기존 프로젝트 전체를 가져오는 경우 같은 GUID의 공용 에셋을 중복으로 넣지 말고 기존 에셋을 사용하세요.

## 포함한 자료
- Shaders/LivingTissue.shader: 젖은 생체 조직 표현, 표면하 산란 근사, 로컬 Z 단면 절개. _Deform=0으로 사용하면 심장 박동 변형 없이 재사용할 수 있습니다.
- Shaders/AnatomyGhost.shader: 반투명 외피 표현.
- Shaders/GuideOverlay.shader: 깊이와 무관하게 보이는 교육용 경로·화살표 표시.
- Rendering: URP 파이프라인과 렌더러 설정 및 원래 .meta 파일.
- Scripts/HeartGeometry.cs: 곡선 보간과 메시 생성 유틸리티 및 .meta 파일.
- Resources/Fonts: NotoSansKR 폰트와 라이선스. 원본에 폰트 .meta 파일이 없어 Unity가 새로 생성합니다. 수록 글리프 범위는 새 화면의 한글 문구로 확인해야 합니다.
- docs: 폐포·동물세포 설계 도안과 전체 모델링 참고 명세.
- ReferenceSource/SplineBloodFlowController.cs.txt: RedCellMesh()의 양면이 오목한 적혈구 생성 코드와 혈류 색상 참고. 심장 코드에 의존하므로 컴파일되지 않는 참고용 텍스트로 보관했습니다.
- ReferenceSource/original-manifest.json, ProjectVersion.txt: 원본 패키지와 Unity 버전 기록. 새 프로젝트 설정을 자동으로 덮어쓰지 않습니다.

## 아직 없는 에셋
기존 프로젝트에는 폐포낭 모델, 폐포를 감싸는 모세혈관 모델, 독립 적혈구 프리팹, O2/CO2 분자 프리팹, 확산 파티클, AlveoliGasExchangeController, 폐포 전용 씬이 없습니다. 이 파일 묶음은 실행 가능한 완성 프로젝트가 아닌 재사용 기반 자료입니다. 단면 제어기도 심장 전용 의존성이 있어 포함하지 않았습니다.

후속 구현에서 산소는 폐포 공기에서 혈액으로, 이산화탄소는 혈액에서 폐포 공기로 순이동하도록 표현합니다. 무작위 분자 운동과 분압 차에 따른 순확산을 구분해 시각화해야 합니다.

## 확인
복사한 파일은 원본과 SHA-256 해시를 대조했습니다. 파일 목록은 FILE_MANIFEST.csv에 있습니다. Unity 에디터에서의 임포트·실행 검증은 수행하지 않았습니다.

## 추가한 산소·이산화 탄소·적혈구 원본 자료
BloodAssetSources 폴더에 혈액 3d 작업 폴더의 다음 원본을 추가 복사했습니다.
- molecular_data/sdf/oxygen_O2_CID977.sdf: 산소 분자 구조 데이터.
- molecular_data/sdf/carbon_dioxide_CO2_CID280.sdf: 이산화 탄소 분자 구조 데이터.
- molecular_data/pdb: 산소 결합/비결합 헤모글로빈 구조 데이터.
- blueprints: 적혈구 형태·변형 및 혈관 단면 도안.
- MEDICAL_REALISM_REFERENCE.md, BLENDER_3D_BLUEPRINT_SPEC.md: 출처와 모델링 명세.

SDF/PDB/SVG는 Unity에서 바로 사용할 수 있는 3D 모델/프리팹이 아닙니다. 분자 데이터는 메시 변환이 필요하고 적혈구는 ReferenceSource의 RedCellMesh() 코드를 독립 메시 또는 프리팹으로 내보내야 합니다. 주변 작업 폴더에서 완성된 산소·이산화 탄소·적혈구 FBX/OBJ/프리팹은 발견하지 못했습니다.
