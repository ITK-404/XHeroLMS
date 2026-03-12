using UnityEngine;

[CreateAssetMenu(fileName = "Mail Text Config",menuName = "Config/Mail Text Config")]
public class MailTextConfig : ScriptableObject
{
    // sprite
    public Sprite bgSprite;
    public Sprite readStateSprite;
    // color
    public Color titleColor;
    public Color descriptionColor;
    public Color readStateColor;
    public Color timeSinceColor;
    
    // others
    public Material iconMaterial;
}

