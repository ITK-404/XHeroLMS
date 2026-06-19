# XHero Mobile Batch/FPS Report

Generated: 2026-06-19 15:41:58
Mode: after one-click safe batch/FPS pass

## Safety Contract
- Does not edit materials, shaders, prefab assets, GPUI components, HTrace, lighting, bake data, Addressables, generated split scenes, QualitySettings, URP assets, or camera render options.
- URP camera opaque/depth/post settings are snapshotted before optimization and restored after scene save.
- Writes only conservative scene-level FPS flags: gameplay camera occlusion, terrain distance clamps, renderer occlusion allowance, and static batching flags on objects that are already static.

## Applied Changes
- Safe batch/FPS optimizer. It does not edit materials, shaders, prefab assets, GPUI components, HTrace, RenderSettings, lights, lightmaps, bake data, Addressables, scene splitting, QualitySettings, URP assets, or camera render options.
- URP camera opaque/depth/post settings are snapshotted before optimization and restored after scene save.
- Applied changes are limited to gameplay camera occlusion culling, conservative terrain distance clamps, renderer occlusion flags, and static batching flags only on objects already marked static.
- Scene 'Assets/Scenes/Certificates Scene.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/Course Scene Test_Data.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/Course Scene.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/dai_dao_chi_gian_1.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/dai_dao_chi_gian_2.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/Enter_Webview.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/IntroScene.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/LoadingScene.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/New Scene 1.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/New Scene.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/New Scene/testS.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/NewScene/DDCG2.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/phong_ky_mon.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/phong_tuyen_sinh.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/Preview_Certificates.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/test.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/UI_Creator Scene.unity': no safe writable optimization needed.
- Scene 'Assets/Scenes/WebView_Mobile.unity': no safe writable optimization needed.

## Project Settings Snapshot
- Quality level: Mobile (0)
- VSync: 0
- Anti-aliasing: 2
- Streaming mipmaps: True
- LOD bias: 1
- Active render pipeline asset: Assets/Settings/Mobile_RPAsset.asset
- Snapshot only. This optimizer does not write ProjectSettings or URP assets.

## Scene: Assets/Scenes/Certificates Scene.unity
### Summary
- Renderers: total=16, enabled=3, mesh=3, skinned=0
- Material slots: 3; unique materials=3; unique meshes=3
- Static batching flagged mesh renderers: 3/3
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 3 | OK |

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 5,382 | giaykhen/de | de | Add LODGroup or mesh simplification manually. |
| 3,920 | giaykhen/khung | khung | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/Course Scene Test_Data.unity
### Summary
- Renderers: total=35, enabled=4, mesh=4, skinned=0
- Material slots: 8; unique materials=2; unique meshes=1
- Static batching flagged mesh renderers: 0/4
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| URP/BasicStencil | 2 | Verify mobile/URP compatibility. |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 4 | 8 | False | SAch | M_PhuKien, M_MatSach | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant/Book 3D/Container/Sach | Mark only verified non-moving environment objects static, then rerun. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (1)/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (2)/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (3)/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/Course Scene.unity
### Summary
- Renderers: total=35, enabled=4, mesh=4, skinned=0
- Material slots: 8; unique materials=2; unique meshes=1
- Static batching flagged mesh renderers: 0/4
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| URP/BasicStencil | 2 | Verify mobile/URP compatibility. |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 4 | 8 | False | SAch | M_PhuKien, M_MatSach | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant/Book 3D/Container/Sach | Mark only verified non-moving environment objects static, then rerun. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (1)/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (2)/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |
| 4,143 | Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (3)/Book 3D/Container/Sach | SAch | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/dai_dao_chi_gian_1.unity
### Summary
- Renderers: total=720, enabled=512, mesh=512, skinned=0
- Material slots: 611; unique materials=37; unique meshes=376
- Static batching flagged mesh renderers: 512/512
- Terrains: 0; cameras=5

