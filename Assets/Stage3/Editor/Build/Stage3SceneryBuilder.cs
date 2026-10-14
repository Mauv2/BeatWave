#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace BeatWave.Stage3
{
    public static class Stage3SceneryBuilder
    {
        static Sprite square;
        static Material material;
        static readonly Color Gold=new Color(.82f,.66f,.35f,.8f);
        static Transform Child(Transform parent,string name,Vector3 position)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
        }
        static SpriteRenderer Rect(Transform p,string n,Vector2 at,Vector2 size,Color c,int order=-10)=>Stage3Drawing.Rect(p,n,at,size,c,order,square);
        static LineRenderer Line(Transform p,string n,Vector3[] v,Color c,float w=.04f,int order=-9)=>Stage3Drawing.Line(p,n,v,c,w,order,material);
        static void Ring(Transform p,string n,float radius,Color c,float width=.06f)=>Stage3Drawing.Ring(p,n,radius,c,width,-12,material);
        static Vector3 At(int beat)=>Stage3Route.Position(Stage3Route.Beat(beat));
        public static void Configure(Sprite white,Material lines){square=white;material=lines;}
        public static void Platform(Transform parent,int act,float start,float width)
        {
            var p=Child(parent,"Act "+act+" stage tile",new Vector3(start,0));
            if(act==1)
            {
                Rect(p,"Brass rail",new Vector2(width*.5f,-.12f),new Vector2(width+.05f,.24f),new Color(.48f,.3f,.14f),8);
                Line(p,"Rail highlight",new[]{Vector3.zero,Vector3.right*width},Gold,.045f,9);
                for(float x=.5f;x<width;x+=1.2f)Line(p,"Clockwork lattice",new[]{new Vector3(x,-.15f),new Vector3(x+.5f,-1),new Vector3(x+1,-.15f)},Gold,.06f,7);
            }
            else if(act==3)
            {
                for(float x=0;x<width;x+=2.4f)
                {
                    float w=Mathf.Min(2.35f,width-x);
                    Rect(p,"Floating book cover",new Vector2(x+w*.5f,-.3f),new Vector2(w,.55f),new Color(.22f,.1f,.28f),8);
                    for(int j=0;j<3;j++)Line(p,"Ivory page edge",new[]{new Vector3(x,-.08f-j*.1f),new Vector3(x+w,-.08f-j*.1f)},new Color(.79f,.69f,.84f),.035f,9);
                }
            }
            else if(act==4)
            {
                Line(p,"Constellation thread",new[]{Vector3.zero,Vector3.right*width},new Color(.65f,.8f,1,.8f),.065f,9);
                for(float x=0;x<width;x+=1.5f)
                {
                    var star=Rect(p,"Path star",new Vector2(x,0),Vector2.one*.18f,new Color(.92f,.8f,.54f),10);star.transform.localRotation=Quaternion.Euler(0,0,45);
                    Line(p,"Hanging constellation",new[]{new Vector3(x,0),new Vector3(x+.5f,-1.5f),new Vector3(x+1.5f,0)},new Color(.4f,.6f,.9f,.35f),.02f,7);
                }
            }
            else
            {
                Rect(p,"Translucent sky glass",new Vector2(width*.5f,-.18f),new Vector2(width+.08f,.36f),new Color(.63f,.85f,.94f,.55f),8);
                Line(p,"Ivory rim",new[]{Vector3.zero,Vector3.right*width},new Color(.95f,.83f,.57f),.055f,10);
                for(float x=0;x<width;x+=2)Line(p,"Glass facets",new[]{new Vector3(x,0),new Vector3(x+1,-1.6f),new Vector3(Mathf.Min(width,x+2),0)},new Color(.73f,.91f,1,.45f),.04f,8);
            }
        }
        public static void Build(Stage3Gameplay game,Sprite white,Material lines,Sprite clock,Sprite sky)
        {
            Configure(white,lines);
            var scene=game.gameObject.AddComponent<Stage3Scenography>();scene.game=game;game.scenery=scene;
            SpriteRenderer Background(string name,Sprite sprite)
            {
                var b=Child(game.transform,name,Vector3.zero).gameObject.AddComponent<SpriteRenderer>();b.sprite=sprite;b.sortingOrder=-100;b.color=Color.clear;return b;
            }
            scene.backdrops=new[]{game.backdrop,Background("Clockwork painted stage",clock),game.transitions.depths,Background("Paper sky painted stage",sky)};
            string[] names={"01 Moonlit prologue","02 Clockwork theater","03 Flooded archive","04 Mirror switchback","05 Eclipse constellation","06 Sky ribbons","07 Curtain call"};
            scene.actSets=new Transform[names.Length];
            for(int i=0;i<names.Length;i++)scene.actSets[i]=Child(game.transform,names[i],Vector3.zero);

            foreach(int beat in new[]{8,26,42})
            {
                var arch=Child(scene.actSets[0],"Broken proscenium",At(beat));
                Line(arch,"Moon arch",new[]{new Vector3(-3,0),new Vector3(-3,7),new Vector3(-1,9),new Vector3(1,9),new Vector3(3,7)},new Color(.38f,.47f,.66f,.5f),.16f);
                Line(arch,"Hanging thread",new[]{new Vector3(1,9),new Vector3(1,4)},Gold,.025f);
                Ring(Child(arch,"Moon charm",new Vector3(1,3.6f)),"Moon",.4f,Gold,.035f);
            }
            var gears=new List<Transform>();
            for(int i=0;i<5;i++)
            {
                var gear=Child(scene.actSets[1],"Turning stage cog "+i,At(56+i*10)+new Vector3(0,i%2==0?6:-4));
                float radius=2.5f+i%3;
                Ring(gear,"Engraved rim",radius,Gold,.22f);Ring(gear,"Inner rim",radius*.75f,Gold);
                for(int j=0;j<16;j++)
                {
                    float a=j*Mathf.PI/8;var tooth=Rect(gear,"Tooth",new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(.48f,.65f),Gold,-13);
                    tooth.transform.localRotation=Quaternion.Euler(0,0,j*22.5f);
                }
                for(int j=0;j<6;j++){float a=j*Mathf.PI/3;Line(gear,"Spoke",new[]{Vector3.zero,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius*.7f},Gold,.12f,-14);}
                gears.Add(gear);
            }
            scene.gears=gears.ToArray();
            scene.pendulum=Child(scene.actSets[1],"Great pendulum pivot",At(84)+Vector3.up*15);
            Line(scene.pendulum,"Pendulum chain",new[]{Vector3.zero,Vector3.down*11},Gold,.12f);
            var bob=Child(scene.pendulum,"Pendulum medallion",Vector3.down*11);Ring(bob,"Outer",2,Gold,.2f);Ring(bob,"Inner",1.6f,Gold);

            scene.water=Child(scene.actSets[2],"Reflecting flood",Vector3.zero);
            Rect(scene.water,"Water veil",new Vector2(0,-5),new Vector2(60,10),new Color(.08f,.38f,.52f,.28f),-7);
            for(int i=0;i<6;i++)Line(scene.water,"Reflection",new[]{new Vector3(-28,-i*.9f),new Vector3(28,-i*.9f)},new Color(.35f,.83f,.95f,.16f),.035f,-6);
            foreach(int beat in new[]{128,140,150})
            {
                var pier=Child(scene.actSets[2],"Drowned shelf",At(beat));
                for(int j=0;j<5;j++)Rect(pier,"Shelf shadow",new Vector2(j*.8f-2,-3),new Vector2(.32f,6),new Color(.05f,.13f,.22f,.7f),-8);
                Line(pier,"Water arch",new[]{new Vector3(-3,-8),new Vector3(-3,7),new Vector3(3,7),new Vector3(3,-8)},new Color(.25f,.62f,.77f,.4f),.09f);
            }
            // A real moving stage supports the steep ascent instead of another seesaw.
            scene.lift=Child(game.transform,"Archive counterweight lift",At(152));
            Rect(scene.lift,"Lift deck",new Vector2(0,-.18f),new Vector2(5,.36f),new Color(.13f,.25f,.35f),8);
            Line(scene.lift,"Deck rim",new[]{new Vector3(-2.5f,0),new Vector3(2.5f,0)},new Color(.4f,.9f,1),.06f,10);
            for(int side=-1;side<=1;side+=2)Line(scene.lift,"Lift suspension",new[]{new Vector3(side*2,-.1f),new Vector3(side*2,11)},Gold,.035f,7);
            var collider=scene.lift.gameObject.AddComponent<BoxCollider2D>();collider.size=new Vector2(5,.3f);collider.offset=Vector2.down*.15f;

            foreach(int beat in new[]{184,216})
            {
                var turn=Child(scene.actSets[3],"Mirror turn beacon",At(beat)+Vector3.up*4);
                float direction=beat==184?-1:1;
                for(int i=0;i<3;i++)Line(turn,"Direction chevron",new[]{new Vector3(direction*(i-1),-1),new Vector3(direction*(i-1)+direction*.7f,0),new Vector3(direction*(i-1),1)},new Color(.76f,.61f,1,.8f),.09f);
                for(int j=0;j<5;j++)
                {
                    var frame=Child(turn,"Floating mirror",new Vector3(j*4-8,7+(j%2)*3));
                    Line(frame,"Diamond frame",new[]{new Vector3(0,-3),new Vector3(-1.5f,0),new Vector3(0,3),new Vector3(1.5f,0),new Vector3(0,-3)},new Color(.74f,.57f,.85f,.4f),.055f);
                }
            }
            scene.eclipse=Child(scene.actSets[4],"Eclipsed mechanical moon",At(266)+new Vector3(8,8));
            Ring(scene.eclipse,"Eclipse rim",5,new Color(.7f,.8f,1,.65f),.12f);Ring(scene.eclipse,"Broken halo",6,Gold,.035f);
            for(int i=0;i<20;i++)
            {
                float a=i*Mathf.PI/10;
                Line(scene.eclipse,"Radial ray",new[]{new Vector3(Mathf.Cos(a),Mathf.Sin(a))*6.4f,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*(7+i%3)},Gold,.025f);
            }
            var doorway=Child(scene.actSets[4],"Opening astral doors",At(296));
            scene.liftDoors=new Transform[2];
            for(int i=0;i<2;i++)
            {
                var door=Child(doorway,"Split star door",new Vector3(i==0?-1.7f:1.7f,2.5f));scene.liftDoors[i]=door;
                Line(door,"Door frame",new[]{new Vector3(-1.5f,-2.5f),new Vector3(-1.5f,6),new Vector3(0,8),new Vector3(1.5f,6),new Vector3(1.5f,-2.5f)},Gold,.14f);
                for(int j=0;j<5;j++)Line(door,"Door thread",new[]{new Vector3(j*.55f-1.1f,-2),new Vector3(j*.55f-1.1f,5)},new Color(.5f,.75f,1,.6f),.025f);
            }
            foreach(int beat in new[]{320,336,352,364})
            {
                var sail=Child(scene.actSets[5],"Suspended sky sail",At(beat));
                for(int j=0;j<4;j++)Line(sail,"Ribbon rib",new[]{new Vector3(-7,-2-j*.7f),new Vector3(0,-5-j),new Vector3(7,-2-j*.7f)},new Color(1,.78f,.49f,.4f),.045f);
                Line(sail,"High suspension",new[]{new Vector3(-5,0),new Vector3(-5,16)},Gold,.035f);
            }
            var finale=Child(game.transform,"Unfolding final stage",At(368)+Vector3.up*3);
            scene.finalWings=new Transform[2];
            for(int i=0;i<2;i++)
            {
                float side=i==0?-1:1;var wing=Child(finale,"Wish fan",Vector3.zero);scene.finalWings[i]=wing;
                for(int j=0;j<7;j++)Line(wing,"Fan feather",new[]{Vector3.zero,new Vector3(side*(6+j*.8f),3-j*.8f),new Vector3(side*3,-1)},new Color(1,.83f,.54f,.65f),.06f);
            }
            var slips=new List<Transform>();
            for(int i=0;i<36;i++)slips.Add(Rect(game.transform,"Drifting wish slip",Vector2.zero,new Vector2(.08f+(i%4)*.035f,.25f+(i%3)*.12f),new Color(1,.85f,.64f,.4f),-5).transform);
            scene.paperSlips=slips.ToArray();
        }
    }
}
#endif
