using UnityEngine;

public class CrossSectionPlaneController:MonoBehaviour
{
    public bool section=true;
    public bool capVisible=true;
    [Range(-1.1f,1.3f)]public float depth=-.35f;
    public Renderer tissue;
    public Mesh body;
    public MeshFilter cap;
    float builtDepth=float.NaN;
    public void Invalidate(){builtDepth=float.NaN;}
    void LateUpdate()
    {
        Shader.SetGlobalFloat("_SectionEnabled",section?1:0);
        cap.gameObject.SetActive(section&&tissue.enabled&&capVisible);
        if(section&&tissue.enabled&&capVisible&&(float.IsNaN(builtDepth)||Mathf.Abs(depth-builtDepth)>.025f)){
            if(cap.sharedMesh!=null)Destroy(cap.sharedMesh);
            cap.sharedMesh=AtlasHeart.Cap(body,depth);builtDepth=depth;
        }
        Shader.SetGlobalFloat("_SectionDepth",float.IsNaN(builtDepth)?depth:builtDepth);
    }
}
