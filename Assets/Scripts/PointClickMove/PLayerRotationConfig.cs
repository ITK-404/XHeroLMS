using UnityEngine;
[CreateAssetMenu(fileName = "PlayerRotationConfig",menuName = "SO/PlayerRotationConfig")]
public class PlayerRotationConfig : ScriptableObject
{
    public float rotationMultiplier = 1;      
    public float minRotationMultiplier = 0.5f; 
    public float maxRotationMultiplier = 1f;   
}