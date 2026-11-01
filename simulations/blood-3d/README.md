# 혈액 3D 관찰과 원심분리

기본 페이지는 혈액·혈관과 심장 WebGL을 함께 볼 수 있는 탐구실입니다. `?view=heart`, `?view=blood`, `?view=compare`로 화면을 선택합니다. 휴대전화는 혈액 화면 하나만 먼저 불러오며, 다른 화면은 선택할 때 내려받습니다. 기존 혈액 시뮬레이션은 `blood-simulation.html`에서도 단독 실행할 수 있습니다. 심장 모델과 재빌드 소스는 `../heart-3d/`에 있습니다.

Unity 6.3 LTS WebGL 빌드입니다. 혈구, 혈관, 분자 및 단백질 46개를 한국어로 관찰하고 원자 구를 선택해 원소 설명을 볼 수 있습니다. 원심분리는 항응고 처리한 혈액의 대표적인 층을 보여 주는 교육용 애니메이션이며 물리적 침강 계산은 아닙니다. 모델은 교육용 근사이고 서로 다른 분자는 관찰을 위해 각각 확대되어 있습니다.

`source/`에는 인터페이스, 설명, 셰이더 및 빌드 도구를 보관했습니다. 완전한 Unity 프로젝트와 원본 모델·PDB/SDF 데이터는 제작자의 로컬 작업 폴더에 있으며 이 저장소에 포함되지 않습니다. 빌드는 gzip 및 Unity의 브라우저 압축 해제 기능을 사용하므로 GitHub Pages에서 별도의 압축 HTTP 헤더가 필요하지 않습니다.

참고: [OpenStax 혈액 구성](https://openstax.org/books/anatomy-and-physiology-2e/pages/18-1-an-overview-of-blood), [MedlinePlus 혈액 구성](https://medlineplus.gov/ency/anatomyvideos/000104.htm), [RCSB PDB](https://www.rcsb.org/), [PubChem](https://pubchem.ncbi.nlm.nih.gov/). 한글 글꼴은 Noto Sans KR이며 라이선스는 `NotoSansKR-LICENSE.txt`에 있습니다.
