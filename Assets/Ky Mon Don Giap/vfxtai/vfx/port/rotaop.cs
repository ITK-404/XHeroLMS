using UnityEngine;

public class rotaop : MonoBehaviour
{
    public float sp;
 

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, 0f, sp * Time.deltaTime);
    }
}