### Cameras
- Animator/Camera: occlusion=True, utility=False, targetTexture=null
- CaptureCam: occlusion=True, utility=False, targetTexture=null
- Learning Group/Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer: occlusion=True, utility=False, targetTexture=null
- Learning Group/Player/YawPivot/PitchPivot/FP_Camera/UICamera: occlusion=True, utility=True, targetTexture=null
- phongthuycohoc1 1 1/Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Lit | 36 | OK |
| Universal Render Pipeline/Particles/Simple Lit | 1 | OK |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 88 | 88 | True | polySurface654 | New Material | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface654 | Should batch through Unity static batching if materials are compatible. |
| 81 | 81 | True | polySurface628 | v1 | phongthuycohoc1 1 1/door_grp/door_01/polySurface628 | Should batch through Unity static batching if materials are compatible. |
| 31 | 62 | True | Plane134 | v1, uv01 | phongthuycohoc1 1 1/phongthuycohoc1/Plane134 | Should batch through Unity static batching if materials are compatible. |
| 41 | 41 | True | pCube70 | uv01 | phongthuycohoc1 1 1/phongthuycohoc1/pCube70 | Should batch through Unity static batching if materials are compatible. |
| 36 | 36 | True | Plane | M_ | Learning Group/Check Point Sitdown/Chair Check Point/Check Point Sprite Temp/Plane | Should batch through Unity static batching if materials are compatible. |
| 16 | 32 | True | pPlane11 | RL18, uv01 | phongthuycohoc1 1 1/phongthuycohoc1/pPlane11 | Should batch through Unity static batching if materials are compatible. |
| 28 | 28 | True | polySurface559 | KHUNGCUWA | phongthuycohoc1 1 1/phongthuycohoc1/polySurface559 | Should batch through Unity static batching if materials are compatible. |
| 14 | 28 | True | pPlane12 | RL17, uv01 | phongthuycohoc1 1 1/phongthuycohoc1/pPlane12 | Should batch through Unity static batching if materials are compatible. |
| 12 | 24 | True | Plane136 | V18, uv01 | phongthuycohoc1 1 1/phongthuycohoc1/Plane136 | Should batch through Unity static batching if materials are compatible. |
| 5 | 20 | True | polySurface643 | uv01, phiasau, RL17, RL18 | phongthuycohoc1 1 1/door_grp/door_04/polySurface643 | Should batch through Unity static batching if materials are compatible. |
| 19 | 19 | True | Plane039 | v13 | phongthuycohoc1 1 1/phongthuycohoc1/Plane039 | Should batch through Unity static batching if materials are compatible. |
| 17 | 17 | True | polySurface35 | phiasau | phongthuycohoc1 1 1/phongthuycohoc1/polySurface35 | Should batch through Unity static batching if materials are compatible. |
| 12 | 12 | True | Plane012 | v15 | phongthuycohoc1 1 1/phongthuycohoc1/Plane012 | Should batch through Unity static batching if materials are compatible. |
| 12 | 12 | True | Plane054 | v7 | phongthuycohoc1 1 1/phongthuycohoc1/Plane054 | Should batch through Unity static batching if materials are compatible. |
| 9 | 9 | True | Plane089 | v11 | phongthuycohoc1 1 1/phongthuycohoc1/Plane089 | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | True | Plane074 | VRayMtl56 | phongthuycohoc1 1 1/phongthuycohoc1/Plane074 | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | True | Plane075 | v8 | phongthuycohoc1 1 1/phongthuycohoc1/Plane075 | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | True | polySurface521 | v9 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface521 | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | True | polySurface522 | v10 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface522 | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | True | polySurface523 | v12 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface523 | Should batch through Unity static batching if materials are compatible. |
| 7 | 7 | True | Plane041 | v14 | phongthuycohoc1 1 1/phongthuycohoc1/Plane041 | Should batch through Unity static batching if materials are compatible. |
| 7 | 7 | True | polySurface160 | uv6 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface160 | Should batch through Unity static batching if materials are compatible. |
| 3 | 6 | True | Plane150 | v1, New Material | phongthuycohoc1 1 1/phongthuycohoc1/Plane150 | Should batch through Unity static batching if materials are compatible. |
| 5 | 5 | True | polySurface553 | RL17 | phongthuycohoc1 1 1/door_grp/door_01/polySurface553 | Should batch through Unity static batching if materials are compatible. |
| 4 | 4 | True | Plane096 | v4 | phongthuycohoc1 1 1/phongthuycohoc1/Plane096 | Should batch through Unity static batching if materials are compatible. |
| 4 | 4 | True | v44 | banhoc | phongthuycohoc1 1 1/phongthuycohoc1/v44 | Should batch through Unity static batching if materials are compatible. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 98,142 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 | polySurface685 | Add LODGroup or mesh simplification manually. |
| 98,142 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 (1) | polySurface685 | Add LODGroup or mesh simplification manually. |
| 98,142 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 (2) | polySurface685 | Add LODGroup or mesh simplification manually. |
| 98,142 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface590 | polySurface590 | Add LODGroup or mesh simplification manually. |
| 75,084 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface140 | polySurface140 | Add LODGroup or mesh simplification manually. |
| 47,788 | phongthuycohoc1 1 1/phongthuycohoc1/cay1 | cay1 | Add LODGroup or mesh simplification manually. |
| 36,464 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface600 | polySurface600 | Add LODGroup or mesh simplification manually. |
| 33,039 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface588 | polySurface588 | Add LODGroup or mesh simplification manually. |
| 33,035 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface589 | polySurface589 | Add LODGroup or mesh simplification manually. |
| 33,032 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 | polySurface683 | Add LODGroup or mesh simplification manually. |
| 33,032 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 (1) | polySurface683 | Add LODGroup or mesh simplification manually. |
| 33,032 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 (2) | polySurface683 | Add LODGroup or mesh simplification manually. |
| 33,032 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 | polySurface684 | Add LODGroup or mesh simplification manually. |
| 33,032 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 (1) | polySurface684 | Add LODGroup or mesh simplification manually. |
| 33,032 | phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 (2) | polySurface684 | Add LODGroup or mesh simplification manually. |
| 10,843 | phongthuycohoc1 1 1/phongthuycohoc1/san_geo | san_geo | Add LODGroup or mesh simplification manually. |
| 9,950 | phongthuycohoc1 1 1/door_grp/door_03/polySurface639 | polySurface639 | Add LODGroup or mesh simplification manually. |
| 9,950 | phongthuycohoc1 1 1/door_grp/door_04/polySurface643 | polySurface643 | Add LODGroup or mesh simplification manually. |
| 9,950 | phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (1) | polySurface643 | Add LODGroup or mesh simplification manually. |
| 9,950 | phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (2) | polySurface643 | Add LODGroup or mesh simplification manually. |
| 9,950 | phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (3) | polySurface643 | Add LODGroup or mesh simplification manually. |
| 9,950 | phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (4) | polySurface643 | Add LODGroup or mesh simplification manually. |
| 9,946 | phongthuycohoc1 1 1/door_grp/door_01/polySurface633 | polySurface633 | Add LODGroup or mesh simplification manually. |
| 9,942 | phongthuycohoc1 1 1/door_grp/door_02/polySurface635 | polySurface635 | Add LODGroup or mesh simplification manually. |
| 9,447 | phongthuycohoc1 1 1/phongthuycohoc1/v44 | v44 | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/dai_dao_chi_gian_2.unity
### Summary
- Renderers: total=173, enabled=112, mesh=97, skinned=0
- Material slots: 188; unique materials=91; unique meshes=68
- Static batching flagged mesh renderers: 86/97
- Terrains: 0; cameras=3

