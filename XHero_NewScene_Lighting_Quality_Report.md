# XHero LMS URP Mobile Scene Optimization Report

Generated: 2026-06-17 11:08:37
Mode: after mobile lighting quality pass
Unity: 6000.2.6f2
Active build target: Android

## Applied Changes
- Mobile lighting quality pass: better main-light shadows, mixed lighting bake prep, HTrace/GPUI remain disabled.
- Mobile quality lighting tuned: 2x MSAA, 28m shadow distance, 2 cascades, high main shadow quality target.
- Scene 'Assets/Scenes/New Scene.unity':
- Directional Light set to Mixed, neutral-warm 5900K, High soft shadows, 0.74 shadow strength.
- RenderSettings balanced: Skybox ambient 0.88, reflection 0.82, neutral cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- Disabled realtime shadows on 25 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.

## URP / Quality Baseline
Quality level: Mobile (0)
Static batching project flag: use Player Settings UI; scene renderer static flags are reported below.
Dynamic batching project flag: controlled in URP asset below.
Quality shadow distance: 28
Quality anti-aliasing: 2
Quality realtime reflection probes: False

### Mobile_RPAsset
- m_UseSRPBatcher: True
- m_SupportsDynamicBatching: False
- m_RenderScale: 0.8
- m_SupportsHDR: False
- m_MSAA: 2x
- m_RequireDepthTexture: True
- m_RequireOpaqueTexture: True
- m_MainLightShadowmapResolution: 2048
- m_ShadowDistance: 28
- m_ShadowCascadeCount: 2
- m_AdditionalLightsPerObjectLimit: 1
- m_AdditionalLightShadowsSupported: False
- m_SoftShadowsSupported: True
- m_SoftShadowQuality: Medium

### Mobile Renderer Features
- DecalRendererFeature (DecalRendererFeature) active=True
- ScreenSpaceAmbientOcclusion_MobileLow (ScreenSpaceAmbientOcclusion) active=False
- HTrace Screen Space Global Illumination (HTraceSSGIRendererFeature) active=False

## HTrace SSGI Status
- Assets/HTraceSSGI exists: True
- Scripts folder exists: True
- Resources folder exists: True
- Renderer feature detected in Mobile_Renderer: True
- Mobile use: feature should be active + volume-driven; XHero tool configures HTrace in the scene Volume at low-cost mobile settings.

## Scene: Assets/Scenes/New Scene.unity
### Scene Summary
- Root objects: 39
- GameObjects: 8415 active=4764 inactive=3651
- Renderers: 4942 enabled+active=2175
- MeshRenderers: 4911 SkinnedMeshRenderers: 0 Terrains: 4
- Meshes: unique=184 approx rendered tris=7,894,161 verts=8,854,376
- Material slots approx draw submissions before batching: 5,134; unique materials=276; instancing-enabled materials=130
- Static batching flagged renderers: 2,105/2,175
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=1, active=1, realtime=0, mixed=1, baked=0
- Directional: 1
  - Directional Light type=Directional mode=Mixed intensity=1.55 shadows=Soft range=10
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=0.88, reflectionIntensity=0.82, skybox=Assets/Asset_Store/AllSkyFree/SkyMap.mat

### Cameras / Volumes
- Camera Main Camera active=False post=False depth=True color=False hdr=False msaa=True
- Camera Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer active=True post=False depth=True color=False hdr=False msaa=True
- Camera Player/YawPivot/PitchPivot/FP_Camera/UICamera active=True post=False depth=False color=False hdr=False msaa=False
- Camera Minimap/Topdown Camera active=True post=False depth=False color=False hdr=False msaa=False
- Volume Global Volume active=True global=True weight=1 profile=Assets/Settings/SampleSceneProfile.asset
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- GPUI Prefab Manager active=False enabled=False prototypes=0 findAtInit=False
- GPUIPrefab components in scene hierarchy: 47
- GPUI Tree Managers: 1; GPUI Terrain components: 3

