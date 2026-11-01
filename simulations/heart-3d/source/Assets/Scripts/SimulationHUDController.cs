using UnityEngine;

public class SimulationHUDController:MonoBehaviour
{
    public Circulation simulation;
    Font font;GUIStyle label,button;
    readonly Color white=new Color(.98f,.99f,1),muted=new Color(.86f,.91f,.94f),mint=new Color(.43f,.98f,.84f),gold=new Color(1,.85f,.43f);
    void Text(float x,float y,float w,float h,string text,int size=17,Color? color=null,bool bold=false){label.fontSize=size;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;label.normal.textColor=color??white;GUI.Label(new Rect(x,y,w,h),text,label);}
    void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
    bool Button(Rect r,string text,bool active=false){GUI.backgroundColor=active?new Color(.27f,.58f,.52f):new Color(.25f,.34f,.42f);bool result=GUI.Button(r,text,button);GUI.backgroundColor=Color.white;return result;}
    void Line(Vector2 a,Vector2 b,Color c,float width=1){Matrix4x4 old=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);Box(new Rect(a.x,a.y,(b-a).magnitude,width),c);GUI.matrix=old;}
    void OnGUI()
    {
        if(simulation==null||simulation.learning==null)return;
        if(font==null){font=Resources.Load<Font>("Fonts/NotoSansKR");if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},22);label=new GUIStyle(GUI.skin.label){font=font,wordWrap=true};button=new GUIStyle(GUI.skin.button){font=font,fontSize=17,wordWrap=true};}
        GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1));GUI.contentColor=Color.white;
        var s=simulation;var l=s.learning;var c=s.cycle;
        Box(new Rect(0,0,330,900),new Color(.085f,.13f,.18f));Box(new Rect(330,0,1,900),new Color(.27f,.35f,.40f));
        Text(25,24,285,23,"과학 탐구실  /  혈액의 순환",14,mint,true);Text(24,65,300,48,"혈액의 길을 따라",29,white,true);
        Text(26,120,280,56,"실제 해부 모델에서\n들어오고 나가는 혈액을 관찰해요.",17,muted);
        string[] modes={"1   혈액 따라가기","2   자유 관찰","3   확인 문제"};
        for(int i=0;i<3;i++)if(Button(new Rect(25,206+i*49,280,40),modes[i],(int)l.mode==i))l.SetMode((HeartLearningController.LearningMode)i);
        Text(26,382,280,24,"모델 보기",15,muted,true);
        string[] views={"단면","외형","판막"};for(int i=0;i<3;i++)if(Button(new Rect(25+i*96,423,88,37),views[i],s.view==i)){if(i==2)l.SetMode(HeartLearningController.LearningMode.Explore);s.SetView(i);}
        if(l.mode==HeartLearningController.LearningMode.Explore){
            Text(26,490,280,27,"심장 박동수  "+c.bpm.ToString("0")+" BPM",18,white,true);c.bpm=GUI.HorizontalSlider(new Rect(27,531,274,20),c.bpm,35,160);
            Text(26,564,280,25,"관찰 속도  "+c.speed.ToString("0.00")+"배",17,muted);c.speed=GUI.HorizontalSlider(new Rect(27,604,274,20),c.speed,.1f,2);
            if(Button(new Rect(25,644,133,42),c.playing?"일시 정지":"재생"))c.playing=!c.playing;
            if(Button(new Rect(171,644,133,42),"전체 구조")){s.ClearFocus();s.SetView(0);}
        }else if(l.mode==HeartLearningController.LearningMode.Guided){
            Text(26,490,278,55,"혈액 한 무리를\n순서대로 따라가 봅시다.",19,white,true);
            if(Button(new Rect(25,571,280,46),l.automatic?"Ⅱ   자동 진행 멈춤":"▶   자동으로 따라가기",l.automatic)){l.automatic=!l.automatic;if(l.automatic)l.packetPaused=false;}
            if(Button(new Rect(25,635,133,44),"← 이전"))l.Advance(-1);
            if(Button(new Rect(171,635,133,44),"다음 →"))l.Advance(1);
            if(Button(new Rect(25,695,280,36),l.Progress>=1?"이 단계 다시 관찰":l.packetPaused?"혈액 이동 재생":"혈액 이동 멈춤")){if(l.Progress>=1)l.Go(l.step);else l.packetPaused=!l.packetPaused;}
            Text(26,745,280,45,"노란 테두리: 추적하는 혈액\n민트색 화살표: 이동 방향",14,muted);
        }else{
            Text(26,490,278,118,"다음 이동을 예측하고\n오른쪽에서 답을 고르세요.\n\n선택 후 경로와 이유가 나타납니다.",19,white);
            if(Button(new Rect(25,647,280,46),"이 경로 다시 따라가기")){int review=l.step;l.SetMode(HeartLearningController.LearningMode.Guided);l.Go(review);}
        }
        Text(26,795,285,78,"오른쪽 드래그: 회전  ·  휠: 확대\nSpace: 재생 / 정지  ·  ← →: 단계\n정면에서 왼쪽은 인체의 오른쪽",14,muted);
        Text(365,24,800,28,"심장 · 폐순환 · 온몸순환",18,mint,true);Text(1260,24,300,28,"관찰 → 예측 → 확인",16,muted,true);
        if(l.mode==HeartLearningController.LearningMode.Explore){
            Text(365,70,820,33,"자유 관찰  ·  "+c.PhaseName,25,white,true);Text(365,111,830,30,"심장의 방을 클릭하면 그 구조만 드러납니다. 양쪽 심장은 함께 박동합니다.",16,muted);
        }else if(l.mode==HeartLearningController.LearningMode.Quiz&&l.answer<0){
            Text(365,70,830,36,l.question>=HeartLearningController.Questions.Length?"혈액의 순환을 설명해 봅시다":"다음 이동을 예측해 봅시다",25,white,true);
            Text(365,113,830,35,"답을 고르면 이동 경로와 이유를 함께 확인할 수 있습니다.",15,muted);
        }else{
            Text(365,70,830,36,HeartLearningController.Locations[l.step],25,white,true);
            Text(365,113,830,35,"경로를 차례대로 보여 주는 학습 모드입니다. 실제로는 양쪽 심장이 함께 박동합니다.",15,muted);
        }
        if(s.view==2)ValveCard();else if(l.mode!=HeartLearningController.LearningMode.Explore)LearningCard();else ExploreCard();
        if(l.mode==HeartLearningController.LearningMode.Explore&&s.labels&&s.view!=2)DrawLabels();
        if(s.view==2&&s.valveSelection<0)ValveLabels();
        Timeline();
        Text(365,713,825,42,"자료: Z-Anatomy / BodyParts3D · CC BY-SA 4.0\n혈액 입자와 경로선은 관찰용 확대 표현입니다.",13,muted);
    }
    void ValveCard()
    {
        var s=simulation;Box(new Rect(1225,164,350,566),new Color(.11f,.17f,.22f));Text(1248,184,301,26,"판막을 하나씩 비교해요",18,mint,true);
        string[] names={"삼첨판 · 3엽","이첨판 · 2엽","폐동맥판 · 3포켓","대동맥판 · 3포켓"};
        for(int i=0;i<4;i++)if(Button(new Rect(1248,233+i*48,302,39),names[i],s.valveSelection==i))s.SelectValve(i);
        if(Button(new Rect(1248,439,302,35),"네 판막 함께 보기",s.valveSelection<0))s.SelectValve(-1);
        Text(1248,501,302,89,s.valveSelection<2&&s.valveSelection>=0?"방실판막\n판엽의 끝이 건삭을 통해 유두근에 연결됩니다.":"반월판막\n세 개의 주머니형 판엽으로 이루어지며 건삭이 없습니다.",18,white);
        if(Button(new Rect(1248,617,145,43),"충만 단계")){s.cycle.playing=false;s.cycle.SetPhase(.08f);}
        if(Button(new Rect(1405,617,145,43),"분출 단계")){s.cycle.playing=false;s.cycle.SetPhase(.30f);}
        Text(1248,678,301,28,"재생하면 판막의 변형이 반복됩니다.",15,muted);
    }
    void LearningCard()
    {
        var l=simulation.learning;Box(new Rect(1225,164,350,566),new Color(.11f,.17f,.22f));
        if(l.mode==HeartLearningController.LearningMode.Quiz){QuizCard();return;}
        Text(1248,184,300,25,"따라가기  "+(l.step+1)+" / 12",15,mint,true);
        Text(1247,228,303,83,HeartLearningController.Titles[l.step],25,white,true);
        bool red=l.Oxygenated||l.step==5;Box(new Rect(1249,323,8,45),red?new Color(.95f,.21f,.23f):new Color(.57f,.16f,.24f));
        Text(1270,323,265,54,l.step==5?"산소 받기 · 이산화 탄소 내보내기":l.step==10?"산소 전달 · 이산화 탄소 받기":red?"산소가 많은 혈액 · 선홍색":"산소가 적은 혈액 · 암적색",17,white,true);
        Text(1248,394,299,178,HeartLearningController.Explanation[l.step],18,white);
        Box(new Rect(1248,594,303,2),new Color(.30f,.42f,.48f));Text(1248,614,303,89,HeartLearningController.Observe[l.step],18,gold,true);
        if(l.step==5||l.step==10){
            Text(450,611,640,42,l.step==5?"폐 모세혈관:  O2 → 혈액    /    CO2 → 폐":"몸 모세혈관:  O2 → 세포    /    CO2 → 혈액",19,gold,true);
            Text(451,653,680,34,"기체 교환은 심장 바깥에서 일어납니다.",16,white);
        }
    }
    void QuizCard()
    {
        var l=simulation.learning;
        if(l.question>=HeartLearningController.Questions.Length){
            Text(1248,192,300,28,"학습 확인 완료",18,mint,true);Text(1247,250,300,67,l.correct+" / 6  정답",33,white,true);
            Text(1248,342,300,132,"온몸 → 오른쪽 심장 → 폐 → 왼쪽 심장 → 온몸\n\n이 순서를 말로 설명해 보세요.",20,white);
            if(Button(new Rect(1248,520,302,47),"혈액 경로 다시 따라가기"))l.SetMode(HeartLearningController.LearningMode.Guided);
            if(Button(new Rect(1248,583,302,47),"확인 문제 다시 풀기"))l.SetMode(HeartLearningController.LearningMode.Quiz);return;
        }
        var q=HeartLearningController.Questions[l.question];Text(1248,184,300,25,"확인 문제  "+(l.question+1)+" / 6",15,mint,true);
        Text(1248,227,300,87,q.prompt,23,white,true);
        for(int i=0;i<q.choices.Length;i++){
            GUI.enabled=l.answer<0;if(Button(new Rect(1248,329+i*65,302,53),(i+1)+".  "+q.choices[i],l.answer==i))l.Choose(i);GUI.enabled=true;
        }
        if(l.answer>=0){Text(1248,538,301,27,l.answer==q.correct?"정답입니다":"다시 생각해 봅시다",20,l.answer==q.correct?mint:gold,true);Text(1248,579,301,83,q.reason,17,white);
            if(Button(new Rect(1248,670,302,39),"다음 문제 →"))l.NextQuestion();}
        else Text(1248,559,301,80,"모델의 구조를 살펴보고 답을 선택하세요. 이동 경로와 이유는 선택 후에 나타납니다.",17,muted);
    }
    void ExploreCard()
    {
        var s=simulation;var c=s.cycle;Box(new Rect(1225,164,350,566),new Color(.11f,.17f,.22f));
        Text(1248,186,301,28,"움직임 비교하기",18,mint,true);Text(1248,235,301,83,"네 개의 판막은\n언제 열리고 닫힐까요?",23,white,true);
        string av=c.AVOpen>.8f?"열림":c.AVOpen<.1f?"닫힘":"부분 열림",semi=c.SemilunarOpen>.8f?"열림":c.SemilunarOpen<.1f?"닫힘":"전환 중";
        Text(1248,333,300,124,"삼첨판 · 이첨판\n"+av+"\n\n폐동맥판 · 대동맥판\n"+semi,18,white);
        Text(1248,503,300,25,"단면 깊이",17,muted);GUI.enabled=s.view==0;s.section.depth=GUI.HorizontalSlider(new Rect(1249,550,298,20),s.section.depth,-1.05f,1.25f);GUI.enabled=true;
        if(Button(new Rect(1248,591,302,40),s.labels?"이름 표시 켜짐":"이름 표시 꺼짐",s.labels))s.labels=!s.labels;
        if(Button(new Rect(1248,646,302,40),s.transparentVessels?"혈관 벽 반투명":"혈관 벽 불투명",s.transparentVessels)){s.transparentVessels=!s.transparentVessels;s.SetView(s.view);}
    }
    void Timeline()
    {
        var l=simulation.learning;Box(new Rect(350,770,1225,108),new Color(.10f,.16f,.20f));
        Text(369,782,1160,24,"혈액의 이동 순서  ·  각 이름을 눌러 다시 관찰하세요.",14,muted);
        string[] names={"온몸","대정맥","우심방","우심실","폐동맥","폐","폐정맥","좌심방","좌심실","대동맥","온몸"};int[] steps={0,0,1,2,4,5,6,6,7,9,10};
        int current=l.step==0?1:l.step==1?2:l.step==2?3:l.step<=4?4:l.step==5?5:l.step==6?7:l.step<=8?8:l.step==9?9:l.step==10?10:1;
        for(int i=0;i<names.Length;i++){
            float x=368+i*108;if(Button(new Rect(x,819,99,36),names[i],i==current&&(l.mode==HeartLearningController.LearningMode.Guided||l.mode==HeartLearningController.LearningMode.Quiz&&l.answer>=0))){if(l.mode!=HeartLearningController.LearningMode.Guided)l.SetMode(HeartLearningController.LearningMode.Guided);l.Go(steps[i]);}
            if(i<names.Length-1)Text(x+100,825,12,25,"›",14,mint);
        }
    }
    void DrawLabels()
    {
        Callout("우심방",AtlasHeart.RA,new Vector2(385,270));Callout("우심실",AtlasHeart.RV,new Vector2(390,567));Callout("좌심방 · 뒤쪽",AtlasHeart.LA,new Vector2(1030,265));Callout("좌심실",AtlasHeart.LV,new Vector2(1040,580));
    }
    void ValveLabels()
    {
        Callout("삼첨판 · 3엽",AtlasHeart.Tricuspid,new Vector2(410,510));Callout("이첨판 · 2엽",AtlasHeart.Mitral,new Vector2(1025,540));Callout("폐동맥판 · 3포켓",AtlasHeart.Pulmonary,new Vector2(1010,285));Callout("대동맥판 · 3포켓",AtlasHeart.Aortic,new Vector2(400,275));
    }
    void Callout(string name,Vector3 p,Vector2 pos)
    {
        Vector3 world=simulation.viewCamera.WorldToScreenPoint(p);if(world.z<0)return;Vector2 target=new Vector2(world.x*1600/Screen.width,(Screen.height-world.y)*900/Screen.height);Vector2 anchor=pos+new Vector2(pos.x<800?145:0,15);
        Line(anchor,target,new Color(.47f,.69f,.72f),1);Box(new Rect(target.x-2,target.y-2,4,4),mint);Box(new Rect(pos.x-4,pos.y,168,29),new Color(.035f,.055f,.075f,.93f));Text(pos.x,pos.y,170,29,name,16,white);
    }
}
