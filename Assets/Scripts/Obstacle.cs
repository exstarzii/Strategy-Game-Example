using UnityEngine;

public class Obstacle : MonoBehaviour
{
    void Start()
    {
        var obsRen = GetComponent<Renderer>();
        obsRen.material.SetVector("_Tiling", new Vector4(
            transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z,
            1));
    }
}
