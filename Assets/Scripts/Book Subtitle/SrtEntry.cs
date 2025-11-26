using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public struct SrtEntry
{
    public float Start;
    public float End;
    public string Text;
}
