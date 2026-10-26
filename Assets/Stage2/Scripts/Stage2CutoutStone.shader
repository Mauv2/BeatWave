Shader "BeatWave/Stage2CutoutStone"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D)="white"{}
        _Color ("Tint",Color)=(1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap",Float)=0
        [HideInInspector] _RendererColor ("RendererColor",Color)=(1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector)=(1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha",2D)="white"{}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 frag(v2f IN):SV_Target
            {
                fixed4 source=SampleSpriteTexture(IN.texcoord);
                // The supplied cutouts contain translucent rectangular remnants. Keep their
                // opaque stone, while removing that matte without changing the source PNGs.
                source.a=smoothstep(.76,.96,source.a);
                fixed4 c=source*IN.color;c.rgb*=c.a;return c;
            }
            ENDCG
        }
    }
}