### Cameras
- Animator/Camera: occlusion=True, utility=False, targetTexture=null
- Learning Group (1)/Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer: occlusion=True, utility=False, targetTexture=null
- Learning Group (1)/Player/YawPivot/PitchPivot/FP_Camera/UICamera: occlusion=True, utility=True, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 62 | OK |
| Universal Render Pipeline/Lit | 27 | OK |
| Particles/Standard Unlit | 1 | Verify mobile/URP compatibility. |
| Universal Render Pipeline/Particles/Simple Lit | 1 | OK |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 16 | 16 | False | khung_trong | lambert5 | Dai dao chi gian 2/khungcuaso/khung_trong | Mark only verified non-moving environment objects static, then rerun. |
| 14 | 14 | True | Plane | M_ | Learning Group (1)/Check Point Sitdown/Chair Check Point (6)/Check Point Sprite Temp/Plane | Should batch through Unity static batching if materials are compatible. |
| 4 | 12 | True | BonSai_c3 | M_Leacves_2, M_Chau_4, M_Branches_4 | Dai dao chi gian 2/DDCG2/BonSai/BonSai_c3 | Should batch through Unity static batching if materials are compatible. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 95,445 | Dai dao chi gian 2/DDCG2/SachTre | SachTre | Add LODGroup or mesh simplification manually. |
| 59,745 | Dai dao chi gian 2/DDCG2/DenHocSinh2 | DenHocSinh2 | Add LODGroup or mesh simplification manually. |
| 34,597 | Dai dao chi gian 2/DDCG2/BinhPhong | polySurface68751 | Add LODGroup or mesh simplification manually. |
| 34,597 | Dai dao chi gian 2/DDCG2/BinhPhong | polySurface68751 | Add LODGroup or mesh simplification manually. |
| 22,752 | Dai dao chi gian 2/DDCG2/CayTruc | CayTruc | Add LODGroup or mesh simplification manually. |
| 22,302 | Dai dao chi gian 2/DDCG2/BinhHoaSen | BinhHoaSen | Add LODGroup or mesh simplification manually. |
| 18,638 | Dai dao chi gian 2/DDCG2/BonSai/BonSai | BonSai | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (1) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (8) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (9) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (10) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (11) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (12) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (13) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (14) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (15) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (2) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (3) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (4) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (5) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (6) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (7) | khung_trong | Add LODGroup or mesh simplification manually. |
| 9,379 | Dai dao chi gian 2/DDCG2/ButLong | ButLong | Add LODGroup or mesh simplification manually. |
| 8,754 | Dai dao chi gian 2/DDCG2/ManhTre | ManhTre | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/Enter_Webview.unity
### Summary
- Renderers: total=0, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/IntroScene.unity
### Summary
- Renderers: total=1, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/LoadingScene.unity
### Summary
- Renderers: total=1, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/New Scene 1.unity
### Summary
- Renderers: total=84, enabled=83, mesh=83, skinned=0
- Material slots: 159; unique materials=89; unique meshes=67
- Static batching flagged mesh renderers: 83/83
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 62 | OK |
| Universal Render Pipeline/Lit | 27 | OK |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 15 | 15 | True | khung_trong | lambert5 | Dai dao chi gian 2/khungcuaso/khung_trong | Should batch through Unity static batching if materials are compatible. |
| 4 | 12 | True | BonSai_c3 | M_Leacves_2, M_Chau_4, M_Branches_4 | Dai dao chi gian 2/DDCG2/BonSai/BonSai_c3 | Should batch through Unity static batching if materials are compatible. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 95,445 | Dai dao chi gian 2/DDCG2/SachTre | SachTre | Add LODGroup or mesh simplification manually. |
| 59,745 | Dai dao chi gian 2/DDCG2/DenHocSinh2 | DenHocSinh2 | Add LODGroup or mesh simplification manually. |
| 34,597 | Dai dao chi gian 2/DDCG2/BinhPhong | polySurface68751 | Add LODGroup or mesh simplification manually. |
| 34,597 | Dai dao chi gian 2/DDCG2/BinhPhong | polySurface68751 | Add LODGroup or mesh simplification manually. |
| 22,752 | Dai dao chi gian 2/DDCG2/CayTruc | CayTruc | Add LODGroup or mesh simplification manually. |
| 22,302 | Dai dao chi gian 2/DDCG2/BinhHoaSen | BinhHoaSen | Add LODGroup or mesh simplification manually. |
| 18,638 | Dai dao chi gian 2/DDCG2/BonSai/BonSai | BonSai | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (1) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (8) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (9) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (10) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (11) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (12) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (13) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (14) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (15) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (2) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (3) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (4) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (5) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (6) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2/khungcuaso/khung_trong (7) | khung_trong | Add LODGroup or mesh simplification manually. |
| 9,379 | Dai dao chi gian 2/DDCG2/ButLong | ButLong | Add LODGroup or mesh simplification manually. |
| 8,754 | Dai dao chi gian 2/DDCG2/ManhTre | ManhTre | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/New Scene.unity
### Summary
- Renderers: total=4,948, enabled=2,175, mesh=2,147, skinned=0
- Material slots: 5,134; unique materials=276; unique meshes=184
- Static batching flagged mesh renderers: 2,102/2,147
- Terrains: 4; cameras=4

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null
- Minimap/Topdown Camera: occlusion=True, utility=True, targetTexture=MiniMap
- Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer: occlusion=True, utility=False, targetTexture=null
- Player/YawPivot/PitchPivot/FP_Camera/UICamera: occlusion=True, utility=True, targetTexture=null

