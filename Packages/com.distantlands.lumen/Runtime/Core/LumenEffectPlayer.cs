// Distant Lands 2025
// Lumen: Stylized Light FX 2
// The contents of this file are protected under the Unity Asset Store EULA
//
// Controls the runtime behavior of Lumen effects in a Unity scene, including playback and updates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DistantLands.Lumen
{
    /// <summary>
    /// Controls the runtime behavior of Lumen effects in a Unity scene, including playback and updates.
    /// </summary>
    [ExecuteAlways]
    public class LumenEffectPlayer : MonoBehaviour
    {
        /// <summary>
        /// Create a new instance of a Lumen Effect Player for a given effect.
        /// </summary>
        /// <param name="effect">The effect that is played.</param>
        /// <returns>The new Lumen Effect Player.</returns>
        public static LumenEffectPlayer CreatePlayer(LumenEffectProfile effect)
        {
            return CreatePlayer(effect, Vector3.zero);
        }

        /// <summary>
        /// Create a new instance of a Lumen Effect Player at a specific position.
        /// </summary>
        /// <param name="effect">The effect that is played.</param>
        /// <param name="pos">The location to create the effect.</param>
        /// <returns>The new Lumen Effect Player.</returns>
        public static LumenEffectPlayer CreatePlayer(LumenEffectProfile effect, Vector3 pos)
        {
            LumenEffectPlayer player = new GameObject().AddComponent<LumenEffectPlayer>();
            player.transform.position = pos;
            player.gameObject.name = $"{effect.name} Player";
            player.profile = effect;
            return player;
        }

        /// <summary>
        /// Determine the frequency that a particular effect receives updates
        /// Always - updates every frame in the update loop
        /// On Changes - only update when a change is detected in the editor
        /// Via Scripting - only update when the RedoEffect method is called
        /// </summary>
        public enum UpdateFrequency
        {
            Always,
            OnChanges,
            ViaScripting
        }

        /// <summary>
        /// How frequently should the effect update?
        /// </summary>
        [Tooltip("How frequently should the effect update?")]
        public UpdateFrequency updateFrequency = UpdateFrequency.OnChanges;

        /// <summary>
        /// Applies a uniform scale modifier on all effects in the stack
        /// </summary>
        [Tooltip("Applies a uniform scale modifier on all effects in the stack")]
        public float scale = 1;

        /// <summary>
        /// Applies a uniform brightness modifier on all effects in the stack
        /// </summary>
        [Tooltip("Applies a uniform brightness modifier on all effects in the stack")]
        public float brightness = 1;

        /// <summary>
        /// Applies a uniform color modifier on all effects in the stack
        /// </summary>
        [Tooltip("Applies a uniform color modifier on all effects in the stack")]
        [ColorUsage(true, true)]
        public Color color = Color.white;

        float controlledBrightness = 1;
        float controlledScale = 1;
        Color controlledColor = Color.white;

        /// <summary>
        /// Should the layers be displayed in the hierarchy? 
        /// </summary>
        public bool displayLayersInHierarchy = false;




        /// <summary>
        /// Applies a uniform range multiplier on all light layer effects in the stack
        /// </summary>
        [Tooltip("Applies a uniform range multiplier on all light layer effects in the stack")]
        public float range = 1;




        /// <summary>
        /// Controls the current sun direction for this effect. Only applies if the effect is set to not use the main directional light or the Lumen light direction component.
        /// </summary>
        [Tooltip("Controls the current sun direction for this effect.")]
        public Vector3 localSunDirection;

        /// <summary>
        /// Should the Lumen effect automatically assign the sun from the brightest directional light in the scene?
        /// </summary>
        [Tooltip("Should the Lumen effect automatically assign the sun from the brightest directional light in the scene?")]
        public bool autoAssignSun = true;

        /// <summary>
        /// Should the Lumen effect automatically assign the sun from the LumenSun.cs script?
        /// </summary>
        [Tooltip("Should the Lumen effect automatically assign the sun from the LumenSun.cs script?")]
        public bool useLumenSunScript = true;




        /// <summary>
        /// The profile used for the effect
        /// </summary>
        [Tooltip("The profile used for the effect")]
        public LumenEffectProfile profile;

        public enum InitializationBehavior
        {
            Immediate,
            FadeToTargetBrightness,
            FadeToTargetScale,
            FadeToTargetColor,
            SkipInitialization
        }

        /// <summary>
        /// Determines the time it takes to transition or complete a fade
        /// </summary>
        [Tooltip("Determines the time it takes to transition or complete a fade")]
        public float fadingTime = 1;

        /// <summary>
        /// Determines the behavior of the Lumen Effect Player when the effect is initialized
        /// </summary>
        [Tooltip("Determines the behavior of the Lumen Effect Player when the effect is initialized")]
        public InitializationBehavior initializationBehavior = InitializationBehavior.Immediate;

        public enum DeinitializationBehavior
        {
            Immediate,
            FadeBrightnessToZero,
            FadeScaleToZero,
            FadeColorToBlack
        }

        /// <summary>
        /// Determines the behavior of the Lumen Effect Player when the effect is deinitialized or destroyed.
        /// </summary>
        [Tooltip("Determines the behavior of the Lumen Effect Player when the effect is deinitialized or destroyed")]
        public DeinitializationBehavior deinitializationBehavior = DeinitializationBehavior.Immediate;

        public struct InstantiatedLumenLayer
        {
            public Transform transform;
            public Renderer renderer;
            public MeshFilter filter;

        }

        /// <summary>
        /// Manages the references for the currently instantiated effect layers
        /// </summary>
        public List<InstantiatedLumenLayer> instantiatedLumenLayers = new List<InstantiatedLumenLayer>();

        private MaterialPropertyBlock sharedBlock;
        private int totalLayerCount;

        bool isQuitting = false;
        Action StartQuit => () => isQuitting = true;

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
            if (sharedBlock == null)
                sharedBlock = new MaterialPropertyBlock();

            if (Application.isPlaying)
            {
                switch (initializationBehavior)
                {
                    case InitializationBehavior.Immediate:
                        RedoEffect();
                        break;
                    case InitializationBehavior.FadeToTargetBrightness:
                        RedoEffect();
                        controlledBrightness = 0;
                        FadeBrightness(1, fadingTime);
                        break;
                    case InitializationBehavior.FadeToTargetScale:
                        RedoEffect();
                        controlledScale = 0;
                        FadeScale(1, fadingTime);
                        break;
                    case InitializationBehavior.FadeToTargetColor:
                        RedoEffect();
                        controlledColor = Color.black;
                        FadeColor(Color.white, fadingTime);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ClearEffect();
                RedoEffect();
            }
            LumenUtility.OnRedoEntireEffect += RedoEffect;
            Application.quitting += StartQuit.Invoke;

            isQuitting = false;
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled or inactive.
        /// </summary>
        void OnDisable()
        {
            if (Application.isPlaying)
            {
                switch (deinitializationBehavior)
                {
                    case DeinitializationBehavior.Immediate:
                        ClearEffect();
                        break;
                    case DeinitializationBehavior.FadeBrightnessToZero:
                        ClearEffect(fadingTime);
                        FadeBrightness(0, fadingTime);
                        break;
                    case DeinitializationBehavior.FadeScaleToZero:
                        ClearEffect(fadingTime);
                        FadeScale(0, fadingTime);
                        break;
                    case DeinitializationBehavior.FadeColorToBlack:
                        ClearEffect(fadingTime);
                        FadeColor(Color.black, fadingTime);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ClearEffect();
            }
            LumenUtility.OnRedoEntireEffect -= RedoEffect;
            Application.quitting -= StartQuit.Invoke;
        }

        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled.
        /// It is called after all Update functions have been called.
        /// </summary>
        void LateUpdate()
        {
            if (updateFrequency == UpdateFrequency.Always)
                RedoEffect();
        }

        public void RedoEffectFromDelegate()
        {
            if (updateFrequency != UpdateFrequency.OnChanges)
                return;

            RedoEffect();
        }

        private Coroutine fadeBrightnessRoutine;
        private Coroutine fadeScaleRoutine;
        private Coroutine fadeColorRoutine;

        /// <summary>
        /// Fades the effect linearly to a set brightness
        /// </summary>
        /// <param name="brightness">The new target brightness</param>
        /// <param name="time">The amount of time that the fade takes to fully complete.</param>
        public void FadeBrightness(float brightness, float time)
        {
            if (fadeBrightnessRoutine != null)
                StopCoroutine(fadeBrightnessRoutine);

            if (!isQuitting && gameObject.activeInHierarchy)
                fadeBrightnessRoutine = StartCoroutine(FadeBrightnessRoutine(brightness, time));
        }

        IEnumerator FadeBrightnessRoutine(float brightness, float time)
        {
            float t = 0;
            float originalValue = controlledBrightness;
            while (t < time)
            {
                yield return null;
                controlledBrightness = Mathf.Lerp(originalValue, brightness, t / time);
                t += Time.deltaTime;

                if (updateFrequency == UpdateFrequency.OnChanges)
                    RedoEffect();

            }

            controlledBrightness = brightness;
        }

        /// <summary>
        /// Fades the effect linearly to a set scale
        /// </summary>
        /// <param name="scale">The new target scale</param>
        /// <param name="time">The amount of time that the fade takes to fully complete.</param>
        public void FadeScale(float scale, float time)
        {
            if (fadeScaleRoutine != null)
                StopCoroutine(fadeScaleRoutine);

            if (!isQuitting && gameObject.activeInHierarchy)
                fadeScaleRoutine = StartCoroutine(FadeScaleRoutine(scale, time));
        }

        IEnumerator FadeScaleRoutine(float scale, float time)
        {
            float t = 0;
            float originalValue = controlledScale;
            while (t < time)
            {
                yield return null;
                controlledScale = Mathf.Lerp(originalValue, scale, t / time);
                t += Time.deltaTime;

                if (updateFrequency == UpdateFrequency.OnChanges)
                    RedoEffect();
            }

            controlledScale = scale;
        }

        /// <summary>
        /// Fades the effect linearly to a set color
        /// </summary>
        /// <param name="color">The new target color</param>
        /// <param name="time">The amount of time that the fade takes to fully complete.</param>
        public void FadeColor(Color color, float time)
        {
            if (fadeColorRoutine != null)
                StopCoroutine(fadeColorRoutine);

            if (!isQuitting && gameObject.activeInHierarchy)
                fadeColorRoutine = StartCoroutine(FadeColorRoutine(color, time));
        }

        IEnumerator FadeColorRoutine(Color color, float time)
        {
            float t = 0;
            Color originalValue = controlledColor;
            while (t < time)
            {
                yield return null;
                controlledColor = Color.Lerp(originalValue, color, t / time);
                t += Time.deltaTime;

                if (updateFrequency == UpdateFrequency.OnChanges)
                    RedoEffect();
            }

            controlledColor = color;
        }

        [ContextMenu("Refresh Effect")]
        void RefreshEffect()
        {
            ClearEffect();
            RedoEffect();
        }
        [ContextMenu("Toggle Display")]
        void ToggleDisplay()
        {
            displayLayersInHierarchy = !displayLayersInHierarchy;
            ClearEffect();
            RedoEffect();
        }

        /// <summary>
        /// Redraws the effect stack.
        /// </summary>
        public void RedoEffect()
        {
            if (!gameObject || !gameObject.scene.IsValid() || gameObject.scene.name == null)
            {
                return;
            }
            ClearEffect();

            if (profile == null) { return; }
            if (profile.layers.Count == 0) { return; }

            totalLayerCount = GetTotalLayerCount(profile);
            UpdateMeshPool();
            int currentMeshIndex = 0;

            void DrawInstancedMesh(LumenEffectLayer layer, Vector3 posOffset, Vector3 rotOffset, Vector3 scaleOffset, Color colorOffset, float brightnessOffset)
            {
                if (!layer.active) return;

                if (layer is EffectStackLayer)
                {
                    EffectStackLayer effectStack = (EffectStackLayer)layer;

                    if (!effectStack.profile)
                        return;

                    if (effectStack.profile == profile)
                        return;

                    foreach (LumenEffectLayer childLayer in effectStack.profile.layers)
                        DrawInstancedMesh(childLayer, layer.position * scale, layer.rotation, layer.scale * scale, layer.color, layer.brightness);

                    return;
                }


                if (layer.mesh == null) return;

                if (layer.repeat && layer.repeatCount > 0)
                {
                    float adjustedCount = Mathf.Max(layer.repeatCount, 1);
                    System.Random random = new System.Random(layer.repeatSeed);

                    for (int index = 0; index < adjustedCount; index++)
                    {
                        InstantiatedLumenLayer currentMesh = instantiatedLumenLayers[currentMeshIndex];
                        float completionPercentage = 0;

                        switch (layer.repeatDistribution)
                        {
                            case LumenEffectLayer.RepeatDistributionStyle.Uniform:
                                completionPercentage = (float)index / (float)Mathf.Max(adjustedCount - 1, 1);
                                currentMesh.transform.localPosition = layer.position * scale + layer.repeatPositionSpread * completionPercentage;
                                currentMesh.transform.localEulerAngles = layer.rotation + layer.repeatRotationSpread * completionPercentage;
                                currentMesh.transform.localScale = scale * Vector3.Lerp(layer.scale, new Vector3(layer.scale.x * layer.repeatScaleSpread.x, layer.scale.y * layer.repeatScaleSpread.y, layer.scale.z * layer.repeatScaleSpread.z), completionPercentage);
                                break;
                            case LumenEffectLayer.RepeatDistributionStyle.Random:
                                completionPercentage = (float)random.NextDouble();
                                currentMesh.transform.localPosition = layer.position * scale + layer.repeatPositionSpread * completionPercentage;
                                currentMesh.transform.localEulerAngles = layer.rotation + layer.repeatRotationSpread * completionPercentage;
                                currentMesh.transform.localScale = scale * Vector3.Lerp(layer.scale, new Vector3(layer.scale.x * layer.repeatScaleSpread.x, layer.scale.y * layer.repeatScaleSpread.y, layer.scale.z * layer.repeatScaleSpread.z), completionPercentage);
                                break;
                            case LumenEffectLayer.RepeatDistributionStyle.Curve:
                                completionPercentage = layer.repeatCurve.Evaluate(index / Mathf.Max(adjustedCount - 1, 1));
                                currentMesh.transform.localPosition = layer.position * scale + layer.repeatPositionSpread * completionPercentage;
                                currentMesh.transform.localEulerAngles = layer.rotation + layer.repeatRotationSpread * completionPercentage;
                                currentMesh.transform.localScale = scale * Vector3.Lerp(layer.scale, new Vector3(layer.scale.x * layer.repeatScaleSpread.x, layer.scale.y * layer.repeatScaleSpread.y, layer.scale.z * layer.repeatScaleSpread.z), completionPercentage);
                                break;
                        }


                        float RemappedRandom(float range) { return ((float)random.NextDouble() - 0.5f) * 2f * range; }

                        if (layer.repeatVariation)
                        {
                            currentMesh.transform.localPosition += new Vector3(RemappedRandom(layer.repeatPositionVariation.x), RemappedRandom(layer.repeatPositionVariation.y), RemappedRandom(layer.repeatPositionVariation.z));
                            currentMesh.transform.localEulerAngles += new Vector3(RemappedRandom(layer.repeatRotationVariation.x), RemappedRandom(layer.repeatRotationVariation.y), RemappedRandom(layer.repeatRotationVariation.z));
                            currentMesh.transform.localScale *= 1f - (float)random.NextDouble() * layer.repeatScaleVariation;
                        }

                        MeshRenderer renderer = (MeshRenderer)currentMesh.renderer;
                        currentMesh.transform.gameObject.layer = gameObject.layer;
                        renderer.sharedMaterial = layer.EffectMaterial;
                        MeshFilter meshFilter = currentMesh.filter;

                        sharedBlock?.Clear();

                        if (layer is DynamicRayLayer dynamicRay)
                        {
                            renderer.bounds = new Bounds(Vector3.zero, new Vector3(dynamicRay.rayLength, dynamicRay.rayLength, dynamicRay.rayLength));
                            sharedBlock.SetFloat(LumenShaderIDs.AutoAssignSun, autoAssignSun ? 1f : 0f);
                            sharedBlock.SetFloat(LumenShaderIDs.UseLumenSunScript, useLumenSunScript ? 1f : 0f);
                            sharedBlock.SetVector(LumenShaderIDs.SunDirection, localSunDirection);
                        }


                        if (layer is LumenLightLayer lightLayer)
                        {
                            sharedBlock.SetFloat(LumenShaderIDs.Range, range * lightLayer.range);
                            currentMesh.transform.localScale = Vector3.one * range * lightLayer.range * 2;
                        }

                        meshFilter.mesh = layer.mesh;
                        sharedBlock.SetColor(LumenShaderIDs.Color, color * controlledColor * layer.color * colorOffset * layer.repeatColors.Evaluate(completionPercentage));
                        sharedBlock.SetFloat(LumenShaderIDs.Brightness, brightness * controlledBrightness * layer.brightness * brightnessOffset * Mathf.Lerp(1, (1 - layer.repeatBrightnessSpread), completionPercentage));

                        layer.DrawLayer(sharedBlock, renderer);
                        renderer.SetPropertyBlock(sharedBlock);
                        currentMeshIndex++;
                    }
                }
                else
                {
                    InstantiatedLumenLayer currentMesh = instantiatedLumenLayers[currentMeshIndex];

                    currentMesh.transform.localPosition = (layer.position + posOffset) * scale;
                    currentMesh.transform.localEulerAngles = layer.rotation + rotOffset;
                    currentMesh.transform.localScale = (new Vector3(layer.scale.x * scaleOffset.x, layer.scale.y * scaleOffset.y, layer.scale.z * scaleOffset.z)) * scale;
                    if (displayLayersInHierarchy)
                        currentMesh.transform.gameObject.hideFlags = HideFlags.DontSaveInEditor;
                    else
                        currentMesh.transform.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
                    sharedBlock.Clear();

                    currentMesh.transform.gameObject.layer = gameObject.layer;
                    MeshRenderer renderer = (MeshRenderer)currentMesh.renderer;
                    renderer.sharedMaterial = layer.EffectMaterial;
                    MeshFilter meshFilter = currentMesh.filter;

                    sharedBlock.Clear();

                    if (layer is DynamicRayLayer dynamicRay)
                    {
                        renderer.bounds = new Bounds(Vector3.zero, new Vector3(dynamicRay.rayLength, dynamicRay.rayLength, dynamicRay.rayLength));
                        sharedBlock.SetFloat(LumenShaderIDs.AutoAssignSun, autoAssignSun ? 1f : 0f);
                        sharedBlock.SetFloat(LumenShaderIDs.UseLumenSunScript, useLumenSunScript ? 1f : 0f);
                        sharedBlock.SetVector(LumenShaderIDs.SunDirection, localSunDirection);
                    }

                    if (layer is LumenLightLayer lightLayer)
                    {
                        sharedBlock.SetFloat(LumenShaderIDs.Range, range * lightLayer.range);
                        currentMesh.transform.localScale = Vector3.one * range * lightLayer.range * 2;
                    }
                    meshFilter.mesh = layer.mesh;
                    sharedBlock.SetColor(LumenShaderIDs.Color, color * controlledColor * layer.color * colorOffset);
                    sharedBlock.SetFloat(LumenShaderIDs.Brightness, brightness * controlledBrightness * layer.brightness * brightnessOffset);

                    layer.DrawLayer(sharedBlock, renderer);
                    renderer.SetPropertyBlock(sharedBlock);
                    currentMeshIndex++;
                }

            }


            foreach (LumenEffectLayer layer in profile.layers)
            {
                if (layer == null) continue;
                DrawInstancedMesh(layer, Vector3.zero, Vector3.zero, Vector3.one, Color.white, 1);
            }

        }

        /// <summary>
        /// Counts the total number of meshes needed to draw the effect (including repeated iterations and effect stacks)
        /// </summary>
        public int GetTotalLayerCount(LumenEffectProfile lProfile)
        {
            int currentLayerCount = 0;

            foreach (LumenEffectLayer layer in profile.layers)
            {
                if (!layer.active) continue;

                if (layer is EffectStackLayer)
                {
                    var effectStack = (EffectStackLayer)layer;
                    if (effectStack.profile && effectStack.profile != lProfile && effectStack.profile != profile)
                        currentLayerCount += GetTotalLayerCount(effectStack.profile);

                    continue;
                }

                if (layer.mesh == null) continue;

                if (layer.repeat && layer.repeatCount > 0)
                {
                    currentLayerCount += layer.repeatCount;
                    continue;
                }
                currentLayerCount++;
            }

            return currentLayerCount;
        }

        /// <summary>
        /// Updates the pool of meshes used in the effect. Can be heavy or create garbage if there are large changes happening often so use sparingly!
        /// </summary>
        public void UpdateMeshPool()
        {
            while (instantiatedLumenLayers.Count > totalLayerCount)
            {
                Destroy(instantiatedLumenLayers[^1].transform.gameObject);
                instantiatedLumenLayers.RemoveAt(instantiatedLumenLayers.Count - 1);
            }
            while (instantiatedLumenLayers.Count < totalLayerCount)
            {
                GameObject obj = new GameObject($"Layer");
                obj.layer = gameObject.layer;
                if (displayLayersInHierarchy)
                    obj.hideFlags = HideFlags.DontSaveInEditor;
                else
                    obj.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
                obj.transform.parent = transform;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = obj.AddComponent<MeshFilter>();

                instantiatedLumenLayers.Add(new InstantiatedLumenLayer() { filter = meshFilter, renderer = renderer, transform = obj.transform });
            }
        }

        /// <summary>
        /// Clears the effect stack
        /// </summary>
        public void ClearEffect()
        {
            if (!gameObject.scene.IsValid() || gameObject.scene.name == null)
            {
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    if (transform.GetChild(i) != null)
                        Destroy(transform.GetChild(i).gameObject);
                }
                else
                {
                    if (transform.GetChild(i) != null)
                        DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }

            instantiatedLumenLayers.Clear();
        }

        /// <summary>
        /// Clears the effect stack
        /// </summary>
        /// <param name="delay">Will clear the effect after a set delay (in seconds)</param>
        public void ClearEffect(float delay)
        {
            if (!isQuitting && gameObject.activeInHierarchy)
                StartCoroutine(DelayClear(delay));
        }

        IEnumerator DelayClear(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearEffect();
        }

    }
}
