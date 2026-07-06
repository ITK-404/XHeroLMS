# XHero LMS URP Mobile Scene Optimization Report

Generated: 2026-06-18 17:24:06
Mode: after safe one-click mobile repair
Unity: 6000.2.6f2
Active build target: Android

## Applied Changes
- Safe one-click mobile repair/optimization pipeline: disable unsafe auto-GPUI/HTrace, restore stable lighting, keep SRP Batcher/static split safety, terrain/shadow culling, then New Scene split regeneration.
- Mobile quality tuned: 2x MSAA, 35m shadows, 2 cascades, display buffer 32-bit.
- No GPUIPrefab components found in project prefabs.
- Scene 'Assets/Scenes/Certificates Scene.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 4 cool-white no-shadow corner point fill light(s), range=34.0, intensity=0.18. Existing scene fill lights=0.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/Course Scene Test_Data.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 2 cool-white no-shadow corner point fill light(s), range=16.0, intensity=0.24. Existing scene fill lights=0.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/Course Scene.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 2 cool-white no-shadow corner point fill light(s), range=16.0, intensity=0.24. Existing scene fill lights=0.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/dai_dao_chi_gian_1.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 2 cool-white no-shadow corner point fill light(s), range=16.0, intensity=0.24. Existing scene fill lights=4.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 36 decorative material(s).
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/dai_dao_chi_gian_2.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 2 cool-white no-shadow corner point fill light(s), range=16.0, intensity=0.24. Existing scene fill lights=6.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 15 decorative material(s).
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/Enter_Webview.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Renderer bounds empty; skipped reflection probe fit.
- Renderer bounds empty; skipped light probe grid.
- Scene has no real 3D mesh/terrain geometry; skipped auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/IntroScene.unity':
- No Directional Light found; skipped main light quality tuning.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Renderer bounds empty; skipped reflection probe fit.
- Renderer bounds empty; skipped light probe grid.
- Scene has no real 3D mesh/terrain geometry; skipped auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/LoadingScene.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Renderer bounds empty; skipped reflection probe fit.
- Renderer bounds empty; skipped light probe grid.
- Scene has no real 3D mesh/terrain geometry; skipped auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/New Scene 1.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 2 cool-white no-shadow corner point fill light(s), range=16.0, intensity=0.24. Existing scene fill lights=3.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 15 decorative material(s).
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/New Scene.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 4 cool-white no-shadow corner point fill light(s), range=34.0, intensity=0.18. Existing scene fill lights=0.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- Tuned 4 terrain(s): pixel error 6+, detail 45m, tree 420m, billboard 55m, max full LOD trees 24.
- Disabled GPUI Prefab Manager 'GPUI Prefab Manager' and cleared 0 prototype(s).
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 113 decorative material(s).
- Disabled realtime shadows on 25 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/New Scene/testS.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Renderer bounds empty; skipped reflection probe fit.
- Renderer bounds empty; skipped light probe grid.
- Scene has no real 3D mesh/terrain geometry; skipped auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/NewScene/DDCG2.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 2 cool-white no-shadow corner point fill light(s), range=16.0, intensity=0.24. Existing scene fill lights=2.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 15 decorative material(s).
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/phong_ky_mon.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 4 cool-white no-shadow corner point fill light(s), range=34.0, intensity=0.18. Existing scene fill lights=0.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- Disabled GPUI Prefab Manager 'GPUI Prefab Manager - XHero Mobile' and cleared 0 prototype(s).
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 27 decorative material(s).
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/phong_tuyen_sinh.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Added/tuned 4 cool-white no-shadow corner point fill light(s), range=34.0, intensity=0.18. Existing scene fill lights=1.
- Prepared static environment for mobile bake: changed=6, batchingStatic=6, contributeGI=0.
- Disabled GPUI Prefab Manager 'GPUI Prefab Manager - XHero Mobile' and cleared 0 prototype(s).
- No scene GPUIPrefab component found.
- Enabled GPU instancing on 15 decorative material(s).
- Disabled realtime shadows on 6 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/Preview_Certificates.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Scene has no real 3D mesh/terrain geometry; disabled auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/test.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Renderer bounds empty; skipped reflection probe fit.
- Renderer bounds empty; skipped light probe grid.
- Scene has no real 3D mesh/terrain geometry; skipped auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/UI_Creator Scene.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Updated existing XHero mobile reflection probe.
- Existing LightProbeGroup found; kept as-is.
- Scene has no real 3D mesh/terrain geometry; disabled auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Scene 'Assets/Scenes/WebView_Mobile.unity':
- Directional Light set to Mixed clean daylight 6800K, High soft shadows, 0.50 shadow strength.
- RenderSettings balanced for fresh white mobile look: Skybox ambient 1.14, reflection 0.90, soft cool subtractive shadow color.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Disabled HTrace volume component in profile 'Assets/Settings/XHero_Mobile_CinematicLook.asset'.
- Cameras kept HDR/post off; world cameras keep depth and occlusion culling.
- Direct light samples set to 24.
- Assigned XHero_NewScene_MobileLighting.lighting to the active scene.
- Renderer bounds empty; skipped reflection probe fit.
- Renderer bounds empty; skipped light probe grid.
- Scene has no real 3D mesh/terrain geometry; skipped auto fill lights.
- Prepared static environment for mobile bake: changed=0, batchingStatic=0, contributeGI=0.
- No GPUI Prefab Manager found in scene.
- No scene GPUIPrefab component found.
- Disabled realtime shadows on 0 tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable.
- No risky ShaderGraph/custom/legacy material instancing found to disable.
- Regenerated mesh-safe Bundle_NewScene split after optimizer pass so generated scenes inherit current lighting/post/HTrace settings.

## URP / Quality Baseline
Quality level: Mobile (0)
Static batching project flag: use Player Settings UI; scene renderer static flags are reported below.
Dynamic batching project flag: controlled in URP asset below.
Quality shadow distance: 35
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
- m_MainLightShadowmapResolution: 1024
- m_ShadowDistance: 35
- m_ShadowCascadeCount: 2
- m_AdditionalLightsPerObjectLimit: 2
- m_AdditionalLightShadowsSupported: False
- m_SoftShadowsSupported: True
- m_SoftShadowQuality: Low

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

