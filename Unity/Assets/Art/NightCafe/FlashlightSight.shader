Shader "NightCafe/Flashlight Sight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite texture", 2D) = "white" {}
        _Facing ("Facing", Vector) = (0,-1,0,0)
        _On ("Flashlight", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a { float4 position:POSITION; float2 uv:TEXCOORD0; };
            struct b { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Facing; float _On;
            b vert(a i) { b o; o.position=UnityObjectToClipPos(i.position); o.uv=i.uv; return o; }
            fixed4 frag(b i):SV_Target
            {
                float2 p=(i.uv-0.5)*float2(1.835,1);
                float d=length(p);
                float cone=smoothstep(0.82,0.95,dot(normalize(p+0.00001),normalize(_Facing.xy)));
                float reach=(1-smoothstep(0.35,0.68,d))*cone*_On;
                float nearLight=1-smoothstep(0.055,0.14,d);
                return fixed4(0.045,0.043,0.078,0.92*(1-max(nearLight,reach)));
            }
            ENDHLSL
        }
    }
}
