using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSessionConfig", menuName = "Game Session Config")]
public class GameSessionConfig : ScriptableObject
{
    public bool canSave = false;
    public bool canLoad = false;
    public float MinSaveInterval = 1f;
}