### Terrains
- Nui 2/MOUNTAIN 2/group1/nui/Nui10: pixelError=6, basemap=350, detailDistance=45, detailDensity=0.65, treeDistance=0, instanced=False
- Nui 2/Terrain_(-161.10, -11.10, -1041.58): pixelError=16, basemap=260, detailDistance=45, detailDensity=0.65, treeDistance=420, instanced=False
- Nui 2/Terrain_(238.90, -11.10, -441.58): pixelError=16, basemap=260, detailDistance=45, detailDensity=0.65, treeDistance=0, instanced=True
- Nui 2/Terrian_SongSuoi (1): pixelError=16, basemap=260, detailDistance=45, detailDensity=0.65, treeDistance=0, instanced=True

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Lit | 97 | OK |
| Universal Render Pipeline/Nature/SpeedTree8_PBRLit | 72 | OK |
| Universal Render Pipeline/Simple Lit | 44 | OK |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 43 | OK |
| Legacy Shaders/Particles/Alpha Blended | 6 | Verify mobile/URP compatibility. |
| Legacy Shaders/Particles/Additive | 2 | Verify mobile/URP compatibility. |
| Custom/AlwaysOnTop3D_HiddenByUI | 1 | Verify mobile/URP compatibility. |
| Distant Lands/Lumen/Fake Light | 1 | Verify mobile/URP compatibility. |
| Distant Lands/Lumen/Light Ray | 1 | Verify mobile/URP compatibility. |
| Shader Graphs/M_Water_Graph | 1 | OK |
| Shader Graphs/SG_WindowCubemap | 1 | OK |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveTransparent | 1 | OK |
| Universal Render Pipeline/Particles/Unlit | 1 | OK |
| Vefects/SH_Vefects_VFX_URP_Simple_Water_Flat_01 | 1 | Verify mobile/URP compatibility. |
| Vefects/SH_Vefects_VFX_URP_Splash_Mesh_01 | 1 | Verify mobile/URP compatibility. |
| Vefects/SH_Vefects_VFX_URP_Water_Mist_01 | 1 | Verify mobile/URP compatibility. |
| Vefects/SH_Vefects_VFX_URP_Water_Surface_01 | 1 | Verify mobile/URP compatibility. |
| Vefects/SH_Vefects_VFX_URP_Water_Turbulent_Disc_01 | 1 | Verify mobile/URP compatibility. |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 36 | 324 | True | khungNha | VienPhuDieu, GoDoc, door_fake, TuongGach, CuaSoTron, VRayMtl176, M_Go | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/khungNha | Should batch through Unity static batching if materials are compatible. |
| 36 | 324 | True | khungNha | VienPhuDieu, GoDoc, door_fake, TuongGach, CuaSoTron, VRayMtl176, M_Go | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/khungNha | Should batch through Unity static batching if materials are compatible. |
| 36 | 252 | True | Tru_Lancan | LanCan, ChanTru, HoaVanLanCan, GoDoc, TruNha, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/Tru_Lancan | Should batch through Unity static batching if materials are compatible. |
| 36 | 252 | True | Tru_Lancan | LanCan, ChanTru, HoaVanLanCan, GoDoc, TruNha, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/Tru_Lancan | Should batch through Unity static batching if materials are compatible. |
| 117 | 234 | True | buicayV1 1_LOD0 | go, buicayV1 | Enviroment/Tree Decor - Upper map/boncay sau/buicayV1 1/buicayV1 1_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 36 | 180 | True | Ngoi | Ngoi, GoNgang, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/Ngoi | Should batch through Unity static batching if materials are compatible. |
| 36 | 180 | True | Ngoi | Ngoi, GoNgang, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/Ngoi | Should batch through Unity static batching if materials are compatible. |
| 137 | 137 | True | lasen_b_LOD0 | lasen_b | Enviroment/Hoa Sen Group/lasen_b/lasen_b_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 8 | 128 | True | khuevancac | New Material 1, M_Go, New Material, M_GocMai, dinhmai, rongbathang, M_Ngoi, M_Da, lambert2, phongthuycohoc_01_hoavan_hanglan1 | Enviroment/khu_trung_bay_vat_pham/MB_Khue_Van_Cac_LOD Group/MB_Khue_Van_Cac (1) | Should batch through Unity static batching if materials are compatible. |
| 8 | 128 | True | khuevancac | New Material 1, M_Go, New Material, M_GocMai, dinhmai, rongbathang, M_Ngoi, lambert2, phongthuycohoc_01_hoavan_hanglan1 | Enviroment/khu_trung_bay_vat_pham/MB_Khue_Van_Cac_LOD Group/MB_Khue_Van_Cac_Low_Version | Should batch through Unity static batching if materials are compatible. |
| 109 | 109 | True | Quad | SG_WindowCubemap Material | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/Quad | Should batch through Unity static batching if materials are compatible. |
| 36 | 108 | True | CauThang | RongBacThang, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/CauThang | Should batch through Unity static batching if materials are compatible. |
| 36 | 108 | True | CauThang | RongBacThang, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/CauThang | Should batch through Unity static batching if materials are compatible. |
| 4 | 104 | True | polySurface44777 | New Material 2, New Material 1, New Material 3, M_Go4, blengo, New Material 5, phudieu, New Material, M_Da1, M_Tuong1, door_fake, phongthuycohoc_01_batthang1, M_Go5, M_ngoi, M_BacThang3, dinhmai, phongthuycohoc_02_hoavan_hanglan1 | Enviroment/khu_trung_bay_vat_pham/MB_NhaDai_01_LOD Group/MB_NhaDai_01/polySurface44777 | Should batch through Unity static batching if materials are compatible. |
| 102 | 102 | True | lasena_LOD0 | lasena | Enviroment/Hoa Sen Group/lasena (4)/lasena_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 96 | 96 | True | buicokien_LOD0 | buicokien | Enviroment/khu_trung_bay_vat_pham/buicokien/buicokien_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 40 | 80 | True | tree_BB_LOD2 | thancay, tree_BB | Enviroment/Mot Goc Khuon Vien/Tree Group (1)/Tree Group 1 (2)/tree_BB/tree_BB_LOD2 | Should batch through Unity static batching if materials are compatible. |
| 37 | 74 | True | hoasen_LOD0 | than, hoasen | Enviroment/Hoa Sen Group/hoasen/hoasen_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 36 | 72 | True | hoavangoc | HoaVanGocMai, HoaVanDauMai | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/hoavangoc | Should batch through Unity static batching if materials are compatible. |
| 36 | 72 | True | PhuDieu | VienPhuDieu, PhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/PhuDieu | Should batch through Unity static batching if materials are compatible. |
| 36 | 72 | True | VienMai | GoDoc, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712/VienMai | Should batch through Unity static batching if materials are compatible. |
| 36 | 72 | True | hoavangoc | HoaVanGocMai, HoaVanDauMai | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/hoavangoc | Should batch through Unity static batching if materials are compatible. |
| 36 | 72 | True | PhuDieu | VienPhuDieu, PhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/PhuDieu | Should batch through Unity static batching if materials are compatible. |
| 36 | 72 | True | VienMai | GoDoc, VienPhuDieu | Enviroment/khu_trung_bay_vat_pham/Nha_T1/NhaNgan_1712 (1)/VienMai | Should batch through Unity static batching if materials are compatible. |
| 8 | 72 | False | Tru_Den_Da | M_ChanTru, M_HoaVan, M_HopDen_01, M_Ngoi, M_KhungMai_01, M_HopDen_02, M_KhungMai_02, M_HopDen_Light | Enviroment/KMDG (1)/truDen | Mark only verified non-moving environment objects static, then rerun. |
| 22 | 66 | True | Bon_Hoa_5 | BonDa, New Material | Enviroment/Tree Decor - Upper map/boncay sau/Bon_Hoa_5 | Should batch through Unity static batching if materials are compatible. |
| 32 | 64 | True | bupsen_LOD0 | than, bupsen | Enviroment/Hoa Sen Group/bupsen/bupsen_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 30 | 60 | True | nusen_LOD0 | than, nusen | Enviroment/Hoa Sen Group/nusen (1)/nusen_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 24 | 48 | True | Caycaob_2_LOD0 | thancay, Caycaob | Enviroment/khu_trung_bay_vat_pham/Caycaob_2/Caycaob_2_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 40 | 40 | True | tree_BB_LOD3 | tree_BB_Billboard_LOD3 | Enviroment/Mot Goc Khuon Vien/Tree Group (1)/Tree Group 1 (2)/tree_BB/tree_BB_LOD3 | Should batch through Unity static batching if materials are compatible. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 429,330 | Enviroment/ToaChanhDien_3toa/NhaChanhDien_Giua | NhaChanhDien_Giua | Add LODGroup or mesh simplification manually. |
| 284,510 | Enviroment/MB_Nen_Sau (1)/Lancan | Lancan | Add LODGroup or mesh simplification manually. |
| 284,510 | Enviroment/MB_Nen_Sau (1)/Lancan (1) | Lancan | Add LODGroup or mesh simplification manually. |
| 207,338 | Enviroment/ToaChanhDien_3toa/NhaChanhDien_Phai | NhaChanhDien_Phai | Add LODGroup or mesh simplification manually. |
| 207,338 | Enviroment/ToaChanhDien_3toa/NhaChanhDien_Trai | NhaChanhDien_Trai | Add LODGroup or mesh simplification manually. |
| 105,516 | Enviroment/Tuong_Thanh/polySurface66579 | polySurface66579 | Add LODGroup or mesh simplification manually. |
| 73,740 | Enviroment/ChoiNho | ChoiNho | Add LODGroup or mesh simplification manually. |
| 62,526 | Enviroment/Lan Can Group/LanCan | LanCanCau | Add LODGroup or mesh simplification manually. |
| 62,526 | Enviroment/Lan Can Group/LanCan (3) | LanCanCau | Add LODGroup or mesh simplification manually. |
| 62,526 | Enviroment/Lan Can Group/LanCan (1) | LanCanCau | Add LODGroup or mesh simplification manually. |
| 62,526 | Enviroment/Lan Can Group/LanCan (2) | LanCanCau | Add LODGroup or mesh simplification manually. |
| 61,785 | Enviroment/CongTrong | CongTrong | Add LODGroup or mesh simplification manually. |
| 50,733 | Enviroment/MB_Nen_Sau (1)/Lancan2 | Lancan2 | Add LODGroup or mesh simplification manually. |
| 41,840 | Enviroment/Mot Goc Khuon Vien/Tree Group (1)/Tree Group 1 (3)/buicaydai/buicaydai_LOD2 | buicaydai_LOD2 | Add LODGroup or mesh simplification manually. |
| 41,840 | Enviroment/Mot Goc Khuon Vien/Tree Group (1)/Tree Group 1 (4)/buicaydai/buicaydai_LOD2 | buicaydai_LOD2 | Add LODGroup or mesh simplification manually. |
| 41,840 | Enviroment/Mot Goc Khuon Vien (1)/Tree Group (1)/Tree Group 1 (3)/buicaydai/buicaydai_LOD2 | buicaydai_LOD2 | Add LODGroup or mesh simplification manually. |
| 41,840 | Enviroment/Mot Goc Khuon Vien (1)/Tree Group (1)/Tree Group 1 (4)/buicaydai/buicaydai_LOD2 | buicaydai_LOD2 | Add LODGroup or mesh simplification manually. |
| 41,526 | Enviroment/khu_trung_bay_vat_pham/MB_NhaDai_01 (1)/polySurface44777 | polySurface44777 | Add LODGroup or mesh simplification manually. |
| 41,526 | Enviroment/khu_trung_bay_vat_pham (1)/MB_NhaDai_01 (1)/polySurface44777 | polySurface44777 | Add LODGroup or mesh simplification manually. |
| 22,965 | Enviroment/MB_Nen_Sau (1)/polySurface79627 | polySurface79627 | Add LODGroup or mesh simplification manually. |
| 21,943 | Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa | Hoa | Add LODGroup or mesh simplification manually. |
| 21,943 | Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (1) | Hoa | Add LODGroup or mesh simplification manually. |
| 21,943 | Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (2) | Hoa | Add LODGroup or mesh simplification manually. |
| 21,943 | Enviroment/Mot Goc Khuon Vien/NenKhuonVien/Hoa (3) | Hoa | Add LODGroup or mesh simplification manually. |
| 21,943 | Enviroment/Mot Goc Khuon Vien (1)/NenKhuonVien/Hoa | Hoa | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/New Scene/testS.unity
### Summary
- Renderers: total=0, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/NewScene/DDCG2.unity
### Summary
- Renderers: total=81, enabled=81, mesh=81, skinned=0
- Material slots: 151; unique materials=86; unique meshes=66
- Static batching flagged mesh renderers: 81/81
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 59 | OK |
| Universal Render Pipeline/Lit | 27 | OK |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 16 | 16 | True | khung_trong | lambert5 | Dai dao chi gian 2 1/khungcuaso/khung_trong | Should batch through Unity static batching if materials are compatible. |
| 4 | 12 | True | BonSai_c3 | M_Leacves_2, M_Chau_4, M_Branches_4 | Dai dao chi gian 2 1/DDCG2/BonSai/BonSai_c3 | Should batch through Unity static batching if materials are compatible. |
| 3 | 6 | True | CuaSo_Phai | M_CuaSo 1, M_KhungCua | Dai dao chi gian 2 1/DDCG2/CuaSo_Phai | Should batch through Unity static batching if materials are compatible. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 95,445 | Dai dao chi gian 2 1/DDCG2/SachTre | SachTre | Add LODGroup or mesh simplification manually. |
| 59,745 | Dai dao chi gian 2 1/DDCG2/DenHocSinh2 | DenHocSinh2 | Add LODGroup or mesh simplification manually. |
| 34,597 | Dai dao chi gian 2 1/DDCG2/BinhPhong | BinhPhong | Add LODGroup or mesh simplification manually. |
| 22,752 | Dai dao chi gian 2 1/DDCG2/CayTruc | CayTruc | Add LODGroup or mesh simplification manually. |
| 22,302 | Dai dao chi gian 2 1/DDCG2/BinhHoaSen | BinhHoaSen | Add LODGroup or mesh simplification manually. |
| 18,638 | Dai dao chi gian 2 1/DDCG2/BonSai/BonSai | BonSai | Add LODGroup or mesh simplification manually. |
| 15,600 | Dai dao chi gian 2 1/DDCG2/BanHocSinh | BanHocSinh | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (1) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (2) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (3) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (4) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (5) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (6) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (8) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (12) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (13) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (14) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (15) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (9) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (10) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (11) | khung_trong | Add LODGroup or mesh simplification manually. |
| 10,113 | Dai dao chi gian 2 1/khungcuaso/khung_trong (7) | khung_trong | Add LODGroup or mesh simplification manually. |
| 9,379 | Dai dao chi gian 2 1/DDCG2/ButLong | ButLong | Add LODGroup or mesh simplification manually. |
| 8,754 | Dai dao chi gian 2 1/DDCG2/ManhTre | ManhTre | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/phong_ky_mon.unity
### Summary
- Renderers: total=579, enabled=477, mesh=467, skinned=7
- Material slots: 512; unique materials=61; unique meshes=106
- Static batching flagged mesh renderers: 393/467
- Terrains: 0; cameras=3

