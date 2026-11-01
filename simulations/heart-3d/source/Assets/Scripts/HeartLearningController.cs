using System;
using UnityEngine;

public class HeartLearningController:MonoBehaviour
{
    public enum LearningMode { Guided, Explore, Quiz }
    public LearningMode mode=LearningMode.Guided;
    public Circulation simulation;
    public int step,question,answer=-1;
    public bool automatic,packetPaused,showRoute=true;
    public int correct;
    public float elapsed;
    public float Progress=>Mathf.Clamp01(elapsed/3.1f);
    public bool ShowPacket=>mode==LearningMode.Guided||mode==LearningMode.Quiz&&question<Questions.Length&&answer>=0;
    public bool Oxygenated=>step>=6&&step<=9;
    public static readonly string[] Titles={"온몸에서 심장으로","우심방에서 우심실로","우심실에 모인 혈액","폐동맥판을 통과","폐를 향해 이동","폐에서 기체 교환","폐정맥에서 좌심방으로","좌심방에서 좌심실로","좌심실의 강한 수축","대동맥을 통해 온몸으로","몸의 세포에 산소 전달","다시 심장으로 돌아오기"};
    public static readonly string[] Locations={"대정맥 → 우심방","우심방 → 삼첨판 → 우심실","우심실","우심실 → 폐동맥판","폐동맥 → 폐","폐의 모세혈관","폐정맥 → 좌심방","좌심방 → 이첨판 → 좌심실","좌심실 → 대동맥판","대동맥 → 온몸","몸의 모세혈관","대정맥 → 우심방"};
    public static readonly string[] Explanation={
        "몸의 세포에 산소를 전달한 혈액이 대정맥을 따라 우심방으로 들어옵니다. 산소가 적어도 혈액은 암적색입니다.",
        "열린 삼첨판을 지나 우심실로 이동합니다. 판막은 압력 차이에 따라 열리고, 반대 방향으로 흐르려 하면 닫힙니다.",
        "우심실은 폐로 보낼 혈액을 받아들입니다. 이어지는 심실 수축 때 삼첨판이 닫혀 우심방으로 되돌아가는 것을 막습니다.",
        "우심실이 수축해 압력이 높아지면 폐동맥판이 열립니다. 이 판막에는 건삭이 없고 세 개의 주머니형 판엽이 있습니다.",
        "폐동맥은 심장에서 폐로 나가는 혈관입니다. 동맥이라는 이름이 산소가 많다는 뜻은 아닙니다.",
        "폐의 모세혈관에서 혈액은 산소를 받아들이고 이산화 탄소를 내보냅니다. 혈액의 색이 더 밝은 선홍색으로 바뀝니다.",
        "폐에서 산소를 받은 혈액이 폐정맥을 따라 좌심방으로 들어옵니다. 폐정맥은 산소가 많은 혈액을 운반하는 정맥입니다.",
        "열린 이첨판(승모판)을 지나 좌심실로 이동합니다. 두 개의 판엽과 건삭이 혈액의 역류를 막는 데 관여합니다.",
        "두꺼운 근육벽을 가진 좌심실이 수축합니다. 이첨판이 닫히고 대동맥판이 열려 혈액을 온몸으로 내보냅니다.",
        "혈액이 대동맥을 따라 심장에서 나갑니다. 같은 순간 오른쪽 심장에서도 폐를 향한 펌프 작용이 일어납니다.",
        "몸의 모세혈관에서 산소가 세포로 전달되고 이산화 탄소가 혈액으로 들어옵니다. 심장 안에서 두 혈액이 서로 바뀌는 것이 아닙니다.",
        "산소가 적어진 혈액이 정맥을 통해 돌아옵니다. 온몸 → 오른쪽 심장 → 폐 → 왼쪽 심장 → 온몸의 순서가 연결됩니다."
    };
    public static readonly string[] Observe={"혈액이 들어오는 화살표를 확인하세요.","삼첨판이 열린 틈과 건삭을 살펴보세요.","다음에 닫혀야 할 판막은 무엇일까요?","삼첨판과 폐동맥판의 상태를 비교하세요.","폐동맥에는 산소가 많은 혈액이 흐를까요?","산소를 얻는 장소를 기억하세요: 폐입니다.","폐정맥의 혈액 색을 폐동맥과 비교하세요.","이첨판과 삼첨판의 판엽 수를 비교하세요.","좌심실 벽이 더 두꺼운 까닭을 생각해 보세요.","동맥은 심장에서 나가는 혈관입니다.","산소를 사용하는 곳은 몸의 세포입니다.","확인 문제로 이동 순서를 점검해 보세요."};
    public static readonly int[] FocusChamber={0,1,1,-2,-2,-2,2,3,3,-2,-2,0};
    public static readonly float[] Phase={.70f,.08f,.70f,.30f,.30f,.70f,.70f,.08f,.30f,.30f,.70f,.70f};

