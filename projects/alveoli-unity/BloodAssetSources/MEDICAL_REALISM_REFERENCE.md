# 의학·혈액학·생체광학 기반 혈액 3D 사실적 렌더링 심화 레퍼런스 (Medical & Biophysical Realism Reference)

본 문서는 기존 [`BLENDER_3D_BLUEPRINT_SPEC.md`](./BLENDER_3D_BLUEPRINT_SPEC.md)의 정적 기하학 도면을 넘어, **실제 생체 내 미세순환(In-vivo Microcirculation), 주사/투과전자현미경(SEM/TEM) 초미세구조, 혈류역학(Hemodynamics & Rheology), 생체의학광학(Biomedical Optics)** 논문에 근거하여 **실제 의학 영상 수준의 3D 렌더링**을 구현하기 위한 필수 의학 데이터 시트입니다.

---

## 1. 임상 혈액학(Clinical Hematology) 정량 구성비 및 씬(Scene) 개체 수밀도 규격

3D 렌더링에서 가장 흔히 발생하는 비현실적 오류는 **적혈구 대비 백혈구·혈소판의 비율 왜곡**과 **헤마토크릿(Hematocrit, 적혈구 용적률) 부족**입니다. 실제 의학 기준치와 교육적 가시성을 절충한 권장 수밀도는 다음과 같습니다.

### 1.1 정상 성인 혈액 기준치 (Normal Complete Blood Count, CBC)
| 성분 구분 | 임상 정상 범위 (성인 기준) | 혈액 $1\text{ }\mu\text{L}$ ($1\text{ mm}^3$)당 개수 | 부피 비율 (Volume %) | 3D 표준 혈관 씬 ($D=30\text{ }\mu\text{m}, L=80\text{ }\mu\text{m}$, 부피 $\approx 56,550\text{ }\mu\text{m}^3$) 내 실제 개수 | 교육용 쇼케이스 씬 권장 배치 개수 |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **혈장 (Plasma)** | $55\% \quad (50 \sim 60\%)$ | 수분 $91.5\%$, 단백질 $7.0\%$, 기타 용질 $1.5\%$ | **$55.0\%$** | 연속 유체 볼륨 + 포커스 존 나노 분자 파티클 | 포커스 존 내 $2,500 \sim 5,000$개 분자 인스턴스 |
| **적혈구 (RBC)** | 남성: $4.5 \sim 5.9 \times 10^6/\mu\text{L}$<br/>여성: $4.1 \sim 5.1 \times 10^6/\mu\text{L}$ | 약 $5,000,000$개 | **$44.0 \sim 45.0\%$**<br/>(국소 미세혈관 내 **Fahraeus 효과**로 $25 \sim 35\%$) | 실제 미세혈관($H_T \approx 30\%$): **약 $180\text{개}$** | **$120 \sim 180\text{개}$** (전경 클로즈업 15개 + 중경/배경 군집) |
| **혈소판 (Platelets)** | $150,000 \sim 400,000/\mu\text{L}$ | 약 $250,000$개 (적혈구 $20$개당 $1$개) | $< 0.3\%$ | 실제 비율: **약 $9 \sim 14\text{개}$** | **$15 \sim 25\text{개}$** (비활성 원반형 80% + 벽면 활성형 20%) |
| **백혈구 전체 (WBC)** | $4,000 \sim 10,000/\mu\text{L}$ | 약 $7,000$개 (적혈구 $700$개당 $1$개) | $< 0.7\%$ | 실제 비율: **$0.4\text{개}$** ($1\text{개}$ 미만) | **백혈구 5종 각 $1 \sim 2\text{개}$ (총 $5 \sim 8\text{개}$)** — 교육적 관찰을 위해 의도적 밀집 배치 |

> **[의학적 포인트 - 파레우스 효과(Fåhræus Effect)]**: 대혈관의 헤마토크릿($H_D$)은 $45\%$이지만, 내경 $30\text{ }\mu\text{m}$ 이하의 세동맥·모세혈관에서는 적혈구가 중심부 빠른 유속 층으로 몰려 빠르게 통과하므로 **실제 미세혈관 내 순간 적혈구 부피 분율(Tube Hematocrit, $H_T$)은 $25\% \sim 32\%$**가 됩니다. 즉, 3D 혈관 내부 공간의 약 $30\%$를 적혈구로 채우는 것이 의학적으로 가장 정확하며 시야 확보에도 완벽합니다.