### GPUI Prefab Prototype Status

| Candidate prefab | Instances | Renderers/instance | Estimated material slots | Estimated verts |
|---|---:|---:|---:|---:|
| Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab | 352 | 2 | 922 | 128,620 |
| Assets/Prefabs/Models/Bon Cay Sanh.prefab | 10 | 8 | 182 | 450,982 |
| Assets/Prefabs/Models/Tree/boncay sau.prefab | 40 | 2 | 150 | 489,600 |
| Assets/Prefabs/Cay Co Bon Hoa.prefab | 10 | 5 | 56 | 206,760 |
| Assets/Models_LMS/Nen/model/HoaSen/stone.prefab | 1 | 30 | 30 | 8,388 |
| Assets/Models_LMS/CongT2/CongT2.prefab | 1 | 3 | 29 | 45,244 |
| Assets/GD_SanhTruoc/Prefab/CongTrong.prefab | 1 | 1 | 18 | 61,785 |
| Assets/GD_SanhTruoc/Prefab/LanCan.prefab | 4 | 1 | 16 | 250,104 |
| Assets/GD_SanhTruoc/Prefab/CayCauLon.prefab | 1 | 5 | 9 | 19,138 |
| Assets/Models_LMS_Mobile/Hieu/GD_SanhTruoc/Prefab/BonHoaNho.prefab | 4 | 1 | 8 | 624 |

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 137 | 137 | lasen_b_LOD0 | lasen_b [Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/lasen_b.st] | Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab | True | Safe candidate only if object is decorative/static. |
| 102 | 102 | lasena_LOD0 | lasena [Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/lasena.st] | Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab | True | Safe candidate only if object is decorative/static. |
| 90 | 90 | Quad | SG_WindowCubemap Material [Assets/HDRI_Captures/SG_WindowCubemap Material.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | False | Safe candidate only if object is decorative/static. |
| 64 | 128 | buicayV1 1_LOD0 | go [Assets/Models_LMS/cay11/cayB/ttrenv/buicayV1 1.st] | Assets/Prefabs/Models/khu_trung_bay_vat_pham.prefab | True | Safe candidate only if object is decorative/static. |
| 40 | 80 | tree_BB_LOD2 | thancay [Assets/Models_LMS_Mobile/Tree/Cay Cong Vien/tree_BB.st] | Assets/GD_SanhTruoc/Prefab/Mot Goc Khuon Vien.prefab | True | Safe candidate only if object is decorative/static. |
| 40 | 40 | tree_BB_LOD3 | tree_BB_Billboard_LOD3 [Assets/Models_LMS_Mobile/Tree/Cay Cong Vien/tree_BB.st] | Assets/GD_SanhTruoc/Prefab/Mot Goc Khuon Vien.prefab | True | Safe candidate only if object is decorative/static. |
| 37 | 74 | hoasen_LOD0 | than [Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/hoasen.st] | Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab | True | Safe candidate only if object is decorative/static. |
| 32 | 64 | bupsen_LOD0 | than [Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/bupsen.st] | Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab | True | Safe candidate only if object is decorative/static. |
| 32 | 32 | buico_LOD0 | buico [Assets/Models_LMS/treecay_cau/co_kien/bui_co/buico.st] | Assets/Prefabs/Models/khu_trung_bay_vat_pham.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 270 | khungNha | VienPhuDieu [Assets/GD_SanhTruoc/Nha4Gian/VienPhuDieu.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | False | Safe candidate only if object is decorative/static. |
| 30 | 270 | khungNha | VienPhuDieu [Assets/GD_SanhTruoc/Nha4Gian/VienPhuDieu.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | False | Safe candidate only if object is decorative/static. |
| 30 | 210 | Tru_Lancan | LanCan [Assets/GD_SanhTruoc/Nha4Gian/LanCan.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 210 | Tru_Lancan | LanCan [Assets/GD_SanhTruoc/Nha4Gian/LanCan.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 150 | Ngoi | Ngoi [Assets/GD_SanhTruoc/Nha4Gian/Ngoi.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 150 | Ngoi | Ngoi [Assets/GD_SanhTruoc/Nha4Gian/Ngoi.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 90 | CauThang | RongBacThang [Assets/GD_SanhTruoc/Nha4Gian/RongBacThang.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 90 | CauThang | RongBacThang [Assets/GD_SanhTruoc/Nha4Gian/RongBacThang.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | nusen_LOD0 | than [Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/nusen.st] | Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | buicayV1 1_LOD0 | go [Assets/Models_LMS/cay11/cayB/ttrenv/buicayV1 1.st] | Assets/Prefabs/Models/Tree/boncay sau.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | hoavangoc | HoaVanGocMai [Assets/GD_SanhTruoc/Nha4Gian/HoaVanGocMai.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | PhuDieu | VienPhuDieu [Assets/GD_SanhTruoc/Nha4Gian/VienPhuDieu.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | VienMai | GoDoc [Assets/GD_SanhTruoc/Nha4Gian/GoDoc.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | hoavangoc | HoaVanGocMai [Assets/GD_SanhTruoc/Nha4Gian/HoaVanGocMai.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | PhuDieu | VienPhuDieu [Assets/GD_SanhTruoc/Nha4Gian/VienPhuDieu.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 60 | VienMai | GoDoc [Assets/GD_SanhTruoc/Nha4Gian/GoDoc.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 30 | BatQuai | VienPhuDieu [Assets/GD_SanhTruoc/Nha4Gian/VienPhuDieu.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 30 | NenGach | NenGach [Assets/GD_SanhTruoc/Nha4Gian/NenGach.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 30 | BangTen | phongthuycohoc_01_banten [Assets/GD_SanhTruoc/Nha4Gian/LOD/NhaNgan_1712.fbx] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | False | Safe candidate only if object is decorative/static. |
| 30 | 30 | BatQuai | VienPhuDieu [Assets/GD_SanhTruoc/Nha4Gian/VienPhuDieu.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |
| 30 | 30 | NenGach | NenGach [Assets/GD_SanhTruoc/Nha4Gian/NenGach.mat] | Assets/Models_LMS_Mobile/Dev/Nha_T1.prefab | True | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Enviroment/ToaChanhDien_3toa/NhaChanhDien_Giua | 429,330 | 36 | 43.99 | Assets/Models_LMS/ToaChanhDien/ToaChanhDien_3toa.fbx |
| Enviroment/MB_Nen_Sau (1)/Lancan | 284,510 | 234,750 | 222.03 | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx |
| Enviroment/MB_Nen_Sau (1)/Lancan (1) | 284,510 | 234,750 | 222.03 | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx |
| Enviroment/ToaChanhDien_3toa/NhaChanhDien_Phai | 207,338 | 5,624 | 35.61 | Assets/Models_LMS/ToaChanhDien/ToaChanhDien_3toa.fbx |
| Enviroment/ToaChanhDien_3toa/NhaChanhDien_Trai | 207,338 | 5,624 | 35.61 | Assets/Models_LMS/ToaChanhDien/ToaChanhDien_3toa.fbx |
| Enviroment/Tuong_Thanh/polySurface66579 | 105,516 | 15,024 | 41.13 | Assets/Models_LMS/Models/CongThanh/Model/Elelemt/Tuong_Thanh.fbx |
| Enviroment/ChoiNho | 73,740 | 342 | 10.49 | Assets/Prefabs/KY MON DON GIAP/ChoiNho.fbx |
| Enviroment/Lan Can Group/LanCan | 62,526 | 63,800 | 75.62 | Assets/GD_SanhTruoc/CayCauLon/LanCan.fbx |
| Enviroment/Lan Can Group/LanCan (3) | 62,526 | 63,800 | 75.62 | Assets/GD_SanhTruoc/CayCauLon/LanCan.fbx |
| Enviroment/Lan Can Group/LanCan (1) | 62,526 | 63,800 | 75.62 | Assets/GD_SanhTruoc/CayCauLon/LanCan.fbx |
| Enviroment/Lan Can Group/LanCan (2) | 62,526 | 63,800 | 75.62 | Assets/GD_SanhTruoc/CayCauLon/LanCan.fbx |
| Enviroment/CongTrong | 61,785 | 14,101 | 34.37 | Assets/GD_SanhTruoc/CongTrong/CongTrong.fbx |
| Enviroment/MB_Nen_Sau (1)/Lancan2 | 50,733 | 42,079 | 155.16 | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx |
| Enviroment/Mot Goc Khuon Vien/Tree Group (1)/Tree Group 1 (3)/buicaydai/buicaydai_LOD2 | 41,840 | 20,920 | 16.95 | Assets/Models_LMS/Tree/buicayvong/buicaydai.st |
| Enviroment/Mot Goc Khuon Vien/Tree Group (1)/Tree Group 1 (4)/buicaydai/buicaydai_LOD2 | 41,840 | 20,920 | 16.95 | Assets/Models_LMS/Tree/buicayvong/buicaydai.st |
| Enviroment/Mot Goc Khuon Vien (1)/Tree Group (1)/Tree Group 1 (3)/buicaydai/buicaydai_LOD2 | 41,840 | 20,920 | 16.95 | Assets/Models_LMS/Tree/buicayvong/buicaydai.st |
| Enviroment/Mot Goc Khuon Vien (1)/Tree Group (1)/Tree Group 1 (4)/buicaydai/buicaydai_LOD2 | 41,840 | 20,920 | 16.95 | Assets/Models_LMS/Tree/buicayvong/buicaydai.st |
| Enviroment/khu_trung_bay_vat_pham/MB_NhaDai_01 (1)/polySurface44777 | 41,526 | 6,857 | 33.97 | Assets/Models_LMS_Mobile/Giang/Models/MB_NhaDai_01.fbx |
| Enviroment/khu_trung_bay_vat_pham (1)/MB_NhaDai_01 (1)/polySurface44777 | 41,526 | 6,857 | 33.97 | Assets/Models_LMS_Mobile/Giang/Models/MB_NhaDai_01.fbx |
| Enviroment/MB_Nen_Sau (1)/polySurface79627 | 22,965 | 160 | 165.94 | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa | 21,943 | 7,476 | 7.25 | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (1) | 21,943 | 7,476 | 7.25 | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (2) | 21,943 | 7,476 | 7.25 | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (3) | 21,943 | 7,476 | 7.25 | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx |
| Enviroment/Mot Goc Khuon Vien (1)/NenKhuonVien/Hoa | 21,943 | 7,476 | 7.25 | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx |

### Collider Simplification Candidates
| Object | Mesh collider verts | Convex | Mesh | Recommendation |
|---|---:|---|---|---|
| Enviroment/ToaChanhDien_3toa/NhaChanhDien_Giua | 429,330 | False | Assets/Models_LMS/ToaChanhDien/ToaChanhDien_3toa.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/MB_Nen_Sau (1)/Lancan | 284,510 | False | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/MB_Nen_Sau (1)/Lancan (1) | 284,510 | False | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/ToaChanhDien_3toa/NhaChanhDien_Phai | 207,338 | False | Assets/Models_LMS/ToaChanhDien/ToaChanhDien_3toa.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/ToaChanhDien_3toa/NhaChanhDien_Trai | 207,338 | False | Assets/Models_LMS/ToaChanhDien/ToaChanhDien_3toa.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/Tuong_Thanh/polySurface66579 | 105,516 | False | Assets/Models_LMS/Models/CongThanh/Model/Elelemt/Tuong_Thanh.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/ChoiNho | 73,740 | False | Assets/Prefabs/KY MON DON GIAP/ChoiNho.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Nui 2/Nui_trc | 62,001 | False | Assets/mountain/mountain-pack.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/CongTrong | 61,785 | False | Assets/GD_SanhTruoc/CongTrong/CongTrong.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/MB_Nen_Sau (1)/Lancan2 | 50,733 | False | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/Mot Goc Khuon Vien/Nha Choi Group/NhaChoi/KhungNha | 39,804 | False | Assets/GD_SanhTruoc/NhaChoi/LOD/NhaChoi.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/Mot Goc Khuon Vien (1)/Nha Choi Group/NhaChoi/KhungNha | 39,804 | False | Assets/GD_SanhTruoc/NhaChoi/LOD/NhaChoi.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham/MB_Khue_Van_Cac_LOD Group/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham/MB_Khue_Van_Cac_LOD Group (1)/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham (1)/MB_Khue_Van_Cac_LOD Group/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham (1)/MB_Khue_Van_Cac_LOD Group (1)/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham (2)/MB_Khue_Van_Cac_LOD Group/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham (2)/MB_Khue_Van_Cac_LOD Group (1)/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham (3)/MB_Khue_Van_Cac_LOD Group/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/khu_trung_bay_vat_pham (3)/MB_Khue_Van_Cac_LOD Group (1)/MB_Khue_Van_Cac (1) | 34,712 | False | Assets/Models_LMS_Mobile/Giang/Models/MB_Khue_Van_Cac.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/CongT2/BonCay_8m/LOD_0/BonCay | 31,144 | False | Assets/Models_LMS/Models/BonCay/BonCay_8m.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/MB_Nen_Sau (1)/polySurface79627 | 22,965 | False | Assets/Models_LMS_Mobile/Giang/NEn/MB_Nen_Sau (1).fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa | 21,943 | False | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (1) | 21,943 | False | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (2) | 21,943 | False | Assets/GD_SanhTruoc/NenKhuonVien/NenKhuonVien.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Lit | 97 | 22 | URP/GPUI friendly |
| Universal Render Pipeline/Nature/SpeedTree8_PBRLit | 72 | 72 | URP/GPUI friendly |
| Universal Render Pipeline/Simple Lit | 44 | 36 | URP/GPUI friendly |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 43 | 0 | URP/GPUI friendly |
| Legacy Shaders/Particles/Alpha Blended | 6 | 0 | Review SRP Batcher compatibility and mobile cost |
| Legacy Shaders/Particles/Additive | 2 | 0 | Review SRP Batcher compatibility and mobile cost |
| Shader Graphs/M_Water_Graph | 1 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveTransparent | 1 | 0 | URP/GPUI friendly |
| Shader Graphs/SG_WindowCubemap | 1 | 0 | URP/GPUI friendly |
| Vefects/SH_Vefects_VFX_URP_Simple_Water_Flat_01 | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Vefects/SH_Vefects_VFX_URP_Splash_Mesh_01 | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Vefects/SH_Vefects_VFX_URP_Water_Turbulent_Disc_01 | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Vefects/SH_Vefects_VFX_URP_Water_Mist_01 | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Vefects/SH_Vefects_VFX_URP_Water_Surface_01 | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Custom/AlwaysOnTop3D_HiddenByUI | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Distant Lands/Lumen/Light Ray | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Distant Lands/Lumen/Fake Light | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Universal Render Pipeline/Particles/Unlit | 1 | 0 | URP/GPUI friendly |

Potential duplicate material names:
- thancay: 9 materials
- than: 8 materials
- New Material: 6 materials
- go: 4 materials
- New Material 1: 3 materials
- New Material 3: 3 materials
- New Material 2: 3 materials
- cayhoa: 3 materials
- New Material 5: 2 materials
- buicayV1: 2 materials
- BupSen: 2 materials
- PhuDieu: 2 materials
- LanCan: 2 materials
- M_Go: 2 materials
- Bark: 2 materials
- Bush_Desktop: 2 materials
- than_go: 2 materials
- cayphiasaucohoc1: 2 materials

