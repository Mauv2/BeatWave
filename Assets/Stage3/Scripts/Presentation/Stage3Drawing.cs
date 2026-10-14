using UnityEngine;
namespace BeatWave.Stage3
{
    public static class Stage3Drawing
    {
        public static SpriteRenderer Rect(Transform parent,string name,Vector2 pos,Vector2 size,Color color,int order,Sprite sprite)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=new Vector3(size.x,size.y,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
        }
        public static LineRenderer Line(Transform parent,string name,Vector3[] points,Color color,float width,int order,Material material,bool world=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=world;
            line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;line.sortingOrder=order;line.numCapVertices=4;line.numCornerVertices=3;return line;
        }
        public static LineRenderer Ring(Transform parent,string name,float radius,Color color,float width,int order,Material material)
        {
            var pts=new Vector3[49];for(int i=0;i<pts.Length;i++){float a=i*Mathf.PI*2/(pts.Length-1);pts[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius;}
            return Line(parent,name,pts,color,width,order,material);
        }
    }
}