### 1.2 백혈구 감별 계산 (WBC Differential Count) 및 핵/세포질 비율 (N:C Ratio)
| 백혈구 아형 | 정상 백분율 (%) | 평균 직경 (부유 상태 / 도말 상태) | 핵 : 세포질 부피비 (N:C Ratio) | 전자현미경(SEM/TEM) 핵심 식별 특징 |
| :--- | :---: | :---: | :---: | :--- |
| **호중구 (Neutrophil)** | **$50 \sim 70\%$** | $8.5 \sim 10.0\text{ }\mu\text{m}$ / $12 \sim 15\text{ }\mu\text{m}$ | $1 : 3$ | $3 \sim 5$분엽 핵이 $0.2\text{ }\mu\text{m}$ 두께의 가느다란 핵사(Chromatin strand)로 연결됨. 1차(아주르친화성) 과립 + 2차(특수) 미세 과립 혼재 |
| **림프구 (Lymphocyte)** | **$20 \sim 40\%$** | 소림프구 $7 \sim 8.5\text{ }\mu\text{m}$ / $9 \sim 12\text{ }\mu\text{m}$ | **$4 : 1 \sim 5 : 1$** (핵이 80~85% 차지) | 짙게 응축된 이질염색질(Heterochromatin) 구형 핵 + 얇은 초승달 모양의 투명한 세포질 테두리. 표면 미세융모($0.35\text{ }\mu\text{m}$) 밀집 ($2 \sim 4\text{개}/\mu\text{m}^2$) |
| **단핵구 (Monocyte)** | **$2 \sim 8\%$** | $12 \sim 15\text{ }\mu\text{m}$ / $15 \sim 20\text{ }\mu\text{m}$ | $1 : 1 \sim 1 : 2$ | 말굽형(Horseshoe) 또는 뇌회전형(Cerebriform) 주름진 거대 핵. 표면에 파도처럼 너울거리는 넓은 판상위족(Lamellipodia / Membrane ruffles, 높이 $0.6\text{ }\mu\text{m}$) |
| **호산구 (Eosinophil)** | **$1 \sim 4\%$** | $9 \sim 11\text{ }\mu\text{m}$ / $12 \sim 15\text{ }\mu\text{m}$ | $1 : 2.5$ | 안경 모양 2분엽 핵. **특수 과립($0.5\text{ }\mu\text{m}$) 내부에 각진 럭비공/다각형 모양의 전자고밀도 결정 코어(Crystalloid Core / Internum, 주염기성 단백질 MBP)**가 반드시 존재함 |
| **호염기구 (Basophil)** | **$0.5 \sim 1\%$** | $8 \sim 10\text{ }\mu\text{m}$ / $10 \sim 14\text{ }\mu\text{m}$ | $1 : 2$ | S자 또는 J자형 2~3분엽 핵이 **직경 $0.5 \sim 1.0\text{ }\mu\text{m}$의 거대 호염기성 과립(헤파린·히스타민 함유)**에 가려져 외곽에서 과립이 포도송이처럼 불룩 튀어나와 보임 |

---

## 2. 혈류역학(Hemodynamics)에 따른 적혈구의 실제 3D 변형 수식 (Biomechanics)

살아있는 혈관 안에서 적혈구는 정지된 양면 오목 원반(Biconcave discocyte) 상태로만 존재하지 않습니다. 혈류 유속과 혈관 반경 위치에 따라 **3가지 대표적 생체 역학적 형태(Morphological States)**가 동시에 렌더링되어야 실제 전자현미경/생체현미경 사진처럼 보입니다.

### 2.1 [State A] 정지/저전단 양면 오목 원반 (Resting Discocyte — Evans & Fung 1972)
- 적용 위치: 전단 속도가 낮은 구역 또는 기본 기준 메쉬.
- 수식:
  $$z(r) = \pm R_0 \sqrt{1 - \left(\frac{r}{R_0}\right)^2} \left[ C_0 + C_2 \left(\frac{r}{R_0}\right)^2 + C_4 \left(\frac{r}{R_0}\right)^4 \right]$$
  - $R_0 = 3.91\text{ }\mu\text{m}$, $C_0 = 0.207161$, $C_2 = 2.002558$, $C_4 = -1.122762$.

### 2.2 [State B] 중심 혈류 고속 이동 시 '낙하산 변형' (Parachute / Umbrella Conformation)
- **의학적 원리**: 세동맥/모세혈관 중심축($r < 4.0\text{ }\mu\text{m}$)에서는 포아즈유 포물선 유속 프로필($v(r) = v_{\max}(1 - r^2/R_{\text{vessel}}^2)$)에 의해 적혈구 중앙부가 혈류 진행 방향($+Y$)으로 밀려나고 외곽 림(Rim)이 뒤쪽($-Y$)으로 젖혀지는 **낙하산(Parachute) 또는 종 모양(Cup/Stomatocyte-like dome)** 변형이 일어납니다 (Skalak 전단 탄성 계수 $\mu_s \approx 4.2 \times 10^{-6}\text{ N/m}$, 굽힘 강성 $k_b \approx 1.8 \times 10^{-19}\text{ J}$).
- **Blender 3D 정점 변형 수식 (원반 정점 $(x_0, y_0, z_0)$에 적용, 혈류 방향 = $+Z_{\text{local}}$)**:
  $$z_{\text{parachute}}(r) = z_0(r) + A_{\text{dome}} \left( 1 - 1.35 \left(\frac{r}{R_0}\right)^2 + 0.35 \left(\frac{r}{R_0}\right)^4 \right)$$
  $$r_{\text{parachute}} = r \cdot \left( 1 - 0.12 \left(\frac{r}{R_0}\right)^2 \right)$$
  - 여기서 $A_{\text{dome}} = 1.65 \sim 2.20\text{ }\mu\text{m}$ (유속에 비례). 외경은 $7.82\text{ }\mu\text{m} \to 6.88\text{ }\mu\text{m}$로 좁아지고 앞쪽이 볼록한 돔, 뒤쪽이 깊게 파인 오목 컵 형태가 됨.

