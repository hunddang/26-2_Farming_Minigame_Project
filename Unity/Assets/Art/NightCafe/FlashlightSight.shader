Shader "NightCafe/Flashlight Sight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite texture", 2D) = "white" {}
        _Facing ("Facing", Vector) = (0,-1,0,0)
        _On ("Flashlight", Float) = 0
        _DoorLamp ("Security Door Lamp", Vector) = (0,0,0,0)
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
            float4 _Facing, _DoorLamp;
            float _On;
            b vert(a i) { b o; o.position=UnityObjectToClipPos(i.position); o.uv=i.uv; return o; }
            fixed4 frag(b i):SV_Target
            {
                float2 scale=float2(1920.0,1080.0)/810.0;
                float2 p=(i.uv-0.5)*scale;
                float d=length(p);
                float cone=smoothstep(0.82,0.95,dot(normalize(p+0.00001),normalize(_Facing.xy)));
                float reach=(1-smoothstep(0.35,0.68,d))*cone;
                float nearLight=1-smoothstep(0.055,0.14,d);
                float flashlight=max(nearLight,reach)*_On;
                float lampDistance=length((i.uv-_DoorLamp.xy)*scale);
                float lamp=(1-smoothstep(0.016,0.115,lampDistance))*_DoorLamp.z;
                float3 glow=float3(0.46,0.025,0.018)*lamp*(1-flashlight);
                return fixed4(glow,1-max(flashlight,lamp*0.62));
            }
            ENDHLSL
        }
    }
}
