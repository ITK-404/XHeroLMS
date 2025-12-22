using System;
using TMPro;
using UnityEngine;

public class AutoResetInputField : MonoBehaviour
{
    private TMP_InputField _inputField;
    [SerializeField] private string defaultText = "";

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        if (_inputField)
        {
            _inputField.text = defaultText;
        }
    }
}