### Cameras
- Learning Group/Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer: occlusion=True, utility=False, targetTexture=null
- Learning Group/Player/YawPivot/PitchPivot/FP_Camera/UICamera: occlusion=True, utility=True, targetTexture=null
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 15 | OK |
| Universal Render Pipeline/Lit | 15 | OK |
| Universal Render Pipeline/Simple Lit | 12 | OK |
| Shader Graphs/Smoke6way | 8 | OK |
| Shader Graphs/TerrainGrass | 7 | OK |
| Shader Graphs/UberParticles | 1 | OK |
| Universal Render Pipeline/Particles/Simple Lit | 1 | OK |
| Universal Render Pipeline/Particles/Unlit | 1 | OK |
| Vefects/SH_Vefects_VFX_URP_Water_Surface_01 | 1 | Verify mobile/URP compatibility. |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 172 | 172 | False | Grass_B | Grass_A | co/Grass_B | Mark only verified non-moving environment objects static, then rerun. |
| 135 | 135 | True | polySurface67319 | New Material | KTB_Cum_senkymon/la_sung/polySurface67319 | Should batch through Unity static batching if materials are compatible. |
| 14 | 28 | True | hoa_sung | New Material 1, M_la_sen1 | KTB_Cum_senkymon/hoa_sung | Should batch through Unity static batching if materials are compatible. |
| 25 | 25 | False | Fern_A | Fern | co/Fern_A | Mark only verified non-moving environment objects static, then rerun. |
| 17 | 17 | False | Bush_A | Bush | co/Bush_A | Mark only verified non-moving environment objects static, then rerun. |
| 15 | 15 | True | nui01_geo | nui_kymon | nuison/nui01_geo | Should batch through Unity static batching if materials are compatible. |
| 5 | 10 | False | polySurface66877 | hv_tren, hv_tren1 | congkymondongiap/polySurface66877 | Mark only verified non-moving environment objects static, then rerun. |
| 9 | 9 | True | Dem_Ngoi | goi_ngoi | Dem Ngoi Group/DemNgoi | Should batch through Unity static batching if materials are compatible. |
| 9 | 9 | True | Plane | M_ | Learning Group/Check Point Sitdown/Chair Check Point/Check Point Sprite Temp/Plane | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | True | Generated VoluSmoke Mesh | Shader Graphs/Smoke6way | VoluSmokeSlice (9) | Should batch through Unity static batching if materials are compatible. |
| 4 | 8 | True | cum_sen | New Material 1, New Material | KTB_Cum_senkymon/cum_sen | Should batch through Unity static batching if materials are compatible. |
| 7 | 7 | False | polySurface66621 | hv_tren1 | congkymondongiap/polySurface66621 | Mark only verified non-moving environment objects static, then rerun. |
| 5 | 5 | True | Bush_Red | Bush_Red | co/Bush_Red | Should batch through Unity static batching if materials are compatible. |
| 5 | 5 | True | polySurface67151 | hv_tren | congkymondongiap/polySurface67151 | Should batch through Unity static batching if materials are compatible. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 67,299 | nenkymon 1/hanhlang_geo | hanhlang_geo | Add LODGroup or mesh simplification manually. |
| 13,744 | congkymondongiap/vachcong_geo | vachcong_geo | Add LODGroup or mesh simplification manually. |
| 9,234 | congkymondongiap/rong_R_geo | rong_R_geo | Add LODGroup or mesh simplification manually. |
| 9,231 | congkymondongiap/rongL_geo | rongL_geo | Add LODGroup or mesh simplification manually. |
| 8,887 | co/Bush_Red | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | co/Bush_Red (4) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | co/Bush_Red (1) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | co/Bush_Red (2) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | co/Bush_Red (3) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 7,618 | congkymondongiap/khungmai_geo | khungmai_geo | Add LODGroup or mesh simplification manually. |
| 5,725 | den_da | polySurface31 | Add LODGroup or mesh simplification manually. |
| 5,725 | den_da (1) | polySurface31 | Add LODGroup or mesh simplification manually. |
| 4,267 | KTB_Cum_senkymon/cum_sen | cum_sen | Add LODGroup or mesh simplification manually. |
| 4,267 | KTB_Cum_senkymon (1)/cum_sen | cum_sen | Add LODGroup or mesh simplification manually. |
| 4,267 | KTB_Cum_senkymon (2)/cum_sen | cum_sen | Add LODGroup or mesh simplification manually. |
| 4,267 | KTB_Cum_senkymon (3)/cum_sen | cum_sen | Add LODGroup or mesh simplification manually. |
| 3,846 | congkymondongiap/polySurface67306 | polySurface67306 | Add LODGroup or mesh simplification manually. |
| 3,815 | congkymondongiap/polySurface67311 | polySurface67311 | Add LODGroup or mesh simplification manually. |
| 3,464 | congkymondongiap/hoavan_geo | hoavan_geo | Add LODGroup or mesh simplification manually. |
| 3,404 | file_kymon | polySurface5 | Add LODGroup or mesh simplification manually. |
| 3,332 | congkymondongiap/truthanh_geo | truthanh_geo | Add LODGroup or mesh simplification manually. |
| 3,305 | KTB_Cum_senkymon/hoa_sung | hoa_sung | Add LODGroup or mesh simplification manually. |
| 3,305 | KTB_Cum_senkymon/hoa_sung (5) | hoa_sung | Add LODGroup or mesh simplification manually. |
| 3,305 | KTB_Cum_senkymon/hoa_sung (1) | hoa_sung | Add LODGroup or mesh simplification manually. |
| 3,305 | KTB_Cum_senkymon/hoa_sung (6) | hoa_sung | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/phong_tuyen_sinh.unity
### Summary
- Renderers: total=396, enabled=373, mesh=359, skinned=0
- Material slots: 435; unique materials=78; unique meshes=199
- Static batching flagged mesh renderers: 246/359
- Terrains: 0; cameras=3

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null
- Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer: occlusion=True, utility=False, targetTexture=null
- Player/YawPivot/PitchPivot/FP_Camera/UICamera: occlusion=True, utility=True, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 34 | OK |
| Universal Render Pipeline/Lit | 10 | OK |
| Shader Graphs/TerrainGrass | 6 | OK |
| Universal Render Pipeline/Nature/SpeedTree8_PBRLit | 6 | OK |
| Universal Render Pipeline/Simple Lit | 6 | OK |
| Particles/Standard Unlit | 4 | Verify mobile/URP compatibility. |
| Shader Graphs/crystals | 2 | OK |
| Unlit/Transparent | 2 | Verify mobile/URP compatibility. |
| Custom/BlurBoxShader | 1 | Verify mobile/URP compatibility. |
| Distant Lands/Lumen/Fake Light | 1 | Verify mobile/URP compatibility. |
| Distant Lands/Lumen/Light Ray | 1 | Verify mobile/URP compatibility. |
| Shader Graphs/New Shader Graph | 1 | OK |
| Shader Graphs/Rem | 1 | OK |
| Shader Graphs/Rotation UV Shader Graph | 1 | OK |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveTransparent | 1 | OK |
| Universal Render Pipeline/Particles/Unlit | 1 | OK |

