using UnityEngine;

namespace VertexField.VoluSmokeFX
{
    [ExecuteAlways]
    public class MaterialChangeWatcher : MonoBehaviour
    {
        public string lastAppliedMatGuid;
        public string lastFingerprint;
        public bool armed = true;
    }
}