### 2.3 [State C] 편심 혈류 전단 구역의 '슬리퍼/크루아상 변형' (Slipper / Tank-Treading Shape)
- **의학적 원리**: 혈관 중심축에서 벗어난 중간 구역($4.0 < r < 11.0\text{ }\mu\text{m}$)에서는 벽면 쪽 유속이 느리고 중심 쪽 유속이 빠른 **비대칭 전단 응력 구배(Shear Rate Gradient, $\dot{\gamma} = dv/dr$)** 때문에 적혈구가 비대칭으로 찌그러진 **슬리퍼(Slipper) 형태**를 띠며 세포막이 무한궤도처럼 회전(Tank-treading)합니다.
- **Blender 3D 정점 변형 수식**:
  $$x' = x_0 \cdot (1 + 0.22 \sin\theta), \quad y' = y_0 + 0.35 \cdot x_0 \cdot \left(\frac{x_0}{R_0}\right), \quad z' = z_0 + 0.95 \left(\frac{x_0}{R_0}\right) \left(1 - \frac{r^2}{R_0^2}\right)$$

### 2.4 [State D] 저전단 영역의 '적혈구 연전 현상' (Rouleaux Stack)
- **의학적 원리**: 유속이 완만한 부위에서는 혈장 내 **피브리노겐(Fibrinogen)**과 **면역글로불린**이 적혈구 사이의 고갈 인력(Depletion attraction) 및 가교 역할을 하여 적혈구 $3 \sim 6$개가 동전 더미처럼 나란히 포개집니다.
- **배치 규격**: 중심축 간격 $\Delta d = 1.95 \sim 2.15\text{ }\mu\text{m}$, 인접 적혈구 간 미세 경사각 $\Delta\theta = \pm 3.5^\circ$, 측면에 피브리노겐 막대(`3GHG`, $45\text{ nm}$) 분자들을 브릿지 형태로 배치.

---

## 3. 생체의학광학(Biomedical Optics) 기반 혈액의 실제 광학 상수 (Bosschaert & Friebel 데이터)

적혈구와 혈장이 플라스틱 장난감처럼 보이지 않고 **실제 현미경/내시경 촬영처럼 깊이감 있는 반투명 루비빛과 황금빛 산란**을 내려면, 의학물리학에서 실측된 복소굴절률($n + ik$)과 산란 이방성 계수($g$)를 Cycles 렌더러에 정확히 매핑해야 합니다.

### 3.1 혈액 구성 요소별 실제 굴절률 (Refractive Index, $n$, 가시광선 $589\text{ nm}$ 기준)
| 매질 구분 | 실측 굴절률 ($n$) | 물리·생화학적 근거 | Blender 상대 IOR 설정 (혈장 내부 카메라 기준 $n_{\text{rel}} = n_{\text{obj}} / n_{\text{plasma}}$) |
| :--- | :---: | :--- | :---: |
| **순수 물 ($\text{H}_2\text{O}$, $37^\circ\text{C}$)** | `1.331` | 체온($37^\circ\text{C}$)에서의 물 굴절률 | - |
| **인간 혈장 (Blood Plasma)** | **`1.348 ~ 1.351`** | 알부민·글로불린 등 $7\text{ g/dL}$ 단백질 용해로 물보다 상승 | `1.350` (월드 볼륨 기준) |
| **적혈구 세포질 (RBC Cytosol)** | **`1.398 ~ 1.406`** | 평균 적혈구 헤모글로빈 농도($\text{MCHC} \approx 33 \sim 34\text{ g/dL}$, 약 $2.7 \times 10^8$개 Hb 분자/세포)에 의해 고굴절률 형성 ($n_{\text{RBC}} = n_{\text{water}} + \alpha \cdot C_{\text{Hb}}, \alpha \approx 0.00193\text{ dL/g}$) | 절대 `1.402` / 혈장 대비 상대 `1.038` |
| **적혈구 인지질 이중층 막** | **`1.460 ~ 1.480`** | 두께 $7.5\text{ nm}$의 고밀도 콜레스테롤·인지질·막단백질 층 | Thin Film / Coat IOR `1.470` |
| **백혈구 세포질 / 핵** | `1.365` / `1.415` | 세포질은 수분이 많고($n \approx 1.365$), 응축된 염색질 핵은 DNA·히스톤 고밀도로 $n \approx 1.415$ | 외막 `1.368` / 핵 `1.415` |
| **호산구 결정 과립 (MBP Core)** | **`1.455`** | 단백질 결정 코어로 인해 강한 빛 굴절 및 스페큘러 반사 발생 | `1.455` |

