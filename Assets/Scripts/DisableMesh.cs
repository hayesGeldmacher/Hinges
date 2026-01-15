using UnityEngine;

public class DisableMesh : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        //disables mesh on game start
        if(TryGetComponent<MeshRenderer>(out MeshRenderer mesh))
        {
            mesh.enabled = false;
        }
    }

}
