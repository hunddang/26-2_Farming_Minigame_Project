using System.Collections.Generic;
using NightCafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NightStoryAuthoring
{
    public static void Attach(NightGame game)
    {
        var content = new GameObject("Nights 04–05 Content").AddComponent<NightStoryContent>();
        game.story = content;
        content.securityChair=Group(content.transform,"Security chair",CafeWorld.PlanPoint(8, 3.5f, 2.65f),8);
        Shape(content.securityChair,"Seat",Vector2.zero,new Vector2(0.8f,0.8f),Color.white,3,"chair");
        content.securityChair.rotation=Quaternion.Euler(0,0,180);
        var hunts = new List<CafeInteractable>();
        var finals = new List<CafeInteractable>();
        var hunt = Group(content.transform,"Ceiling hunter"); content.huntObjects=hunt.gameObject;
        CafeInteractable H(StoryAction action,string name,Vector2 p,string art="note",int index=0)
        {
            var item=Task(hunt,action,name,p,art,index); hunts.Add(item);return item;
        }
        H(StoryAction.Leak,"천장 누수 지점",new Vector2(-12,-6),"circle");
        content.leak=Group(hunt,"Water drops",new Vector2(-12,-6),9).gameObject;
        for(int i=0;i<7;i++) Shape(content.leak.transform,"Droplet",new Vector2(Mathf.Sin(i)*0.4f,i*0.22f),Vector2.one*0.1f,new Color32(110,166,174,255),5,"circle");
        H(StoryAction.Extinguisher,"벽면 소화기",new Vector2(-18,-5.5f),"extinguisher");
        H(StoryAction.Net,"포획용 그물",new Vector2(18,8),"capture-net");
        H(StoryAction.Anchor,"그물 고정구",new Vector2(18,5.8f),"anchor-pin");
        var markers=new List<Transform>();
        foreach(int room in new[]{9,10,11,12,13})
        {
            Vector2 p = room switch
            {
                9 => new Vector2(-14,-8), 10 => new Vector2(.6f,-8),
                11 => new Vector2(20,5.6f), 13 => new Vector2(3,7),
                _ => CafeWorld.RoomCenter(room)
            };
            var item=H(StoryAction.Trap,"함정 설치 / 회수",p,"circle",room);
            item.GetComponentInChildren<SpriteRenderer>().size=new Vector2(1.8f,1.8f);
            item.GetComponentInChildren<SpriteRenderer>().color=new Color(1,0.8f,0.2f,0.7f);
            var column=Shape(item.transform,"Yellow light column",new Vector2(0,0.9f),new Vector2(0.38f,2.4f),new Color(1,0.8f,0.2f,0.25f),4);
            markers.Add(item.transform);
        }
        content.trapMarkers=markers.ToArray();
        content.hunter=Actor(hunt,"낙하자",new Color32(119,77,83,255),9);
        content.powder=Group(hunt,"Extinguisher powder",Vector2.zero,9).gameObject;
        for(int i=0;i<12;i++) Shape(content.powder.transform,"Powder",new Vector2(Mathf.Sin(i*2.4f)*i*0.12f,Mathf.Cos(i*2.4f)*i*0.12f),Vector2.one*0.65f,new Color(0.9f,0.9f,0.82f,0.5f),13,"circle");
        content.powder.SetActive(false);
        content.closedNet=Group(hunt,"Closed net",Vector2.zero,9).gameObject;
        for(int i=-3;i<=3;i++)
        {
            Shape(content.closedNet.transform,"Net thread",new Vector2(i*0.28f,0),new Vector2(0.035f,2.1f),CafeUi.Paper,15);
            Shape(content.closedNet.transform,"Net thread",new Vector2(0,i*0.28f),new Vector2(2.1f,0.035f),CafeUi.Paper,15);
        }
        content.closedNet.SetActive(false); content.trap=Group(hunt,"Armed trap",Vector2.zero,9);
        Shape(content.trap,"Net perimeter",Vector2.zero,new Vector2(2.2f,2.2f),new Color(0.75f,0.65f,0.3f,0.3f),2,"circle");
        content.huntTasks=hunts.ToArray(); hunt.gameObject.SetActive(false);

        var final=Group(content.transform,"Last shift restoration"); content.finalObjects=final.gameObject;
        CafeInteractable F(StoryAction action,string name,Vector2 p,string art="note",int index=0)
        {
            var item=Task(final,action,name,p,art,index);finals.Add(item);return item;
        }
        for(int i=0;i<CafeWorld.BlackoutChannels;i++)
        {
            Vector2 p = i == 2 ? CafeWorld.PlanPoint(11, 5.5f, 3.55f)
                : CafeWorld.RoomCenter(CafeWorld.CameraRooms[i]) + new Vector2(i == 1 ? 2 : 0, 2.3f);
            var digit=F(StoryAction.DarkCameraDigit,new[]{"손님","카운터 알바생","창고 알바생","사장님"}[i]+" 카메라 아래 숫자",p,"paper",i);
            Writing(digit.transform,game.tuning.cameraPassword[DigitRank(i)].ToString(),30,CafeUi.Amber);
        }
        F(StoryAction.Computer,"숙직실 컴퓨터",CafeWorld.PlanPoint(12, 8, 2.85f),"monitor");
        F(StoryAction.Roster,"쓰레기통의 직원 근무표",new Vector2(-17,5),"note");
        F(StoryAction.Accident,"시계 아래 서랍 · 사고 기록",new Vector2(-11,7),"book");
        F(StoryAction.Identity,"자신과 닮은 형체 · 명찰 확인",new Vector2(18,-8),"player-body");
        content.identity=finals[finals.Count-1].transform;
        Shape(content.identity,"Head",new Vector2(0,0.25f),new Vector2(0.35f,0.35f),Color.white,10,"player-head");
        Shape(content.identity,"Bandaged left hand",new Vector2(-0.3f,0),new Vector2(0.18f,0.28f),CafeUi.Paper,9);
        for(int i=0;i<3;i++) F(StoryAction.Badge,"직원 명찰 · "+(i==0?game.tuning.victimName:i==1?"이도현":"박지우"),new Vector2(17+2*i,7.55f),"staff-badge",i);
        F(StoryAction.WireCode,"촉수 표면의 잠금장치 번호",new Vector2(38,2),"note");
        F(StoryAction.Control,"냉장 설비 제어함",new Vector2(46,2),"monitor");
        F(StoryAction.ExhaustBox,"외부 배기구를 막는 상자",new Vector2(93.2f,1.1f),"exhaust-crate");
        F(StoryAction.VictimCup,"오래된 명찰이 붙은 컵",new Vector2(23,-5.95f),"cup");
        F(StoryAction.CupTable,"숙직실 테이블 · 컵 두기",CafeWorld.PlanPoint(12, 7.6f, 5.42f),"cup");
        var exit = F(StoryAction.Exit,"카페 출입문",new Vector2(72,0),"door").GetComponentInChildren<SpriteRenderer>();
        exit.size = new Vector2(CafeWorld.DoorWidth, .48f);
        exit.transform.localRotation = Quaternion.Euler(0, 0, 90);
        F(StoryAction.Departure,"문 안쪽 퇴실 확인 버튼",new Vector2(70.6f,1.6f),"monitor");
        F(StoryAction.FinalMenu,"로비 메뉴판의 마지막 문장",new Vector2(-11,-4.1f),"paper");
        content.spirit=Actor(final,"마지막 마감 근무자",new Color32(119,161,155,255),12);
        content.bandage=Group(content.spirit,"Left hand bandage").gameObject;
        Shape(content.bandage.transform,"Bandage",new Vector2(-0.34f,0),new Vector2(0.18f,0.25f),CafeUi.Paper,12);
        content.memoryActor=Actor(final,"A의 마지막 영상",new Color32(131,151,151,255),14);
        content.memoryActor.gameObject.SetActive(false);
        content.smoke=Group(final,"Trapped smoke",new Vector2(90,0),15).gameObject;
        for(int i=0;i<14;i++) Shape(content.smoke.transform,"Smoke",new Vector2(-8+i*1.2f,Mathf.Sin(i)*1.8f),new Vector2(2.4f,2.1f),new Color(0.18f,0.18f,0.2f,0.3f),8,"circle");
        var clock=Group(content.transform,"Accident wall clock",new Vector2(-10.8f,8.5f),8);
        content.wallClock=Writing(clock,"00:00:00",18,CafeUi.Paper);
        content.finalTasks=finals.ToArray(); final.gameObject.SetActive(false);

        var root=game.view.transform;
        content.darkness=CafeUiAuthoring.Panel(root,"Directional flashlight sight",19,130,1488,810,Color.white);
        content.darkness.transform.SetAsFirstSibling();
        var material=new Material(Shader.Find("NightCafe/Flashlight Sight"));
        const string materialPath="Assets/Art/NightCafe/FlashlightSight.mat";
        var saved=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(saved==null) AssetDatabase.CreateAsset(material,materialPath);
        else { Object.DestroyImmediate(material); material=saved; }
        content.darknessMaterial=material;
        content.darkness.material=material;

        var panel=CafeUiAuthoring.Panel(root,"Investigation form",320,80,1280,930,CafeUi.Ink);
        content.form=panel.gameObject; panel.raycastTarget=true;
        content.title=CafeUiAuthoring.Label(panel.transform,"",48,34,1184,65,33,CafeUi.Amber);
        content.body=CafeUiAuthoring.Label(panel.transform,"",48,110,1184,170,24,CafeUi.Paper);
        content.input=CafeUiAuthoring.Label(panel.transform,"",48,245,1184,65,36,CafeUi.Green,TextAnchor.MiddleCenter);
        content.choices=new Button[6]; content.choiceLabels=new Text[6];
        for(int i=0;i<6;i++)
        {
            content.choices[i]=CafeUiAuthoring.Button(panel.transform,"",48,320+i*85,1184,70,null);
            content.choiceLabels[i]=content.choices[i].GetComponentInChildren<Text>();
            var style=content.choices[i].gameObject.AddComponent<CafeButtonStyle>();
            style.idleSprite=CafeSceneAuthoring.Sprite("button");style.selectedSprite=CafeSceneAuthoring.Sprite("button-selected");
        }
        CafeUiAuthoring.Label(panel.transform,"W/S 선택 · Space/Enter 확인 · Esc 닫기",48,850,1184,45,21,CafeUi.Muted);
        panel.gameObject.SetActive(false);

        var movie=CafeUiAuthoring.Panel(root,"Memory and ending",0,108,1490,849,new Color(0.01f,0.02f,0.025f,0.58f));
        content.cinema=movie.gameObject; movie.raycastTarget=true;
        var preview=new GameObject("CCTV ending preview",typeof(RectTransform),typeof(RawImage));
        preview.transform.SetParent(movie.transform,false);
        content.cinemaFeed=preview.GetComponent<RawImage>();
        var previewRect=content.cinemaFeed.rectTransform;previewRect.anchorMin=previewRect.anchorMax=new Vector2(0,1);previewRect.pivot=new Vector2(0,1);
        previewRect.anchoredPosition=new Vector2(340,-40);previewRect.sizeDelta=new Vector2(810,456);
        preview.SetActive(false);
        content.cinemaCaption=CafeUiAuthoring.Label(movie.transform,"",90,520,1310,240,29,CafeUi.Paper,TextAnchor.MiddleCenter);
        movie.gameObject.SetActive(false);
        var effect=CafeUiAuthoring.Panel(game.view.feedImage.transform,"Broken CCTV",0,0,1280,720,new Color(0.01f,0.02f,0.03f,0.96f));
        content.feedEffect=effect.gameObject;
        content.feedCaption=CafeUiAuthoring.Label(effect.transform,"",60,160,1160,340,36,CafeUi.Paper,TextAnchor.MiddleCenter);
        content.cracks=new Image[8];
        content.handprint=CafeUiAuthoring.Panel(game.view.feedImage.transform,"Hand behind glass",0,0,1280,720,Color.clear).gameObject;
        var palm=CafeUiAuthoring.Panel(content.handprint.transform,"Palm",510,340,90,110,new Color(0.7f,0.72f,0.67f,0.4f));
        palm.sprite=CafeSceneAuthoring.Sprite("circle");
        for(int finger=0;finger<5;finger++) CafeUiAuthoring.Panel(content.handprint.transform,"Finger",510+finger*19,290-Mathf.Sin(finger*0.8f)*20,12,70,new Color(0.7f,0.72f,0.67f,0.4f));
        content.faceOutline=CafeUiAuthoring.Panel(game.view.feedImage.transform,"Face behind glass",0,0,1280,720,Color.clear).gameObject;
        var face=CafeUiAuthoring.Panel(content.faceOutline.transform,"Face",480,110,320,440,new Color(0.1f,0.12f,0.12f,0.85f));face.sprite=CafeSceneAuthoring.Sprite("circle");
        CafeUiAuthoring.Panel(content.faceOutline.transform,"Left eye",540,260,35,8,CafeUi.Paper);
        CafeUiAuthoring.Panel(content.faceOutline.transform,"Right eye",700,260,35,8,CafeUi.Paper);
        content.handprint.SetActive(false);content.faceOutline.SetActive(false);
        for(int i=0;i<8;i++)
        {
            var crack=CafeUiAuthoring.Panel(game.view.feedImage.transform,"Screen fracture",i<4?30:1240,30+i%4*180,160,3,CafeUi.Paper);
            crack.rectTransform.localRotation=Quaternion.Euler(0,0,i%2==0?35:-35);content.cracks[i]=crack;crack.gameObject.SetActive(false);
        }
        effect.gameObject.SetActive(false);
        game.view.GetComponent<CafeFont>().Refresh();
    }
    private static int DigitRank(int camera) => new[]{0,2,3,1}[camera];
    public static Transform Group(Transform parent,string name,Vector2 position=default,int layer=-1)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.gameObject.layer=layer<0?parent.gameObject.layer:layer;return t;
    }
    private static SpriteRenderer Shape(Transform parent,string name,Vector2 position,Vector2 size,Color color,int order,string art="pixel")
    {
        var r=CafeWorldAuthoring.Shape(name,position,size,color,order,false,parent);r.sprite=CafeSceneAuthoring.Sprite(art);r.size=size;return r;
    }
    private static CafeInteractable Task(Transform parent,StoryAction action,string name,Vector2 position,string art,int index)
    {
        var layer=CafeWorld.RoomLayer(position);var t=Group(parent,name,position,layer);
        var r=Shape(t,"Artwork",Vector2.zero,new Vector2(0.8f,0.7f),Color.white,6,art);
        var item=t.gameObject.AddComponent<CafeInteractable>();item.kind=InteractionKind.Story;item.storyAction=action;item.title=name;item.taskIndex=index;item.ConfigureMarker(r);
        return item;
    }
    private static Transform Actor(Transform parent,string name,Color color,int layer)
    {
        var t=Group(parent,name,Vector2.zero,layer);
        Shape(t,"Body",Vector2.zero,new Vector2(0.8f,1.1f),color,9,"circle");
        Shape(t,"Head",new Vector2(0,0.37f),new Vector2(0.56f,0.6f),color,10,"circle");
        foreach(int sign in new[]{-1,1})
        {
            Shape(t,"Arm",new Vector2(0.48f*sign,-0.15f),new Vector2(0.2f,0.9f),color,9,"circle");
            Shape(t,"Eye",new Vector2(0.13f*sign,0.4f),new Vector2(0.1f,0.06f),CafeUi.Amber,11);
        }
        return t;
    }
    private static Text Writing(Transform parent,string value,int size,Color color)
    {
        var canvas=new GameObject("Writing",typeof(Canvas)).GetComponent<Canvas>();canvas.transform.SetParent(parent,false);canvas.transform.localScale=Vector3.one*0.02f;
        canvas.gameObject.layer=parent.gameObject.layer;canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=11;
        var label=CafeUiAuthoring.Label(canvas.transform,value,0,0,180,40,size,color,TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(0.5f,0.5f);
        label.rectTransform.anchoredPosition=Vector2.zero;
        label.gameObject.layer=parent.gameObject.layer;return label;
    }
}