### 3.2 동맥혈(Oxy-Hb) vs 정맥혈(Deoxy-Hb) 분광 흡수 및 전방 산란(Anisotropy) 파라미터
- **헤모글로빈 흡수 대역 (Soret Band & Q-Bands)**:
  - **Soret 흡수대 ($415\text{ nm}$ 보라/청색광)**: 흡수계수 $\mu_a > 250\text{ mm}^{-1}$로 극도로 강하게 흡수됨 $\to$ 적혈구 내부를 통과한 투과광(Subsurface/Transmission)에서 **파란색 채널($B$)은 거의 0에 가깝게 소멸**되어야 함.
  - **Q-Band ($\text{HbO}_2$: $541\text{ nm}, 577\text{ nm}$ 녹/황색광 흡수, $620 \sim 700\text{ nm}$ 적색광 투과)**:
    - **동맥혈 적혈구 ($\text{HbO}_2 \ge 97\%$, PDB `1GZX`)**: $600\text{ nm}$ 이상에서 흡수계수가 급감($\mu_a \approx 0.15\text{ mm}^{-1}$)하여 얇은 오목부($0.81\text{ }\mu\text{m}$)를 빛이 통과할 때 **밝은 주홍/루비색 투과광(Translucent Ruby Glow)**이 나타남.
      - Linear RGB Base: `(0.580, 0.014, 0.012)`, SSS Radius: `(1.20, 0.25, 0.08)`
    - **정맥혈 적혈구 ($\text{Deoxy-Hb}$, 산소포화도 $70 \sim 75\%$, PDB `2HHB`)**: $600 \sim 750\text{ nm}$ 적색 영역에서도 흡수계수가 동맥혈보다 약 $4 \sim 8$배 높고($\mu_a \approx 0.8 \sim 1.2\text{ mm}^{-1}$), Soret 밴드가 $430\text{ nm}$로 적색 편이되어 **검붉은 자주색(Dark Burgundy / Maroon)**을 띰.
      - Linear RGB Base: `(0.210, 0.008, 0.028)`, SSS Radius: `(0.55, 0.08, 0.14)`
- **극단적 전방 산란 이방성 (Scattering Anisotropy Factor, $g$)**:
  - 의학광학 실측치(Friebel et al., Bosschaert et al.): 적혈구 현탁액의 이방성 계수는 **$g = 0.982 \sim 0.990$**입니다.
  - **렌더링 필수 적용 사항**: Cycles의 `Subsurface Anisotropy` 및 `Principled Volume Anisotropy`를 기본값(`0.0`)으로 두면 빛이 사방으로 균일 산란되어 우유나 분필처럼 탁해집니다. 반드시 **적혈구 SSS Anisotropy = `0.85 ~ 0.92`**, **혈장 볼륨 Anisotropy = `0.75 ~ 0.85`**로 설정하여 역광(Rim Light / Backlight) 시 적혈구 가장자리와 중앙 오목부가 빛나는 현미경 투과 조명 효과(Transillumination)를 구현해야 합니다.

---

## 4. 혈관벽 및 세포 초미세구조(Ultrastructure) 추가 의학 디테일

### 4.1 혈관 내벽의 핵심: 내피세포 당질층 (Endothelial Glycocalyx Layer, EGL)
- **의학적 구조**: 실제 살아있는 혈관 내벽은 맨들맨들한 세포막이 혈액과 직접 닿지 않습니다. 내피세포 내강 표면 전체에 두께 **$0.2 \sim 0.5\text{ }\mu\text{m}$ ($200 \sim 500\text{ nm}$)**의 **당질층(Glycocalyx)**이 잔디밭이나 산호초 숲처럼 덮여 있습니다.
  - 구성 성분: 막결합 프로테오글리칸(**Syndecan-1, -2, -4**, **Glypican-1**) 줄기에 **헤파란 황산염(Heparan sulfate)**, **콘드로이틴 황산염(Chondroitin sulfate)**, **히알루론산(Hyaluronic acid)**, 그리고 끝단에 음전하를 띤 **시알산(Sialic acid, Neu5Ac — `molecular_data/sdf/sialic_acid_Neu5Ac_glycocalyx_CID445063.sdf`)**이 촘촘히 결합된 구조.
  - 의학적 기능: 적혈구 표면의 글리코포린 A(시알산 음전하)와 정전기적 반발력을 일으켜 **적혈구가 혈관벽에 달라붙지 않고 $0.5 \sim 1.0\text{ }\mu\text{m}$의 무세포 혈장층(Cell-free layer)을 유지하며 미끄러지듯 흐르게 하는 핵심 장벽**입니다.