## Scene: Assets/Scenes/Certificates Scene.unity
### Scene Summary
- Root objects: 10
- GameObjects: 99 active=36 inactive=63
- Renderers: 16 enabled+active=3
- MeshRenderers: 16 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=3 approx rendered tris=13,618 verts=9,306
- Material slots approx draw submissions before batching: 3; unique materials=3; instancing-enabled materials=0
- Static batching flagged renderers: 3/3
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=5, active=5, realtime=0, mixed=5, baked=0
- Directional: 1
- Point: 4
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 03 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 04 type=Point mode=Mixed intensity=0.18 shadows=None range=34
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| giaykhen/de | 5,382 | 6,500 | 7.63 | Assets/Models_LMS/giay khen/giaykhen.fbx |
| giaykhen/khung | 3,920 | 7,116 | 6.79 | Assets/Models_LMS/giay khen/giaykhen.fbx |

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 3 | 0 | URP/GPUI friendly |

## Scene: Assets/Scenes/Course Scene Test_Data.unity
### Scene Summary
- Root objects: 14
- GameObjects: 602 active=107 inactive=495
- Renderers: 35 enabled+active=4
- MeshRenderers: 35 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=1 approx rendered tris=22,448 verts=16,572
- Material slots approx draw submissions before batching: 8; unique materials=2; instancing-enabled materials=2
- Static batching flagged renderers: 0/4
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=3, active=3, realtime=0, mixed=3, baked=0
- Directional: 1
- Point: 2
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.24 shadows=None range=16
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.24 shadows=None range=16
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (1)/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (2)/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (3)/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| URP/BasicStencil | 2 | 2 | Review SRP Batcher compatibility and mobile cost |

## Scene: Assets/Scenes/Course Scene.unity
### Scene Summary
- Root objects: 15
- GameObjects: 603 active=108 inactive=495
- Renderers: 35 enabled+active=4
- MeshRenderers: 35 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=1 approx rendered tris=22,448 verts=16,572
- Material slots approx draw submissions before batching: 8; unique materials=2; instancing-enabled materials=2
- Static batching flagged renderers: 0/4
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=3, active=3, realtime=0, mixed=3, baked=0
- Directional: 1
- Point: 2
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.24 shadows=None range=16
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.24 shadows=None range=16
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (1)/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (2)/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |
| Buy Course Canvas mobile/Container/Book Shelf Container/Book Shelf Scroll View mobile/Viewport/Content/Shelf Variant/Book Mobile Variant (3)/Book 3D/Container/Sach | 4,143 | 5,502 | 4.62 | Assets/Resources/Sach/Sach.fbx |

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| URP/BasicStencil | 2 | 2 | Review SRP Batcher compatibility and mobile cost |

## Scene: Assets/Scenes/dai_dao_chi_gian_1.unity
### Scene Summary
- Root objects: 17
- GameObjects: 1699 active=1118 inactive=581
- Renderers: 720 enabled+active=512
- MeshRenderers: 684 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=376 approx rendered tris=1,934,729 verts=1,128,047
- Material slots approx draw submissions before batching: 611; unique materials=37; instancing-enabled materials=32
- Static batching flagged renderers: 512/512
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=7, active=7, realtime=4, mixed=3, baked=0
- Point: 6
- Directional: 1
  - Light group/Point Light (7) type=Point mode=Realtime intensity=21.6 shadows=Soft range=10
  - Light group/Point Light (8) type=Point mode=Realtime intensity=9 shadows=Soft range=10
  - Light group/Point Light (9) type=Point mode=Realtime intensity=9 shadows=Soft range=10
  - Light group/Point Light (10) type=Point mode=Realtime intensity=42.96 shadows=Soft range=10
  - Directional Light type=Directional mode=Mixed intensity=1.48 shadows=Soft range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.24 shadows=None range=16
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.24 shadows=None range=16
- ReflectionProbes: 2
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Assets/Asset_Store/AllSkyFree/SkyMap.mat

### Cameras / Volumes
- Camera phongthuycohoc1 1 1/Main Camera active=False post=False depth=True color=False hdr=False msaa=True
- Camera Learning Group/Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer active=True post=False depth=True color=False hdr=False msaa=True
- Camera Learning Group/Player/YawPivot/PitchPivot/FP_Camera/UICamera active=True post=False depth=False color=False hdr=False msaa=False
- Camera Animator/Camera active=False post=False depth=True color=False hdr=False msaa=True
- Camera CaptureCam active=False post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status

| Candidate prefab | Instances | Renderers/instance | Estimated material slots | Estimated verts |
|---|---:|---:|---:|---:|
| Assets/Prefabs/Models/phongthuycohoc1 1 1.prefab | 1 | 476 | 575 | 1,123,691 |

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 13 | 13 | polySurface196 | v1 [Assets/Models_LMS/texture_phudeu/v1.mat] | Assets/Prefabs/Models/phongthuycohoc1 1 1.prefab | True | Safe candidate only if object is decorative/static. |
| 5 | 5 | polySurface628 | v1 [Assets/Models_LMS/texture_phudeu/v1.mat] | Assets/Prefabs/Models/phongthuycohoc1 1 1.prefab | True | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 | 98,142 | 177,225 | 1.44 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 (1) | 98,142 | 177,225 | 1.44 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 (2) | 98,142 | 177,225 | 1.44 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface590 | 98,142 | 177,225 | 1.46 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface140 | 75,084 | 122,768 | 25.36 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/cay1 | 47,788 | 85,360 | 9.42 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface600 | 36,464 | 46,146 | 26.04 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface588 | 33,039 | 61,479 | 0.87 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface589 | 33,035 | 61,479 | 0.87 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 | 33,032 | 61,479 | 0.86 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 (1) | 33,032 | 61,479 | 0.86 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 (2) | 33,032 | 61,479 | 0.86 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 | 33,032 | 61,479 | 0.86 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 (1) | 33,032 | 61,479 | 0.86 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 (2) | 33,032 | 61,479 | 0.86 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/san_geo | 10,843 | 6,928 | 18.89 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_03/polySurface639 | 9,950 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 | 9,950 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (1) | 9,950 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (2) | 9,950 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (3) | 9,950 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (4) | 9,950 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_01/polySurface633 | 9,946 | 15,960 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/door_grp/door_02/polySurface635 | 9,942 | 976 | 3.12 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |
| phongthuycohoc1 1 1/phongthuycohoc1/v44 | 9,447 | 10,380 | 4.43 | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx |

### Collider Simplification Candidates
| Object | Mesh collider verts | Convex | Mesh | Recommendation |
|---|---:|---|---|---|
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 | 98,142 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 (1) | 98,142 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface685 (2) | 98,142 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface590 | 98,142 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface140 | 75,084 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/cay1 | 47,788 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface600 | 36,464 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface588 | 33,039 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface589 | 33,035 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 | 33,032 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 (1) | 33,032 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface683 (2) | 33,032 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 | 33,032 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 (1) | 33,032 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/polySurface141/polySurface684 (2) | 33,032 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/san_geo | 10,843 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_03/polySurface639 | 9,950 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 | 9,950 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (1) | 9,950 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (2) | 9,950 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (3) | 9,950 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_04/polySurface643 (4) | 9,950 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_01/polySurface633 | 9,946 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/door_grp/door_02/polySurface635 | 9,942 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| phongthuycohoc1 1 1/phongthuycohoc1/v44 | 9,447 | False | Assets/Models_LMS/Co Hoc 1/phongthuycohoc1 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Lit | 36 | 32 | URP/GPUI friendly |
| Universal Render Pipeline/Particles/Simple Lit | 1 | 0 | URP/GPUI friendly |

## Scene: Assets/Scenes/dai_dao_chi_gian_2.unity
### Scene Summary
- Root objects: 18
- GameObjects: 1058 active=500 inactive=558
- Renderers: 173 enabled+active=112
- MeshRenderers: 144 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=68 approx rendered tris=656,788 verts=614,119
- Material slots approx draw submissions before batching: 188; unique materials=91; instancing-enabled materials=9
- Static batching flagged renderers: 86/112
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=12, active=11, realtime=8, mixed=3, baked=1
- Directional: 3
- Point: 8
- Rectangle: 1
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - Directional Light (1) type=Directional mode=Realtime intensity=0.25 shadows=Soft range=10
  - Dai dao chi gian 2/Light Group/Directional Light type=Directional mode=Realtime intensity=15.3 shadows=Soft range=10
  - Dai dao chi gian 2/Light Group/Point Light type=Point mode=Realtime intensity=5.26 shadows=None range=45.79
  - Dai dao chi gian 2/Light Group/Point Light (1) type=Point mode=Realtime intensity=1.34 shadows=None range=52.75
  - Dai dao chi gian 2/Light Group/Point Light (2) type=Point mode=Realtime intensity=16.16 shadows=None range=52.75
  - Point Light type=Point mode=Realtime intensity=4.24 shadows=None range=25.5
  - Point Light (1) type=Point mode=Realtime intensity=4.31 shadows=None range=25.5
  - Point Light (2) type=Point mode=Realtime intensity=0.22 shadows=None range=0.74
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.24 shadows=None range=16
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.24 shadows=None range=16
- ReflectionProbes: 2
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Animator/Camera active=False post=False depth=True color=False hdr=False msaa=True
- Camera Learning Group (1)/Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer active=True post=False depth=True color=False hdr=False msaa=True
- Camera Learning Group (1)/Player/YawPivot/PitchPivot/FP_Camera/UICamera active=True post=False depth=False color=False hdr=False msaa=False
- Volume Global Volume active=True global=True weight=1 profile=Assets/Settings/DefaultVolumeProfile.asset
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 16 | 16 | khung_trong | lambert5 [Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx] | Assets/Dai dao chi gian 2.prefab | False | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Dai dao chi gian 2/DDCG2/SachTre | 95,445 | 103,494 | 23.71 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/DenHocSinh2 | 59,745 | 94,680 | 15.71 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | 6 | 3.23 | Assets/Models_LMS/Models/DDCG2/BinhPhong/BinhPhong.fbx |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | 6 | 3.23 | Assets/Models_LMS/Models/DDCG2/BinhPhong/BinhPhong.fbx |
| Dai dao chi gian 2/DDCG2/CayTruc | 22,752 | 14,725 | 3.46 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/BinhHoaSen | 22,302 | 5,640 | 17.11 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/BonSai/BonSai | 18,638 | 8,996 | 1.02 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (1) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (8) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (9) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (10) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (11) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (12) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (13) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (14) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (15) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (2) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (3) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (4) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (5) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (6) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (7) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/DDCG2/ButLong | 9,379 | 8,186 | 0.62 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/ManhTre | 8,754 | 12,400 | 10.91 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |

### Collider Simplification Candidates
| Object | Mesh collider verts | Convex | Mesh | Recommendation |
|---|---:|---|---|---|
| Dai dao chi gian 2/DDCG2/SachTre | 95,445 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/DenHocSinh2 | 59,745 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | False | Assets/Models_LMS/Models/DDCG2/BinhPhong/BinhPhong.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | False | Assets/Models_LMS/Models/DDCG2/BinhPhong/BinhPhong.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/CayTruc | 22,752 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/BinhHoaSen | 22,302 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/BonSai/BonSai | 18,638 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (1) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (8) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (9) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (10) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (11) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (12) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (13) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (14) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (15) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (2) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (3) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (4) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (5) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (6) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/khungcuaso/khung_trong (7) | 10,113 | False | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Dai dao chi gian 2/DDCG2/ButLong | 9,379 | False | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 62 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Lit | 27 | 9 | URP/GPUI friendly |
| Universal Render Pipeline/Particles/Simple Lit | 1 | 0 | URP/GPUI friendly |
| Particles/Standard Unlit | 1 | 0 | Review SRP Batcher compatibility and mobile cost |

