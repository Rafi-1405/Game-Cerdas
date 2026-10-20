Shader "ZombieUnity/Foliage"
{
    Properties
    {
        _Color("Tint",Color)=(1,1,1,1)
        _MainTex("Albedo",2D)="white"{}
        _Cutoff("Alpha cutoff",Range(0,1))=.35
        _BumpMap("Normal map",2D)="bump"{}
        _BumpScale("Normal strength",Range(0,2))=1
        _Glossiness("Smoothness",Range(0,1))=.08
        _Metallic("Metallic",Range(0,1))=0
        _RobloxOverlay("Roblox overlay alpha",Float)=0
        _EmissionMap("Emission",2D)="black"{}
        _EmissionColor("Emission tint",Color)=(0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow
        #pragma target 3.0
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _EmissionMap;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _RobloxOverlay;
        half _Glossiness,_Metallic,_BumpScale;
        struct Input { float2 uv_MainTex;float2 uv_BumpMap; };
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;
            o.Albedo=c.rgb;o.Alpha=c.a;o.Metallic=_Metallic;o.Smoothness=_Glossiness;
            if(_RobloxOverlay>.5){fixed4 t=tex2D(_MainTex,IN.uv_MainTex);o.Albedo=lerp(_Color.rgb,c.rgb,t.a);o.Alpha=_Color.a;}
            o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
            o.Emission=tex2D(_EmissionMap,IN.uv_MainTex).rgb*_EmissionColor.rgb;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}