- **3D 모델링 지침**: 혈관 내벽 표면에 Blender `Hair Curves / Geometry Nodes`로 길이 $0.25 \sim 0.45\text{ }\mu\text{m}$, 두께 $0.015\text{ }\mu\text{m}$의 반투명하게 분지된 미세 섬유 숲을 배치하면 렌더링의 사실감이 비약적으로 상승합니다.

### 4.2 호산구(Eosinophil) 특수 과립의 결정 코어 (Crystalloid Core / Internum)
- **의학적 구조**: 호산구의 특수 과립(직경 $0.5 \sim 0.8\text{ }\mu\text{m}$ 타원체)은 겉껍질(Externum / Matrix: 에오신 친화성 퍼옥시다아제 함유)과 내부 중심의 **각진 육각 기둥/럭비공 모양 단백질 결정(Internum / Crystalloid Core: 주염기성 단백질 Major Basic Protein, MBP 결정 격자)**의 **이중 구조**로 이루어져 있습니다.
- **3D 모델링 지침**: 호산구 과립 구체 내부에 반투명 겉껍질(`Transmission=0.65`)과 그 안에 길이 $0.35\text{ }\mu\text{m}$, 폭 $0.18\text{ }\mu\text{m}$의 각진 팔면체/육각기둥 코어(`Roughness=0.1, IOR=1.455`)를 중첩 배치합니다.