### Batch Reduction Candidates
| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |
| ---: | ---: | --- | --- | --- | --- | --- |
| 33 | 33 | False | Grass_B | Grass_A | Map/Grass_B | Mark only verified non-moving environment objects static, then rerun. |
| 29 | 29 | False | Bush_Red | Bush_Red | Map/Bush_Red | Mark only verified non-moving environment objects static, then rerun. |
| 27 | 27 | False | Bush_B | Bush | Map/Bush_B | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 24 | False | polySurface68726 | M_Sach_1, M_Sach_1 2, M_Sach_3 | Map/PTS_Full/quay_ghi_danh/ke_do_2/sach/polySurface68726 | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 24 | False | polySurface68740 | M_Sach_1 2, M_Sach_1, M_Sach_3 | Map/PTS_Full/quay_ghi_danh/ke_do_2/sach/polySurface68740 | Mark only verified non-moving environment objects static, then rerun. |
| 18 | 18 | False | Fern_A | Fern | Map/Fern_A | Mark only verified non-moving environment objects static, then rerun. |
| 16 | 16 | False | polySurface1211.012 | texture_go | Map/PTS_Full/Cong_Ra/polySurface1211.012 | Mark only verified non-moving environment objects static, then rerun. |
| 16 | 16 | False | polySurface42066 | M_Hoa_Tiet | Map/PTS_Full/Cong_Ra/polySurface42066 | Mark only verified non-moving environment objects static, then rerun. |
| 15 | 15 | False | Heather_A | Heather | Map/Heather_A | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 8 | False | hv_dinh.012 | Hvdinh | Map/PTS_Full/Cong_Ra/hv_dinh.012 | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 8 | False | mai_thanh.012 | MaiNgoi | Map/PTS_Full/Cong_Ra/mai_thanh.012 | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 8 | False | polySurface1152.012 | hv_tren | Map/PTS_Full/Cong_Ra/polySurface1152.012 | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 8 | False | polySurface41500.012 | VienVang | Map/PTS_Full/Cong_Ra/polySurface41500.012 | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 8 | True | polySurface41908.012 | GoMai | Map/PTS_Full/Cong_Ra/polySurface41908.012 | Should batch through Unity static batching if materials are compatible. |
| 8 | 8 | False | polySurface42063 | M_Tru_Go | Map/PTS_Full/Cong_Ra/polySurface42063 | Mark only verified non-moving environment objects static, then rerun. |
| 8 | 8 | False | tu_hoa_van.012 | Tuhv | Map/PTS_Full/Cong_Ra/tu_hoa_van.012 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 8 | False | rong1 | Rong, TranChau | Map/PTS_Full/Cong_Ra/rong1 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 8 | True | caytre_LOD0 | textre, caytre | Map/PTS_Full/quay_ghi_danh/ke_do_2/chau_truc/caytre/caytre_LOD0 | Should batch through Unity static batching if materials are compatible. |
| 4 | 8 | False | polySurface68739 | M_Sach_1 2, M_Sach_3 | Map/PTS_Full/quay_ghi_danh/ke_do_2/sach/polySurface68739 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 8 | False | chaucay_bonsai_LOD0 | thancay, chaucay_bonsai | Map/chaucay_bonsai/chaucay_bonsai_LOD0 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 8 | False | caytrucsan10k_LOD0 | go, caytrucsan10k | Map/caytrucsan10k/caytrucsan10k_LOD0 | Mark only verified non-moving environment objects static, then rerun. |
| 6 | 6 | False | polySurface68754 | M_Cong | Map/PTS_Full/Cong_Ra/polySurface68754 | Mark only verified non-moving environment objects static, then rerun. |
| 6 | 6 | True | Geom_Rock_Overgrown_I_LOD02 | New Material 3 | Map/Rock/Geom_Rock_Overgrown_I_LOD02 | Should batch through Unity static batching if materials are compatible. |
| 5 | 5 | False | _LUMENRAY50_Solid Scatter 1 | Lumen Ray | VFX/ray2/Layer | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 4 | True | Cong_Ra_polySurface41970 | HV_Hoavan_tuong_new | Map/PTS_Full/Cong_Ra/Cong_Ra_polySurface41970 | Should batch through Unity static batching if materials are compatible. |
| 4 | 4 | False | pPlane19 | Rotation UV Shader Graph | Map/PTS_Full/Cong_Ra/pPlane19 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 4 | False | polySurface42068 | New Material | Map/PTS_Full/lich_khai_giang/polySurface42068 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 4 | True | ChauTruc | ChauTruc | Map/PTS_Full/quay_ghi_danh/ke_do_2/chau_truc/ChauTruc | Should batch through Unity static batching if materials are compatible. |
| 4 | 4 | False | SachTre9 | M_SachTre | Map/PTS_Full/quay_ghi_danh/ke_do_2/sach/SachTre9 | Mark only verified non-moving environment objects static, then rerun. |
| 4 | 4 | False | polySurface41963 | M_Cong | Map/PTS_Full/Tuong_Trong/polySurface41963 | Mark only verified non-moving environment objects static, then rerun. |

