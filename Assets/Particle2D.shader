Shader "Instanced/Particle2D" {
    Properties {
        
    }
    SubShader {
        Tags { "RenderType"="Transparemt" "Queue"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		
        Pass {
        	
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
			
            #include "UnityCG.cginc"
			
            struct v2f {
                float4 pos      : SV_POSITION;
				float2 uv       : TEXCOORD0;
                float3 col      : COLOR0;
            };
			
            StructuredBuffer<float2> _Positions;
            StructuredBuffer<int> _Types;
			//StructuredBuffer<byte> _Render;
            float scale;
			
			float3 ColorFromType(int type) {
				switch (type) {
					case  0: return float3(0.500f, 0.500f, 0.500f) * 0.9f;      // 0  neut		(middle gray)
					case  1: return float3(0.784f, 0.568f, 0.105f) * 0.9f;      // 1  sub+		(yellow)
		            case  2: return float3(0.086f, 0.352f, 0.345f) * 0.9f;      // 2  ribo		(dark turquoise)
		            case  3: return float3(0.074f, 0.682f, 0.662f) * 0.9f;      // 3  dpol		(turquoise)
		            case  4: return float3(0.411f, 0.000f, 0.411f) * 0.9f;      // 4  latc		(purple)
		            case  5: return float3(0.831f, 0.784f, 0.542f) * 0.9f;      // 5  cond		(beige)
		            case  6: return float3(0.490f, 0.070f, 0.039f) * 0.9f;      // 6  extr		(dark red)
		            case  7: return float3(0.788f, 0.788f, 0.788f) * 0.9f;      // 7  wall		(light gray)
		            case  8: return float3(0.066f, 0.564f, 0.066f) * 0.9f;      // 8  chlo		(green)
		            case  9: return float3(0.666f, 0.125f, 0.125f) * 0.9f;      // 9  +ion		(red)
		            case 10: return float3(0.858f, 0.694f, 0.360f) * 0.9f;      // 10 chan		(light yellow)
		            case 11: return float3(0.262f, 0.188f, 0.031f) * 0.9f;      // 11 sub-		(dark yellow)
		            case 12: return float3(0.125f, 0.125f, 0.666f) * 0.9f;      // 12 -ion		(blue)
					case 13: return float3(0.667f, 0.125f, 0.125f) * 0.9f;		// 13 post		(medium red)
					case 14: return float3(0.125f, 0.125f, 0.667f) * 0.9f;		// 14 negt		(medium blue)
		            case 15: return float3(0.431f, 0.800f, 0.792f) * 0.9f;      // 15 DNA		(light turquoise)
					default: return float3(0.500f, 0.500f, 0.500f) * 0.9f;      // 0  neut		(middle gray)
				}
			}
			
            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
			{
				float3 centreWorld = float3(_Positions[instanceID], 0);
				float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex * scale);
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

				v2f o;
				o.pos = UnityObjectToClipPos(objectVertPos);
				o.uv = v.texcoord;
				o.col = ColorFromType(_Types[instanceID]);

				return o;
			}

            fixed4 frag(v2f i) : SV_Target {
				float2 centreOffset = (i.uv.xy - 0.5) * 2;
				float sqrDst = dot(centreOffset, centreOffset);
				float alpha = 1 - smoothstep(0.9, 1, sqrDst);
				//if (_Render[instanceID] == 0) alpha = 0;
            	
				float3 col = i.col;
				return float4(col, alpha);
            }

            ENDCG
        }
    }
}