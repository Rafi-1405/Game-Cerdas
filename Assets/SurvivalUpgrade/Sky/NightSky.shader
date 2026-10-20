Shader "ZombieUnity/Night Sky"
{
    Properties
    {
        _Zenith("Zenith",Color)=(0.004,0.008,0.022,1)
        _Horizon("Horizon",Color)=(0.024,0.032,0.052,1)
        _MoonDirection("Moon direction",Vector)=(0.35,0.7,0.5,0)
        _Exposure("Exposure",Range(0,3))=1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;};
            struct v2f {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
            float4 _Zenith,_Horizon,_MoonDirection;float _Exposure;
            v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            float4 frag(v2f i):SV_Target{
                float3 d=normalize(i.direction);float y=saturate(d.y);
                float3 sky=lerp(_Horizon.rgb,_Zenith.rgb,pow(y,.48));
                float2 uv=float2(atan2(d.x,d.z),asin(d.y));
                float2 st=uv*float2(165,200);float2 cell=floor(st);float n=hash(cell);float2 center=float2(hash(cell+3.7),hash(cell+8.2));
                float star=(1-smoothstep(.012,.075,length(frac(st)-center)))*step(.996,n)*smoothstep(.02,.2,y);
                sky+=star*lerp(.16,.6,hash(cell+9.5))*float3(.76,.85,1);
                float3 moon=normalize(_MoonDirection.xyz);float alignment=dot(d,moon);
                float disc=smoothstep(.99989,.99992,alignment);
                float glow=pow(saturate(alignment),600)*.025;
                float craters=noise(uv*650)*.17+noise(uv*1300)*.08;
                sky+=float3(.62,.72,.85)*(disc*(.65+craters)+glow);
                float cloud=noise(uv*float2(5,12))*.6+noise(uv*float2(13,28))*.25+noise(uv*float2(30,55))*.15;
                float mask=smoothstep(.48,.72,cloud)*smoothstep(-.05,.12,d.y)*.65;
                sky=lerp(sky,float3(.012,.017,.028),mask);
                return float4(sky*_Exposure,1);
            }
            ENDHLSL
        }
    }
}
