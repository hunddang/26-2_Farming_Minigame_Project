Shader "NightCafe/CCTV Screen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Live feed", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex; fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 rgb = tex2D(_MainTex, i.uv).rgb;
                float luminance = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                rgb = lerp(rgb, luminance * float3(0.82, 1.04, 0.94), 0.42);
                float scan = 0.96 + 0.04 * sin(i.uv.y * 720 * 3.14159);
                float2 edge = i.uv * (1 - i.uv);
                float vignette = 0.78 + 0.22 * saturate(pow(edge.x * edge.y * 16, 0.3));
                float noise = frac(sin(dot(floor(i.uv * float2(1280,720)), float2(12.9898,78.233)) + floor(_Time.y * 15)) * 43758.5453);
                return fixed4((rgb * scan * vignette + (noise - 0.5) * 0.006) * i.color.rgb, i.color.a);
            }
            ENDHLSL
        }
    }
}