Potential duplicate material names:
- M_Leacves_2: 2 materials
- M_Chau_4: 2 materials

## Scene: Assets/Scenes/Enter_Webview.unity
### Scene Summary
- Root objects: 5
- GameObjects: 7 active=6 inactive=1
- Renderers: 0 enabled+active=0
- MeshRenderers: 0 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=1, active=1, realtime=0, mixed=1, baked=0
- Directional: 1
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
- ReflectionProbes: 0
- LightProbeGroups: 0, probe count=0
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

## Scene: Assets/Scenes/IntroScene.unity
### Scene Summary
- Root objects: 8
- GameObjects: 32 active=20 inactive=12
- Renderers: 1 enabled+active=0
- MeshRenderers: 0 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=0, active=0, realtime=0, mixed=0, baked=0
- ReflectionProbes: 0
- LightProbeGroups: 0, probe count=0
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

## Scene: Assets/Scenes/LoadingScene.unity
### Scene Summary
- Root objects: 6
- GameObjects: 31 active=19 inactive=12
- Renderers: 1 enabled+active=0
- MeshRenderers: 0 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=1, active=0, realtime=0, mixed=1, baked=0
- Directional: 1
- ReflectionProbes: 0
- LightProbeGroups: 0, probe count=0
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

## Scene: Assets/Scenes/New Scene 1.unity
### Scene Summary
- Root objects: 9
- GameObjects: 106 active=104 inactive=2
- Renderers: 84 enabled+active=83
- MeshRenderers: 84 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=67 approx rendered tris=653,988 verts=612,425
- Material slots approx draw submissions before batching: 159; unique materials=89; instancing-enabled materials=9
- Static batching flagged renderers: 83/83
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=8, active=8, realtime=5, mixed=3, baked=0
- Directional: 3
- Point: 5
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - Directional Light (1) type=Directional mode=Realtime intensity=0.3 shadows=Soft range=10
  - Dai dao chi gian 2/Light Group/Directional Light type=Directional mode=Realtime intensity=15.3 shadows=Soft range=10
  - Dai dao chi gian 2/Light Group/Point Light type=Point mode=Realtime intensity=5.26 shadows=None range=45.79
  - Dai dao chi gian 2/Light Group/Point Light (1) type=Point mode=Realtime intensity=1.34 shadows=None range=52.75
  - Dai dao chi gian 2/Light Group/Point Light (2) type=Point mode=Realtime intensity=16.16 shadows=None range=52.75
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.24 shadows=None range=16
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.24 shadows=None range=16
- ReflectionProbes: 2
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 15 | 15 | khung_trong | lambert5 [Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx] | Assets/Dai dao chi gian 2.prefab | False | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Dai dao chi gian 2/DDCG2/SachTre | 95,445 | 103,494 | 23.71 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/DenHocSinh2 | 59,745 | 94,680 | 15.71 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | 6 | 3.23 | Assets/Models_LMS/Models/DDCG2/BinhPhong/BinhPhong.fbx |
| Dai dao chi gian 2/DDCG2/BinhPhong | 34,597 | 6 | 3.23 | Assets/Models_LMS/Models/DDCG2/BinhPhong/BinhPhong.fbx |
| Dai dao chi gian 2/DDCG2/CayTruc | 22,752 | 14,725 | 3.46 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/BinhHoaSen | 22,302 | 5,640 | 17.11 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/BonSai/BonSai | 18,638 | 8,996 | 1.02 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (1) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (8) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (9) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (10) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (11) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (12) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (13) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (14) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (15) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (2) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (3) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (4) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (5) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (6) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/khungcuaso/khung_trong (7) | 10,113 | 8,516 | 2.09 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2/DDCG2/ButLong | 9,379 | 8,186 | 0.62 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2/DDCG2/ManhTre | 8,754 | 12,400 | 10.91 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 62 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Lit | 27 | 9 | URP/GPUI friendly |

Potential duplicate material names:
- M_Leacves_2: 2 materials
- M_Chau_4: 2 materials

## Scene: Assets/Scenes/New Scene.unity
### Scene Summary
- Root objects: 40
- GameObjects: 8420 active=4769 inactive=3651
- Renderers: 4942 enabled+active=2175
- MeshRenderers: 4911 SkinnedMeshRenderers: 0 Terrains: 4
- Meshes: unique=184 approx rendered tris=7,894,161 verts=8,854,376
- Material slots approx draw submissions before batching: 5,134; unique materials=276; instancing-enabled materials=130
- Static batching flagged renderers: 2,102/2,175
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=5, active=5, realtime=0, mixed=5, baked=0
- Directional: 1
- Point: 4
  - Directional Light type=Directional mode=Mixed intensity=1.48 shadows=Soft range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 03 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 04 type=Point mode=Mixed intensity=0.18 shadows=None range=34
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Assets/Asset_Store/AllSkyFree/SkyMap.mat

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
- GPUIPrefab components in scene hierarchy: 0
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

## Scene: Assets/Scenes/New Scene/testS.unity
### Scene Summary
- Root objects: 6
- GameObjects: 19 active=8 inactive=11
- Renderers: 0 enabled+active=0
- MeshRenderers: 0 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=1, active=1, realtime=0, mixed=1, baked=0
- Directional: 1
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
- ReflectionProbes: 0
- LightProbeGroups: 0, probe count=0
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

