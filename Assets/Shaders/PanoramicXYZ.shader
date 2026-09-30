Shader "Skybox/PanoramicXYZ"
{
    Properties
    {
        _MainTex ("Video (Render Texture)", 2D) = "black" {}
        _Rotation ("Rotation XYZ (grader)", Vector) = (0, 0, 0, 0)
        _Exposure ("Exposure", Range(0, 8)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Rotation;
            half _Exposure;

            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            float3x3 RotX(float a) { float s = sin(a), c = cos(a); return float3x3(1,0,0, 0,c,-s, 0,s,c); }
            float3x3 RotY(float a) { float s = sin(a), c = cos(a); return float3x3(c,0,s, 0,1,0, -s,0,c); }
            float3x3 RotZ(float a) { float s = sin(a), c = cos(a); return float3x3(c,-s,0, s,c,0, 0,0,1); }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Samme rækkefølge som Unitys Transform (Z, så X, så Y)
                float3 r = radians(_Rotation.xyz);
                float3x3 m = mul(RotY(r.y), mul(RotX(r.x), RotZ(r.z)));

                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = mul(transpose(m), v.vertex.xyz); // invers rotation = drej himlen
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float lat = acos(d.y);
                float lon = atan2(d.z, d.x);
                float2 uv = float2(0.5, 1.0) - float2(lon * 0.5 / UNITY_PI, lat / UNITY_PI);

                fixed4 c = tex2Dlod(_MainTex, float4(uv, 0, 0)); // lod 0 undgår søm-linje
                c.rgb *= _Exposure;
                return c;
            }
            ENDCG
        }
    }
}