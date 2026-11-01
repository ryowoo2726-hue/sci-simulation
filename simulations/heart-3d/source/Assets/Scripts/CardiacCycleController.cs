using UnityEngine;

public class CardiacCycleController:MonoBehaviour
{
    [Range(35,160)] public float bpm=75;
    [Range(.1f,2)] public float speed=1;
    public bool playing=true;
    public float Phase{get;private set;}
    public float AVOpen{get;private set;}
    public float SemilunarOpen{get;private set;}
    public float AtrialContraction{get;private set;}
    public float VentricularContraction{get;private set;}
    public float Delta{get;private set;}
    public string PhaseName=>Phase<.15f?"심방 수축기":Phase<.45f?"심실 수축기":"심실 이완기";
    public static Vector4 Evaluate(float t)
    {
        t=Mathf.Repeat(t,1);
        if(t<.15f)return new Vector4(1,0,Mathf.Sin(t/.15f*Mathf.PI),0);
        if(t<.45f)return new Vector4(0,1,0,Mathf.SmoothStep(0,1,(t-.15f)/.30f));
        return new Vector4(.6f,0,0,1-Mathf.SmoothStep(0,1,(t-.45f)/.55f));
    }
    public void SetPhase(float t){Phase=Mathf.Repeat(t,1);Apply(true);}
    void Update(){Delta=playing?Time.deltaTime*speed:0;Phase=Mathf.Repeat(Phase+Delta*bpm/60,1);Apply(false);}
    void Apply(bool instant)
    {
        Vector4 state=Evaluate(Phase);float blend=instant?1:1-Mathf.Exp(-Delta*75);
        AVOpen=Mathf.Lerp(AVOpen,state.x,blend);SemilunarOpen=Mathf.Lerp(SemilunarOpen,state.y,blend);
        AtrialContraction=state.z;VentricularContraction=state.w;
        Shader.SetGlobalFloat("_AtrialContraction",state.z);Shader.SetGlobalFloat("_VentricularContraction",state.w);
    }
    public bool CanPass(bool av)=>av?Phase<.15f||Phase>=.45f:Phase>=.15f&&Phase<.45f;
    public float FlowRate(bool av)=>av?(Phase<.15f?1.2f:.6f):(Phase>=.15f&&Phase<.45f?3f:0);
    public static float ECG(float t)
    {
        float G(float c,float w,float h){float x=(t-c)/w;return h*Mathf.Exp(-x*x);}
        return G(.07f,.028f,.16f)-G(.16f,.01f,.16f)+G(.19f,.012f,1)-G(.22f,.013f,.27f)+G(.43f,.055f,.30f);
    }
}