## Scene: Assets/Scenes/NewScene/DDCG2.unity
### Scene Summary
- Root objects: 7
- GameObjects: 99 active=98 inactive=1
- Renderers: 81 enabled+active=81
- MeshRenderers: 81 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=66 approx rendered tris=627,452 verts=577,828
- Material slots approx draw submissions before batching: 151; unique materials=86; instancing-enabled materials=9
- Static batching flagged renderers: 81/81
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=6, active=6, realtime=3, mixed=3, baked=0
- Directional: 2
- Point: 4
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - Dai dao chi gian 2 1/Light Group/Directional Light type=Directional mode=Realtime intensity=21.57 shadows=Soft range=10
  - Dai dao chi gian 2 1/Light Group/Point Light type=Point mode=Realtime intensity=24.53 shadows=None range=42.98
  - Dai dao chi gian 2 1/Light Group/Point Light (1) type=Point mode=Realtime intensity=22.3 shadows=None range=52.75
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.24 shadows=None range=16
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.24 shadows=None range=16
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 16 | 16 | khung_trong | lambert5 [Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx] | Assets/Dai dao chi gian 2 1.prefab | False | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Dai dao chi gian 2 1/DDCG2/SachTre | 95,445 | 103,494 | 23.71 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/DenHocSinh2 | 59,745 | 94,680 | 15.71 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/BinhPhong | 34,597 | 20,992 | 3.23 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/CayTruc | 22,752 | 14,725 | 3.46 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/BinhHoaSen | 22,302 | 5,640 | 17.11 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/BonSai/BonSai | 18,638 | 8,996 | 1.02 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/BanHocSinh | 15,600 | 22,365 | 17.98 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (1) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (2) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (3) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (4) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (5) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (6) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (8) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (12) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (13) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (14) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (15) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (9) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (10) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (11) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/khungcuaso/khung_trong (7) | 10,113 | 8,516 | 2.12 | Assets/Models_LMS/Models/DDCG2/CuaSo/khungcuaso.fbx |
| Dai dao chi gian 2 1/DDCG2/ButLong | 9,379 | 8,186 | 0.62 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |
| Dai dao chi gian 2 1/DDCG2/ManhTre | 8,754 | 12,400 | 10.91 | Assets/Models_LMS/Models/DDCG2/DDCG2.fbx |

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 59 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Lit | 27 | 9 | URP/GPUI friendly |

Potential duplicate material names:
- M_Leacves_2: 2 materials
- M_Chau_4: 2 materials

## Scene: Assets/Scenes/phong_ky_mon.unity
### Scene Summary
- Root objects: 59
- GameObjects: 1764 active=1144 inactive=620
- Renderers: 579 enabled+active=477
- MeshRenderers: 554 SkinnedMeshRenderers: 7 Terrains: 0
- Meshes: unique=106 approx rendered tris=566,913 verts=560,239
- Material slots approx draw submissions before batching: 512; unique materials=61; instancing-enabled materials=15
- Static batching flagged renderers: 396/477
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=7, active=5, realtime=2, mixed=5, baked=0
- Directional: 1
- Spot: 1
- Point: 5
  - Directional Light type=Directional mode=Mixed intensity=1.48 shadows=Soft range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 03 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 04 type=Point mode=Mixed intensity=0.18 shadows=None range=34
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Assets/Asset_Store/AllSkyFree/SkyMap.mat

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Camera Learning Group/Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer active=True post=False depth=True color=False hdr=False msaa=True
- Camera Learning Group/Player/YawPivot/PitchPivot/FP_Camera/UICamera active=True post=False depth=False color=False hdr=False msaa=False
- Volume Global Volume active=True global=True weight=1 profile=Assets/Settings/SampleSceneProfile.asset
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- GPUI Prefab Manager - XHero Mobile active=False enabled=False prototypes=0 findAtInit=False
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status