### LOD Candidates
| Vertices | Renderer | Mesh | Note |
| ---: | --- | --- | --- |
| 37,956 | Map/PTS_Full/KeTu_Sach/Tu_281 | ketugo | Add LODGroup or mesh simplification manually. |
| 37,956 | Map/PTS_Full/KeTu_Sach (1)/Tu_281 | ketugo | Add LODGroup or mesh simplification manually. |
| 27,221 | Map/nuid | Nui_7 | Add LODGroup or mesh simplification manually. |
| 20,337 | Map/PTS_Full/Tuong_Trong/rong1 | rong1 | Add LODGroup or mesh simplification manually. |
| 20,337 | Map/PTS_Full/Tuong_Trong2/rong1 | rong1 | Add LODGroup or mesh simplification manually. |
| 20,335 | Map/PTS_Full/Cong_Ra1/rong1 | rong1 | Add LODGroup or mesh simplification manually. |
| 20,323 | Map/PTS_Full/Cong_Ra/rong1 | rong1 | Add LODGroup or mesh simplification manually. |
| 11,437 | Map/PTS_Full/lich_khai_giang/polySurface41908.009 | polySurface41908.009 | Add LODGroup or mesh simplification manually. |
| 11,437 | Map/PTS_Full/quay_ghi_danh/polySurface41908.009 | polySurface41908.009 | Add LODGroup or mesh simplification manually. |
| 11,437 | Map/PTS_Full/quay_gioi_thieu/polySurface41908.009 | polySurface41908.009 | Add LODGroup or mesh simplification manually. |
| 11,437 | Map/PTS_Full/quay_thu_ngan/polySurface41908.009 | polySurface41908.009 | Add LODGroup or mesh simplification manually. |
| 11,422 | Map/PTS_Full/Cong_Ra/polySurface41908.012 | polySurface41908.012 | Add LODGroup or mesh simplification manually. |
| 11,422 | Map/PTS_Full/Cong_Ra1/polySurface41908.012 | polySurface41908.012 | Add LODGroup or mesh simplification manually. |
| 11,422 | Map/PTS_Full/Tuong_Trong/polySurface41908.012 | polySurface41908.012 | Add LODGroup or mesh simplification manually. |
| 11,422 | Map/PTS_Full/Tuong_Trong2/polySurface41908.012 | polySurface41908.012 | Add LODGroup or mesh simplification manually. |
| 9,379 | Map/PTS_Giabut | ButLong | Add LODGroup or mesh simplification manually. |
| 9,379 | Map/PTS_Giabut (1) | ButLong | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (7) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (14) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (17) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (15) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (18) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (16) | Bush_Red | Add LODGroup or mesh simplification manually. |
| 8,887 | Map/Bush_Red (20) | Bush_Red | Add LODGroup or mesh simplification manually. |


## Scene: Assets/Scenes/Preview_Certificates.unity
### Summary
- Renderers: total=20, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/test.unity
### Summary
- Renderers: total=0, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/UI_Creator Scene.unity
### Summary
- Renderers: total=4, enabled=1, mesh=0, skinned=0
- Material slots: 1; unique materials=1; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
| Shader | Material count | Note |
| --- | ---: | --- |
| Universal Render Pipeline/2D/Sprite-Unlit-Default | 1 | OK |

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


## Scene: Assets/Scenes/WebView_Mobile.unity
### Summary
- Renderers: total=0, enabled=0, mesh=0, skinned=0
- Material slots: 0; unique materials=0; unique meshes=0
- Static batching flagged mesh renderers: 0/0
- Terrains: 0; cameras=1

### Cameras
- Main Camera: occlusion=True, utility=False, targetTexture=null

### Terrains
- No terrains found.

### Materials / Shaders
- No materials found on enabled renderers.

### Batch Reduction Candidates
- No repeated mesh/material groups large enough to report.

### LOD Candidates
- No large no-LOD mesh renderers found.