### 4.3 혈소판(Platelet) 변연 미세소관 고리 (Marginal Band of Microtubules) 및 과립 구분
- **변연 미세소관 다발(Marginal Band)**: 비활성 혈소판이 납작한 원반 모양을 유지하는 이유는 적도면 테두리 안쪽을 따라 $\alpha/\beta$-튜불린 미세소관(직경 $25\text{ nm}$)이 **$8 \sim 12$바퀴 코일처럼 감겨 있는 탄성 링**이 존재하기 때문입니다.
- **과립 3종 분리**:
  1. **알파 과립 ($\alpha$-granules, $50 \sim 80$개/혈소판)**: 직경 $0.20 \sim 0.40\text{ }\mu\text{m}$, 내부에 **폰 빌레브란트 인자(vWF, `1AUQ`)**, **피브리노겐(`3GHG`)**, 혈소판 인자 4(PF4) 함유.
  2. **고밀도 과립 (Dense / $\delta$-granules, $3 \sim 8$개/혈소판)**: 직경 $0.15\text{ }\mu\text{m}$, 내부에 **ATP(`CID5957`), ADP, 칼슘 이온($\text{Ca}^{2+}$), 세로토닌**이 고농도로 농축되어 전자현미경에서 과녁(Bull's-eye)처럼 검고 진한 코어로 보임.
  3. **개방소관계 (Open Canalicular System, OCS)**: 세포막이 스펀지처럼 혈소판 내부 깊숙이 함입된 직경 $0.05 \sim 0.08\text{ }\mu\text{m}$의 구불구불한 터널 네트워크.

### 4.4 백혈구 혈관벽 회전 유착 (Leukocyte Rolling & Selectin-PSGL-1 Tethering)
- 백혈구(호중구·단핵구)가 혈관 내벽(Zone C)을 따라 굴러갈 때, 백혈구 미세융모 끝단에 위치한 **PSGL-1 당단백질**과 혈관 내피세포 표면에 발현된 **P-Selectin (`molecular_data/pdb/1G1Q_P_selectin_PSGL1_rolling.pdb`)**이 결합하며, 혈류 항력에 의해 백혈구 뒤쪽으로 길이 $1.5 \sim 3.0\text{ }\mu\text{m}$, 두께 $0.08\text{ }\mu\text{m}$의 가느다란 **세포막 테더(Membrane Tether & Sling)**가 실처럼 늘어나는 모습을 묘사하면 생체 현미경(Intravital Microscopy) 영상과 동일한 역동성을 얻을 수 있습니다.

---

## 5. 확장된 실제 의학 분자 데이터 (`molecular_data/`) 전체 카탈로그 및 생리학적 상호작용

기존 18종(PDB 4종, SDF 14종)에 더해, **의학적 사실성과 중·고등/대학 생리학 기전(산소 해리, 탄산무수화효소 및 염소 이동, 지방산-알부민 결합 복합체, 혈액 응고 폭포, 백혈구 내피 유착, 당질층, 호르몬 수송)**을 완벽히 묘사하기 위해 **신규 PDB 10종 및 SDF 9종(총 37종)**을 확보하였습니다.

### 5.1 단백질 결정 구조 전체 목록 (`molecular_data/pdb/*.pdb` — 총 14종)

| 분류 | 파일명 | PDB ID | 의학·생리학적 역할 및 3D 렌더링 배치 위치 |
| :--- | :--- | :---: | :--- |
| **산소 운반 (동맥혈)** | `1GZX_oxyhemoglobin.pdb` | `1GZX` | **산소 결합 헤모글로빈 (R-state, $\text{HbO}_2$)** — 동맥혈 적혈구 내부 및 단면 클로즈업 ($4\text{개 }\text{O}_2$ 결합) |
| **산소 해리 (정맥혈)** | `2HHB_deoxyhemoglobin.pdb` | `2HHB` | **탈산소 헤모글로빈 (T-state, $\text{Deoxy-Hb}$)** — 조직에 산소를 내어준 후 중심 공동(Central cavity)이 넓어진 구조 |
| **$\text{CO}_2$ 전환 효소** | `1CA2_carbonic_anhydrase_II.pdb` | `1CA2` | **탄산무수화효소 II (Carbonic Anhydrase II)** — 적혈구 내부에서 $\text{CO}_2 + \text{H}_2\text{O} \to \text{HCO}_3^- + \text{H}^+$ 반응을 촉매하는 핵심 효소 ($\text{Zn}^{2+}$ 활성 부위 포함) |
| **적혈구 막 당단백질** | `1AFO_glycophorin_A_membrane.pdb` | `1AFO` | **글리코포린 A (Glycophorin A 막관통 이량체)** — 적혈구 인지질 이중층을 관통하며 외측에 시알산 음전하 코트를 형성 |
| **혈장 삼투압/운반** | `1AO6_albumin.pdb` | `1AO6` | **인간 혈청 알부민 (Apo-HSA, 기본형)** — 혈장 단백질의 55~60%를 차지하는 하트 모양 구상 단백질 |
| **지방산 수송 복합체** | `1E7I_albumin_palmitic_acid_complex.pdb` | `1E7I` | **팔미트산 7분자가 결합된 인간 혈청 알부민 (Holo-HSA)** — 물에 녹지 않는 지방산(소화 산물)이 실제로 알부민 소수성 포켓에 박혀 혈장으로 이동하는 실제 결정 구조 |
| **철 이온 수송체** | `1A8E_transferrin_iron_carrier.pdb` | `1A8E` | **트랜스페린 (Serum Transferrin)** — 수명이 다한 적혈구에서 회수된 철 이온($\text{Fe}^{3+}$)을 골수로 운반하는 혈장 $\beta$-글로불린 |
| **내분비 호르몬** | `4INS_insulin_hormone.pdb` | `4INS` | **인슐린 (Human Insulin)** — 이자 $\beta$세포에서 분비되어 혈장을 통해 표적 세포로 이동하며 포도당(`CID5793`) 흡수를 촉진하는 대표 호르몬 |
| **면역 항체** | `1IGT_immunoglobulin_IgG.pdb` | `1IGT` | **면역글로불린 G (IgG 항체)** — 림프구(형질세포)가 분비하여 혈장 내 병원체를 중화하는 Y자형 $\gamma$-글로불린 |
| **백혈구 벽면 유착** | `1G1Q_P_selectin_PSGL1_rolling.pdb` | `1G1Q` | **P-셀렉틴 + PSGL-1 결합 복합체** — 백혈구가 혈관 내피세포 벽면(Zone C)을 따라 굴러갈 때(Rolling) 미세융모 끝에서 결합하는 접착 단백질 |
| **혈액 응고 전구체** | `3GHG_fibrinogen.pdb` | `3GHG` | **인간 피브리노겐 (Fibrinogen, Factor I)** — 길이 $45\text{ nm}$의 3결절 아령형 혈장 응고 단백질 |
| **혈액 응고 효소** | `1PPB_thrombin_coagulation.pdb` | `1PPB` | **알파-트롬빈 ($\alpha$-Thrombin, Factor IIa)** — 피브리노겐의 중앙 E-도메인을 절단하여 불용성 피브린 그물망으로 중합시키는 핵심 효소 |
| **혈소판 지혈 가교** | `1AUQ_vWF_A1_platelet_adhesion.pdb` | `1AUQ` | **폰 빌레브란트 인자 A1 도메인 (vWF)** — 혈관 손상 시 노출된 콜라겐과 혈소판(GPIb 수용체)을 연결하는 지혈 단백질 |
| **혈관 기저막 섬유** | `1BKV_collagen_triple_helix.pdb` | `1BKV` | **콜라겐 삼중 나선 (Collagen Triple Helix)** — 혈관 내피세포 아래 기저막(Basement Membrane) 및 혈관 외벽을 이루는 구조 단백질 |

### 5.2 혈장·혈구 저분자 물질 전체 목록 (`molecular_data/sdf/*.sdf` — 총 23종)

| 대분류 | 파일명 | 물질명 (화학식) | PubChem CID | 의학·생리학적 의미 및 렌더링 활용 |
| :--- | :--- | :--- | :---: | :--- |
| **용매 (수분)** | `water_H2O_CID962.sdf` | 물 ($\text{H}_2\text{O}$) | `962` | 혈장의 91.5% 용매 (수소결합 클러스터 표현) |
| **호흡 기체** | `oxygen_O2_CID977.sdf` *(신규)* | **산소 기체 ($\text{O}_2$)** | `977` | 폐포에서 적혈구 헤모글로빈(`1GZX`)으로 결합 및 조직으로 확산되는 이원자 분자 |
| **호흡 기체/완충** | `carbon_dioxide_CO2_CID280.sdf` | 이산화탄소 ($\text{CO}_2$) | `280` | 세포 호흡 기체 노폐물 (적혈구 내 `1CA2` 효소 기질) |
| **호흡 기체/완충** | `bicarbonate_HCO3_CID769.sdf` | 탄산수소 이온 ($\text{HCO}_3^-$) | `769` | 혈장 내 $\text{CO}_2$의 주된 이동 형태(70~80%) 및 혈액 $\text{pH } 7.40$ 완충계 |
| **핵심 영양소** | `glucose_C6H12O6_CID5793.sdf` | 포도당 ($\text{C}_6\text{H}_{12}\text{O}_6$) | `5793` | 탄수화물 최종 소화 산물 (정상 공복 혈당 $70 \sim 100\text{ mg/dL}$) |
| **핵심 영양소** | `alanine_amino_acid_CID5950.sdf` | L-알라닌 ($\text{C}_3\text{H}_7\text{NO}_2$) | `5950` | 단백질 소화 산물 대표 아미노산 및 간-근육 포도당-알라닌 회로 물질 |
| **핵심 영양소** | `palmitic_fatty_acid_CID985.sdf` | 팔미트산 ($\text{C}_{16}\text{H}_{32}\text{O}_2$) | `985` | 지방 소화 산물 대표 유리 지방산 (알부민 `1E7I`에 결합해 이동) |
| **핵심 영양소** | `glycerol_CID753.sdf` | 글리세롤 ($\text{C}_3\text{H}_8\text{O}_3$) | `753` | 중성지방 소화 산물 (수용성이므로 혈장에 직접 용해 이동) |
| **핵심 영양소** | `vitaminC_ascorbic_acid_CID54670067.sdf` | 비타민 C (아스코르브산) | `54670067` | 혈장 수용성 항산화 비타민 |
| **에너지/대사체** | `atp_energy_CID5957.sdf` *(신규)* | **ATP ($\text{C}_{10}\text{H}_{16}\text{N}_5\text{O}_{13}\text{P}_3$)** | `5957` | 세포 호흡 에너지 화폐, 혈소판 고밀도 과립 성분 및 저산소 시 적혈구가 방출하는 혈관 확장 신호 물질 |
| **에너지/대사체** | `pyruvic_acid_CID1060.sdf` *(신규)* | **피루브산 ($\text{C}_3\text{H}_4\text{O}_3$)** | `1060` | 해당과정(Glycolysis) 대사 산물 |
| **에너지/대사체** | `lactic_acid_lactate_CID612.sdf` *(신규)* | **젖산 / 락테이트 ($\text{C}_3\text{H}_6\text{O}_3$)** | `612` | 미토콘드리아가 없는 적혈구의 무산소 해당과정 최종 산물 및 근육 운동 대사물 |
| **에너지/대사체** | `citric_acid_CID311.sdf` *(신규)* | **시트르산 ($\text{C}_6\text{H}_8\text{O}_7$)** | `311` | 혈장 내 정상 유기산($\sim 0.1\text{ mM}$) 및 채혈 시 칼슘($\text{Ca}^{2+}$) 결합 항응고제 성분 |
| **세포막/지질** | `cholesterol_CID5997.sdf` | 콜레스테롤 ($\text{C}_{27}\text{H}_{46}\text{O}$) | `5997` | 적혈구 세포막 유동성 조절(막 지질의 약 45 mol%) 및 LDL/HDL 코어 |
| **세포막/지질** | `phosphocholine_headgroup_CID65091.sdf` *(신규)* | **포스포콜린 ($\text{C}_5\text{H}_{15}\text{NO}_4\text{P}^+$)** | `65091` | 세포막 인지질(Phosphatidylcholine) 및 지단백질 외피의 친수성 머리 작용기 |
| **당질층(EGL)** | `sialic_acid_Neu5Ac_glycocalyx_CID445063.sdf` *(신규)* | **시알산 (N-아세틸뉴라민산)** | `445063` | 혈관 내피세포 당질층(Glycocalyx)과 적혈구 표면 음전하(Zeta potential $\approx -15\text{ mV}$)를 만드는 핵심 당 분자 |
| **헴/색소/대사** | `protoporphyrin_IX_heme_ring_CID4971.sdf` *(신규)* | **프로토포르피린 IX (Heme 고리)** | `4971` | 헤모글로빈 내부에서 철 이온($\text{Fe}^{2+}$)을 감싸 혈액의 붉은색을 만드는 거대 고리 구조 |
| **헴/색소/대사** | `bilirubin_pigment_CID5280352.sdf` | 빌리루빈 ($\text{C}_{33}\text{H}_{36}\text{N}_4\text{O}_6$) | `5280352` | 노화 적혈구 헴 분해 산물 (혈장의 맑은 호박색/담황색을 만드는 실제 색소) |
| **호르몬** | `epinephrine_adrenaline_CID5816.sdf` *(신규)* | **에피네프린 (아드레날린)** | `5816` | 부신속질에서 혈액으로 분비되어 심박수·혈류량을 높이는 카테콜아민 호르몬 |
| **질소 노폐물** | `urea_waste_CID1176.sdf` | 요소 ($\text{CO(NH}_2)_2$) | `1176` | 간(오르니틴 회로)에서 합성되어 콩팥으로 배설되는 주된 질소 노폐물 |
| **질소 노폐물** | `ammonia_NH3_CID222.sdf` | 암모니아 ($\text{NH}_3$) | `222` | 아미노산 탈아미노 반응 독성 노폐물 (문맥혈을 통해 간으로 이동) |
| **질소 노폐물** | `creatinine_waste_CID588.sdf` | 크레아티닌 ($\text{C}_4\text{H}_7\text{N}_3\text{O}$) | `588` | 근육 크레아틴 인산 대사 노폐물 (사구체 여과율 GFR 측정 표준 물질) |
| **질소 노폐물** | `uric_acid_waste_CID1175.sdf` | 요산 ($\text{C}_5\text{H}_4\text{N}_4\text{O}_3$) | `1175` | 핵산 퓨린 염기 대사 최종 노폐물 |

---

## 6. 현미경/의학 시네마틱 카메라 & 조명 연출 규격 (Cinematography & Lighting)

실제 내시경·공초점 현미경·BBC/Hybrid Medical Animation 수준의 리얼리즘을 결정짓는 4대 물리 연출 요소입니다:

1. **초박형 피사계 심도 (Microscopic Shallow Depth of Field, DoF)**:
   - 마이크로 스케일($1\text{ BU} = 1\text{ }\mu\text{m}$)에서는 광학 개구수(Numerical Aperture, $\text{NA} \approx 1.2 \sim 1.4$)가 매우 크므로 초점면 두께(Depth of Field)가 **$1.5 \sim 3.5\text{ }\mu\text{m}$**에 불과합니다.
   - Blender 카메라 설정: `Focal Length = 85 mm`, `DOF F-Stop = 0.25 ~ 0.4` (또는 초점 거리 $12\text{ }\mu\text{m}$ 기준 전경 $8\text{ }\mu\text{m}$ 이내와 배경 $25\text{ }\mu\text{m}$ 이후의 적혈구들이 부드러운 보케(Bokeh)로 흐려지도록 설정).
2. **역광 투과 조명 (Subsurface Transillumination / Rim Backlighting)**:
   - 카메라 정면 조명은 평면적인 플라스틱 느낌을 만듭니다. 주 광원(Key Light)을 혈관 뒤쪽 대각선($135^\circ \sim 160^\circ$)에 배치하여 빛이 **혈관벽과 적혈구의 얇은 중앙 오목부($0.81\text{ }\mu\text{m}$)를 투과(Transillumination)**하면서 오렌지-루비빛 SSS 산란이 일어나도록 구성합니다.
3. **박동성 혈류 모션 블러 (Pulsatile Poiseuille Motion Blur)**:
   - 중심부(Zone A)의 낙하산형 적혈구는 유속이 빠르므로($v_{\max} \approx 1.5 \sim 4.0\text{ mm/s}$) 미세한 방향성 모션 블러를 주고, 혈관벽(Zone C)에 붙어 굴러가는 백혈구와 내피세포 당질층은 선명하게 초점을 맞추면 **중심 혈류의 속도감과 벽면 면역 세포의 대비**가 극대화됩니다.
4. **혈장 내 틴들 산란 (Tyndall Effect & Micro-particulate Bokeh)**:
   - 혈장 속 지단백질(LDL/HDL, $20\text{ nm}$), 피브리노겐($45\text{ nm}$), 알부민($8\text{ nm}$) 입자들이 역광을 받아 보석 가루처럼 반짝이는 미세 산란광(Micro-glints)을 형성하도록 포커스 존에 배치합니다.