| Candidate prefab | Instances | Renderers/instance | Estimated material slots | Estimated verts |
|---|---:|---:|---:|---:|
| Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_B.prefab | 172 | 1 | 172 | 242,176 |

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 172 | 172 | Grass_B | Grass_A [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Grass_A.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_B.prefab | False | Safe candidate only if object is decorative/static. |
| 25 | 25 | Fern_A | Fern [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Fern.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Fern_A.prefab | False | Safe candidate only if object is decorative/static. |
| 17 | 17 | Bush_A | Bush [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Bush.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Bush_A.prefab | False | Safe candidate only if object is decorative/static. |
| 10 | 20 | hoa_sung | New Material 1 [Assets/Ky Mon Don Giap/texture/hoasen_hoasung/New Material 1.mat] |  | False | Safe candidate only if object is decorative/static. |
| 9 | 9 | Dem_Ngoi | goi_ngoi [Assets/Ky Mon Don Giap/texture/ghegoi/goi_ngoi.mat] | Assets/Ky Mon Don Giap/model/DemNgoi.fbx | False | Safe candidate only if object is decorative/static. |
| 6 | 6 | polySurface67340 | New Material [Assets/Ky Mon Don Giap/texture/hoasen_hoasung/New Material.mat] |  | False | Safe candidate only if object is decorative/static. |
| 5 | 5 | Bush_Red | Bush_Red [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Bush_Red.mat] | Assets/TerrainDemoScene_URP/Prefabs/Details/Bush_Red.prefab | False | Safe candidate only if object is decorative/static. |
| 5 | 5 | nui15_geo | nui_kymon [Assets/Ky Mon Don Giap/texture/nui/nui_kymon.mat] |  | False | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| nenkymon 1/hanhlang_geo | 67,299 | 2,280 | 23.18 | Assets/Ky Mon Don Giap/model/nenkymon 1.fbx |
| congkymondongiap/vachcong_geo | 13,744 | 10,560 | 15.21 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| congkymondongiap/rong_R_geo | 9,234 | 13,220 | 3.41 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| congkymondongiap/rongL_geo | 9,231 | 13,220 | 3.41 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| co/Bush_Red | 8,887 | 4,921 | 7.76 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| co/Bush_Red (4) | 8,887 | 4,921 | 6.11 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| co/Bush_Red (1) | 8,887 | 4,921 | 5.03 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| co/Bush_Red (2) | 8,887 | 4,921 | 7.46 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| co/Bush_Red (3) | 8,887 | 4,921 | 5.5 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| congkymondongiap/khungmai_geo | 7,618 | 3,428 | 18.26 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| den_da | 5,725 | 7,338 | 3.34 | Assets/Ky Mon Don Giap/model/den_da.fbx |
| den_da (1) | 5,725 | 7,338 | 3.34 | Assets/Ky Mon Don Giap/model/den_da.fbx |
| KTB_Cum_senkymon/cum_sen | 4,267 | 4,600 | 5.09 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| KTB_Cum_senkymon (1)/cum_sen | 4,267 | 4,600 | 4.79 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| KTB_Cum_senkymon (2)/cum_sen | 4,267 | 4,600 | 3.81 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| KTB_Cum_senkymon (3)/cum_sen | 4,267 | 4,600 | 4.62 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| congkymondongiap/polySurface67306 | 3,846 | 1,962 | 15.32 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| congkymondongiap/polySurface67311 | 3,815 | 1,762 | 18.27 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| congkymondongiap/hoavan_geo | 3,464 | 2,028 | 18.34 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| file_kymon | 3,404 | 4,673 | 157.88 | Assets/Ky Mon Don Giap/model/file_kymon.fbx |
| congkymondongiap/truthanh_geo | 3,332 | 3,248 | 17.89 | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx |
| KTB_Cum_senkymon/hoa_sung | 3,305 | 3,408 | 1.83 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| KTB_Cum_senkymon/hoa_sung (5) | 3,305 | 3,408 | 1.65 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| KTB_Cum_senkymon/hoa_sung (1) | 3,305 | 3,408 | 2.91 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |
| KTB_Cum_senkymon/hoa_sung (6) | 3,305 | 3,408 | 1.83 | Assets/Ky Mon Don Giap/model/KTB_Cum_senkymon.fbx |

### Collider Simplification Candidates
| Object | Mesh collider verts | Convex | Mesh | Recommendation |
|---|---:|---|---|---|
| nenkymon 1/hanhlang_geo | 67,299 | False | Assets/Ky Mon Don Giap/model/nenkymon 1.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/vachcong_geo | 13,744 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (3)/Geom_Rock_Overgrown_C_LOD00 | 9,555 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (4)/Geom_Rock_Overgrown_C_LOD00 | 9,555 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (19)/Geom_Rock_Overgrown_C_LOD00 | 9,555 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (20)/Geom_Rock_Overgrown_C_LOD00 | 9,555 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (29)/Geom_Rock_Overgrown_C_LOD00 | 9,555 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/rong_R_geo | 9,234 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/rongL_geo | 9,231 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| co/Bush_Red | 8,887 | False | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| co/Bush_Red (4) | 8,887 | False | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| co/Bush_Red (1) | 8,887 | False | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| co/Bush_Red (2) | 8,887 | False | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| co/Bush_Red (3) | 8,887 | False | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/khungmai_geo | 7,618 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| den_da | 5,725 | False | Assets/Ky Mon Don Giap/model/den_da.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| den_da (1) | 5,725 | False | Assets/Ky Mon Don Giap/model/den_da.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (3)/Geom_Rock_Overgrown_C_LOD01 | 4,183 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (4)/Geom_Rock_Overgrown_C_LOD01 | 4,183 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (19)/Geom_Rock_Overgrown_C_LOD01 | 4,183 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (20)/Geom_Rock_Overgrown_C_LOD01 | 4,183 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Props/Rock_Overgrown_C (29)/Geom_Rock_Overgrown_C_LOD01 | 4,183 | False | Assets/TerrainDemoScene_URP/Prefabs/Rocks/Models/Rock_Overgrown_C.FBX | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/polySurface67306 | 3,846 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/polySurface67311 | 3,815 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| congkymondongiap/hoavan_geo | 3,464 | False | Assets/Ky Mon Don Giap/model/congkymondongiap.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 15 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Lit | 15 | 3 | URP/GPUI friendly |
| Universal Render Pipeline/Simple Lit | 12 | 12 | URP/GPUI friendly |
| Shader Graphs/Smoke6way | 8 | 0 | URP/GPUI friendly |
| Shader Graphs/TerrainGrass | 7 | 0 | URP/GPUI friendly |
| Shader Graphs/UberParticles | 1 | 0 | URP/GPUI friendly |
| Vefects/SH_Vefects_VFX_URP_Water_Surface_01 | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Universal Render Pipeline/Particles/Unlit | 1 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Particles/Simple Lit | 1 | 0 | URP/GPUI friendly |

Potential duplicate material names:
- Shader Graphs/Smoke6way: 8 materials
- New_Material_8: 2 materials
- M_BupSen4: 2 materials
- hv_tren: 2 materials

## Scene: Assets/Scenes/phong_tuyen_sinh.unity
### Scene Summary
- Root objects: 31
- GameObjects: 2618 active=838 inactive=1780
- Renderers: 396 enabled+active=373
- MeshRenderers: 370 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=199 approx rendered tris=982,788 verts=1,284,498
- Material slots approx draw submissions before batching: 435; unique materials=78; instancing-enabled materials=15
- Static batching flagged renderers: 246/373
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=6, active=6, realtime=1, mixed=5, baked=0
- Directional: 1
- Point: 5
  - Directional Light type=Directional mode=Mixed intensity=1.48 shadows=Soft range=10
  - VFX/Point Light type=Point mode=Realtime intensity=7.04 shadows=None range=10
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 01 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 02 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 03 type=Point mode=Mixed intensity=0.18 shadows=None range=34
  - XHero Mobile Fill Lights/XHero Mobile Corner Fill 04 type=Point mode=Mixed intensity=0.18 shadows=None range=34
- ReflectionProbes: 2
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Assets/Asset_Store/AllSkyFree/SkyMap.mat

### Cameras / Volumes
- Camera Main Camera active=False post=False depth=True color=False hdr=False msaa=True
- Camera Player/YawPivot/PitchPivot/FP_Camera/CameraPlayer active=True post=False depth=True color=False hdr=False msaa=True
- Camera Player/YawPivot/PitchPivot/FP_Camera/UICamera active=True post=False depth=False color=False hdr=False msaa=False
- Volume Global Volume active=True global=True weight=1 profile=Assets/Settings/SampleSceneProfile.asset
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- GPUI Prefab Manager - XHero Mobile active=False enabled=False prototypes=0 findAtInit=False
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status

| Candidate prefab | Instances | Renderers/instance | Estimated material slots | Estimated verts |
|---|---:|---:|---:|---:|
| Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_B.prefab | 33 | 1 | 33 | 46,464 |
| Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_A.prefab | 1 | 1 | 1 | 164 |

### Safe GPU Instancer / GPU Instancing Candidates
| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |
|---:|---:|---|---|---|---|---|
| 33 | 33 | Grass_B | Grass_A [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Grass_A.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Grass_B.prefab | False | Safe candidate only if object is decorative/static. |
| 29 | 29 | Bush_Red | Bush_Red [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Bush_Red.mat] | Assets/TerrainDemoScene_URP/Prefabs/Details/Bush_Red.prefab | False | Safe candidate only if object is decorative/static. |
| 27 | 27 | Bush_B | Bush [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Bush.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Bush_B.prefab | False | Safe candidate only if object is decorative/static. |
| 18 | 18 | Fern_A | Fern [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Fern.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Fern_A.prefab | False | Safe candidate only if object is decorative/static. |
| 15 | 15 | Heather_A | Heather [Assets/TerrainDemoScene_URP/Prefabs/Details/Materials/Heather.asset] | Assets/TerrainDemoScene_URP/Prefabs/Details/Heather_A.prefab | False | Safe candidate only if object is decorative/static. |

### LOD Candidates
| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |
|---|---:|---:|---:|---|
| Map/PTS_Full/KeTu_Sach/Tu_281 | 37,956 | 2,536 | 6.85 | Assets/PhongTuyenSinh/Tu/Tu_281.fbx |
| Map/PTS_Full/KeTu_Sach (1)/Tu_281 | 37,956 | 2,536 | 6.85 | Assets/PhongTuyenSinh/Tu/Tu_281.fbx |
| Map/nuid | 27,221 | 9,986 | 504.01 | Assets/Luat/Model/nuid.fbx |
| Map/PTS_Full/Tuong_Trong/rong1 | 20,337 | 26,440 | 7.04 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Tuong_Trong2/rong1 | 20,337 | 26,440 | 7.04 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Cong_Ra1/rong1 | 20,335 | 26,440 | 7.04 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Cong_Ra/rong1 | 20,323 | 26,440 | 7.04 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/lich_khai_giang/polySurface41908.009 | 11,437 | 13,157 | 15.32 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/quay_ghi_danh/polySurface41908.009 | 11,437 | 13,157 | 15.32 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/quay_gioi_thieu/polySurface41908.009 | 11,437 | 13,157 | 15.32 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/quay_thu_ngan/polySurface41908.009 | 11,437 | 13,157 | 15.32 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Cong_Ra/polySurface41908.012 | 11,422 | 13,157 | 17.12 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Cong_Ra1/polySurface41908.012 | 11,422 | 13,157 | 17.12 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Tuong_Trong/polySurface41908.012 | 11,422 | 13,157 | 16.99 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Full/Tuong_Trong2/polySurface41908.012 | 11,422 | 13,157 | 16.99 | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx |
| Map/PTS_Giabut | 9,379 | 8,186 | 0.88 | Assets/Luat/Model/PTS_Giabut.fbx |
| Map/PTS_Giabut (1) | 9,379 | 8,186 | 1.09 | Assets/Luat/Model/PTS_Giabut.fbx |
| Map/Bush_Red | 8,887 | 4,921 | 4.64 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (7) | 8,887 | 4,921 | 6.09 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (14) | 8,887 | 4,921 | 6.8 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (17) | 8,887 | 4,921 | 6.8 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (15) | 8,887 | 4,921 | 6.69 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (18) | 8,887 | 4,921 | 6.69 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (16) | 8,887 | 4,921 | 6.69 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |
| Map/Bush_Red (20) | 8,887 | 4,921 | 7 | Assets/TerrainDemoScene_URP/Prefabs/Details/Models/Bush_Red.asset |

### Collider Simplification Candidates
| Object | Mesh collider verts | Convex | Mesh | Recommendation |
|---|---:|---|---|---|
| Map/chaucay_bonsai/chaucay_bonsai_LOD0 | 93,189 | False | Assets/Luat/tree/chaucay_bonsai.st | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/chaucay_bonsai (2)/chaucay_bonsai_LOD0 | 93,189 | False | Assets/Luat/tree/chaucay_bonsai.st | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/chaucay_bonsai (3)/chaucay_bonsai_LOD0 | 93,189 | False | Assets/Luat/tree/chaucay_bonsai.st | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/chaucay_bonsai (1)/chaucay_bonsai_LOD0 | 93,189 | False | Assets/Luat/tree/chaucay_bonsai.st | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/KeTu_Sach/Tu_281 | 37,956 | False | Assets/PhongTuyenSinh/Tu/Tu_281.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/KeTu_Sach (1)/Tu_281 | 37,956 | False | Assets/PhongTuyenSinh/Tu/Tu_281.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/nuid | 27,221 | False | Assets/Luat/Model/nuid.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_ghi_danh/ke_do_2/chau_truc/CayTruc | 22,752 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_ghi_danh/ke_do_2/chau_truc1/CayTruc | 22,752 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_thu_ngan/ke_do_1/chau_truc/CayTruc | 22,752 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_thu_ngan/ke_do_1/chau_truc1/CayTruc | 22,752 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Tuong_Trong/rong1 | 20,337 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Tuong_Trong2/rong1 | 20,337 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Cong_Ra1/rong1 | 20,335 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Cong_Ra/rong1 | 20,323 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/lich_khai_giang/polySurface41908.009 | 11,437 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_ghi_danh/polySurface41908.009 | 11,437 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_gioi_thieu/polySurface41908.009 | 11,437 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/quay_thu_ngan/polySurface41908.009 | 11,437 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Cong_Ra/polySurface41908.012 | 11,422 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Cong_Ra1/polySurface41908.012 | 11,422 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Tuong_Trong/polySurface41908.012 | 11,422 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/PTS_Full/Tuong_Trong2/polySurface41908.012 | 11,422 | False | Assets/Giang_ne/PHONG_TUYEN_SINH/PTS_Full.fbx | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/caytrucsan10k/caytrucsan10k_LOD0 | 10,995 | False | Assets/Luat/tree/caytrusan/caytrucsan10k.st | Replace with Box/Capsule/compound collider if player collision can be approximate. |
| Map/caytrucsan10k (2)/caytrucsan10k_LOD0 | 10,995 | False | Assets/Luat/tree/caytrusan/caytrucsan10k.st | Replace with Box/Capsule/compound collider if player collision can be approximate. |

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive | 34 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Lit | 10 | 4 | URP/GPUI friendly |
| Universal Render Pipeline/Simple Lit | 6 | 2 | URP/GPUI friendly |
| Universal Render Pipeline/Nature/SpeedTree8_PBRLit | 6 | 6 | URP/GPUI friendly |
| Shader Graphs/TerrainGrass | 6 | 0 | URP/GPUI friendly |
| Particles/Standard Unlit | 4 | 0 | Review SRP Batcher compatibility and mobile cost |
| Unlit/Transparent | 2 | 2 | Review SRP Batcher compatibility and mobile cost |
| Shader Graphs/crystals | 2 | 0 | URP/GPUI friendly |
| Shader Graphs/Rotation UV Shader Graph | 1 | 0 | URP/GPUI friendly |
| Shader Graphs/New Shader Graph | 1 | 0 | URP/GPUI friendly |
| Shader Graphs/Rem | 1 | 0 | URP/GPUI friendly |
| Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveTransparent | 1 | 0 | URP/GPUI friendly |
| Custom/BlurBoxShader | 1 | 1 | Review SRP Batcher compatibility and mobile cost |
| Universal Render Pipeline/Particles/Unlit | 1 | 0 | URP/GPUI friendly |
| Distant Lands/Lumen/Light Ray | 1 | 0 | Review SRP Batcher compatibility and mobile cost |
| Distant Lands/Lumen/Fake Light | 1 | 0 | Review SRP Batcher compatibility and mobile cost |

Potential duplicate material names:
- New Material: 7 materials
- M_Cong: 2 materials
- New Material 1: 2 materials
- magic_orb2_ADD: 2 materials

## Scene: Assets/Scenes/Preview_Certificates.unity
### Scene Summary
- Root objects: 13
- GameObjects: 86 active=50 inactive=36
- Renderers: 20 enabled+active=0
- MeshRenderers: 20 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=4, active=4, realtime=3, mixed=1, baked=0
- Directional: 1
- Spot: 3
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
  - Spot Light type=Spot mode=Realtime intensity=2383 shadows=None range=50.37
  - Spot Light (2) type=Spot mode=Realtime intensity=2383 shadows=None range=50.37
  - Spot Light (1) type=Spot mode=Realtime intensity=2383 shadows=None range=50.37
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

## Scene: Assets/Scenes/test.unity
### Scene Summary
- Root objects: 4
- GameObjects: 46 active=42 inactive=4
- Renderers: 0 enabled+active=0
- MeshRenderers: 0 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=1, active=1, realtime=0, mixed=1, baked=0
- Directional: 1
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
- ReflectionProbes: 0
- LightProbeGroups: 0, probe count=0
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

## Scene: Assets/Scenes/UI_Creator Scene.unity
### Scene Summary
- Root objects: 19
- GameObjects: 2586 active=46 inactive=2540
- Renderers: 4 enabled+active=1
- MeshRenderers: 3 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 1; unique materials=1; instancing-enabled materials=0
- Static batching flagged renderers: 1/1
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=3, active=1, realtime=0, mixed=3, baked=0
- Directional: 1
- Point: 2
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
- ReflectionProbes: 1
- LightProbeGroups: 1, probe count=18
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|
| Universal Render Pipeline/2D/Sprite-Unlit-Default | 1 | 0 | URP/GPUI friendly |

## Scene: Assets/Scenes/WebView_Mobile.unity
### Scene Summary
- Root objects: 5
- GameObjects: 32 active=27 inactive=5
- Renderers: 0 enabled+active=0
- MeshRenderers: 0 SkinnedMeshRenderers: 0 Terrains: 0
- Meshes: unique=0 approx rendered tris=0 verts=0
- Material slots approx draw submissions before batching: 0; unique materials=0; instancing-enabled materials=0
- Static batching flagged renderers: 0/0
- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.

### Lighting / Probes
- Lights: total=1, active=1, realtime=0, mixed=1, baked=0
- Directional: 1
  - Directional Light type=Directional mode=Mixed intensity=1.45 shadows=Soft range=10
- ReflectionProbes: 0
- LightProbeGroups: 0, probe count=0
- Ambient mode=Skybox, ambientIntensity=1.14, reflectionIntensity=0.9, skybox=Resources/unity_builtin_extra

### Cameras / Volumes
- Camera Main Camera active=True post=False depth=True color=False hdr=False msaa=True
- Volume XHero Mobile Cinematic Look active=False global=True weight=0 profile=Assets/Settings/XHero_Mobile_CinematicLook.asset
  - HTrace enabled=False active=False rays=2 steps=10 scale=0.5 checkerboard=True intensity=0.62

### GPU Instancer Pro
- No GPUIPrefabManager in scene.
- GPUIPrefab components in scene hierarchy: 0
- GPUI Tree Managers: 0; GPUI Terrain components: 0

### GPUI Prefab Prototype Status
- No safe repeated decorative prefab root candidates found.

### Safe GPU Instancer / GPU Instancing Candidates
- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.

### LOD Candidates
- No obvious high-vertex/no-LOD MeshRenderer candidates found.

### Collider Simplification Candidates
- No MeshCollider above 1,200 vertices found.

### Materials / Shaders
| Shader | Material count | Instancing materials | Note |
|---|---:|---:|---|

