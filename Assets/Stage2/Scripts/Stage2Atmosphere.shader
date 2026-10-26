Shader "BeatWave/Stage2Atmosphere"
{
    Properties { _Tint ("Tint", Color) = (0.6,0.8,1,0.2) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            fixed4 _Tint;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float d=length((i.uv-.5)*2);
                float a=pow(saturate(1-d),2.5);
                return fixed4(_Tint.rgb,_Tint.a*a);
            }
            ENDCG
        }
    }
}
