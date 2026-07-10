using UnityEngine;
using UnityEngine.UI;

public class ToggleSwapSprite : MonoBehaviour
{
    [SerializeField] private Image targetImg;
    [SerializeField] private Sprite isOnSprite;
    [SerializeField] private Sprite isOffSprite;

    public void ToggleOn() => targetImg.sprite = isOnSprite;
    public void ToggleOff() => targetImg.sprite = isOffSprite;
}