using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FishShaderRandomizer : MonoBehaviour
{
    private static readonly int RandomPhaseId =
        Shader.PropertyToID("_RandomPhase");

    private Renderer fishRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        fishRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        fishRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(
            RandomPhaseId,
            Random.Range(0f, Mathf.PI * 2f)
        );

        fishRenderer.SetPropertyBlock(propertyBlock);
    }
}