    [Serializable] public class QuizItem
    {
        public string prompt;public string[] choices;public int correct;public string reason;public int reviewStep;
        public QuizItem(string p,string[] c,int a,string r,int s){prompt=p;choices=c;correct=a;reason=r;reviewStep=s;}
    }
    public static readonly QuizItem[] Questions={
        new QuizItem("대정맥에서 돌아온 혈액이 처음 들어가는 방은?",new[]{"좌심방","우심방","좌심실"},1,"대정맥은 온몸에서 돌아온 혈액을 우심방으로 보냅니다.",0),
        new QuizItem("우심방의 혈액이 다음에 통과하는 판막은?",new[]{"삼첨판","대동맥판","이첨판"},0,"우심방과 우심실 사이의 방실판막은 삼첨판입니다.",1),
        new QuizItem("산소가 적은 혈액을 폐로 보내는 혈관은?",new[]{"폐정맥","대동맥","폐동맥"},2,"폐동맥은 심장에서 나가 폐로 향하며 산소가 적은 혈액을 운반합니다.",4),
        new QuizItem("혈액이 산소를 받아들이는 장소는?",new[]{"심실중격","폐의 모세혈관","대동맥판"},1,"기체 교환은 폐에서 일어납니다. 심장의 왼쪽과 오른쪽 혈액은 중격으로 분리됩니다.",5),
        new QuizItem("폐정맥 → 좌심방 다음의 올바른 이동은?",new[]{"이첨판 → 좌심실","삼첨판 → 우심실","폐동맥판 → 폐동맥"},0,"좌심방의 혈액은 이첨판을 지나 좌심실로 들어갑니다.",7),
        new QuizItem("심실이 혈액을 분출할 때 판막 상태는?",new[]{"네 개 모두 열림","방실판막 열림 · 반월판막 닫힘","방실판막 닫힘 · 반월판막 열림"},2,"분출 단계에서는 방실판막이 닫혀 역류를 막고, 반월판막이 열려 혈액이 동맥으로 나갑니다.",8)
    };
    public void SetMode(LearningMode value)
    {
        mode=value;automatic=false;packetPaused=false;elapsed=0;
        if(value==LearningMode.Explore){simulation.ClearFocus();simulation.cycle.playing=true;simulation.flow.visible=true;simulation.SetView(0);}
        else if(value==LearningMode.Guided){simulation.SetView(0);ApplyStep();}
        else {question=0;answer=-1;correct=0;simulation.SetView(0);step=Questions[0].reviewStep;ApplyStep();}
    }
    public void Go(int index){step=Mathf.Clamp(index,0,11);elapsed=0;packetPaused=false;ApplyStep();}
    public void Advance(int direction)
    {
        if(step==11&&direction>0){automatic=false;SetMode(LearningMode.Quiz);return;}
        Go(step+direction);
    }
    public void Choose(int index){if(answer>=0)return;answer=index;elapsed=0;if(index==Questions[question].correct)correct++;}
    public void NextQuestion(){if(answer<0)return;question++;answer=-1;if(question<Questions.Length){step=Questions[question].reviewStep;elapsed=0;ApplyStep();}}
    void ApplyStep()
    {
        simulation.cycle.playing=false;simulation.cycle.SetPhase(Phase[step]);simulation.flow.visible=false;
        simulation.FocusChamber(FocusChamber[step]);
    }
    void Update()
    {
        if(simulation==null)return;
        if(mode==LearningMode.Explore)return;
        simulation.cycle.playing=false;
        if(!packetPaused)elapsed+=Time.unscaledDeltaTime;
        if(mode==LearningMode.Guided&&automatic&&elapsed>5.5f)Advance(1);
    }
    public Vector3[] CurrentPath()
    {
        var right=SplineBloodFlowController.RightRoute;var left=SplineBloodFlowController.LeftRoute;
        Vector3[] Slice(Vector3[] route,int a,int b){var p=new Vector3[b-a+1];Array.Copy(route,a,p,0,p.Length);return p;}
        switch(step){
            case 0:return Slice(right,0,2);case 1:return Slice(right,2,4);case 2:return Slice(right,4,6);
            case 3:return Slice(right,6,8);case 4:return Slice(right,8,10);
            case 5:return new[]{right[10],right[10]+Vector3.up*.12f};
            case 6:return Slice(left,0,2);case 7:return Slice(left,2,4);case 8:return Slice(left,4,8);case 9:return Slice(left,8,11);
            case 10:return new[]{left[11],left[11]+Vector3.up*.12f};default:return Slice(right,0,2);
        }
    }
    public Color PacketColor=>step==5?Color.Lerp(SplineBloodFlowController.Venous,SplineBloodFlowController.Arterial,Progress):step==10?Color.Lerp(SplineBloodFlowController.Arterial,SplineBloodFlowController.Venous,Progress):Oxygenated?SplineBloodFlowController.Arterial:SplineBloodFlowController.Venous;
}
