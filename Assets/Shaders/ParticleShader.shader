Shader "Custom/ParticleShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                //float speed : TEXCOORD0;
                float3 randColor : COLOR0;
            };

            struct Particle {
                float2 position;
                float2 velocity;
            };

            StructuredBuffer<Particle> _Particles;
            
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                Particle p = _Particles[instanceID];

                float2 worldPos = p.position + v.vertex.xy;
                o.pos = UnityObjectToClipPos(float4(worldPos, 0, 1.0));

                //o.speed = length(p.velocity); // Pass speed to fragment shader

                float r = rand(instanceID + 5u);
                float g = rand(instanceID + 18u);
                float b = rand(instanceID + 23u);
                o.randColor = float3(r, g, b);

                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                // // Normalize speed to a [0,1] range
                // float maxSpeed = 5.0; // tweak based on what you observe
                // float t = saturate(i.speed / maxSpeed);

                // // Map speed to color: slow = blue, fast = red
                // float3 color = lerp(float3(0.2, 0.4, 1.0), float3(1.0, 0.2, 0.2), t);

                return fixed4(i.randColor, 1.0);
            }

            ENDCG
        }
    }
}