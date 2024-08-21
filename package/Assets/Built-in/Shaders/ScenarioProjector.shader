// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'
// Upgrade NOTE: replaced '_ProjectorClip' with 'unity_ProjectorClip'

Shader"Scenario/Projector" {
    Properties {      
        _Color ("Main Color", Color) = (1,1,1,1)
        _Decal ("Cookie", 2D) = "" {}    
        _Slider ("Slider", Range(0,1)) = 0
        _ScaleFlat ("Scale Flat", Range(0,10)) = 2
        _OffsetXFlat ("Offset X Flat", Range(-10,10)) = 0
        _OffsetYFlat ("Offset Y Flat", Range(-10,10)) = 0
        _Angle ("Angle", Float) = 0
        [Toggle(UV2)] _UV2 ("Use Second UV Map?", Float) = 0
        _UVSelector ("UV Selector", Range(1,5))= 1
        _Vector ("Vector", Vector) = (0,0,0,1)
        _Offset ("Offset", Vector) = (0,0,0,1)
    }
 
    Subshader {
        Tags {"RenderType"="Opaque"}
        Pass {
            /*ZWrite Off
            Cull Front
            ColorMask RGBA
            Blend SrcAlpha OneMinusSrcAlpha*/
            Offset -1, -1
 
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag  
            #pragma shader_feature UV2
            #include "UnityCG.cginc"
         
            struct MeshData{
                float4 vertex : POSITION; // Vertex position
                float3 normal : NORMAL; // normal position
                float4 tangent : TANGENT;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0; // uv0 for diffuse / normal map textures
                float2 uv1 : TEXCOORD1; // uv1 coordinates lightmap
                float2 uv2 : TEXCOORD2;
            };

            struct v2f {
                float4 position : SV_POSITION;
                float4 vertexUV : TEXCOORD0;
                float4 uvShadow : TEXCOORD1;
                float4 normalView: TEXCOORD2;
                fixed4 worldPos : TEXCOORD10;
                float4 uvShadow2: TEXCOORD3;
                float4 uvShadow3: TEXCOORD4;
            };
         
            float4x4 unity_Projector;
            float4x4 unity_ProjectorClip;
            float _Slider;
            float4 _Color;

            int _UVSelector;
            float _ScaleFlat;
            float _OffsetXFlat;
            float _OffsetYFlat;
            float _Angle;

            float4 _Vector;
            float4 _Offset;

            float4 _UvShadow;

            sampler2D _Decal;

            v2f vert (MeshData v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                float x = v.vertex.x;
                float y = v.vertex.y;
                
                #if UV2
                x = v.uv2.x;
                y = v.uv2.y;              
                #endif
                x= x * _ScaleFlat - _OffsetXFlat;            
                y= y * -_ScaleFlat + _OffsetYFlat;
                
                o.vertexUV = float4(x, y, 0.1, 1);
                o.normalView = UnityObjectToClipPos(v.vertex);
                o.position = lerp(o.vertexUV, o.normalView, _Slider);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);

                if(_UVSelector == 1)
                {
                     o.uvShadow = mul(unity_Projector, (v.vertex + _Offset) * _Vector );
                }
                else if(_UVSelector == 2)
                {
                    o.uvShadow2 = mul(unity_Projector, (v.vertex + _Offset) * _Vector);
                }
                else
                {
                    o.uvShadow3 = mul(unity_Projector, (v.vertex + _Offset) * _Vector);
                }

                return o;
            }  
         
            fixed4 frag (v2f i) : SV_Target
            {
                //return i.uvShadow;
                if(_UVSelector == 1)
                {
                    fixed4 texS = tex2Dproj (_Decal, i.uvShadow) * _Color ;
                    return texS;
                }
                else if(_UVSelector == 2)
                {
                    fixed4 texS = tex2Dproj (_Decal, i.uvShadow2 + _Angle);
                    return texS;
                }
                else
                {
                    fixed4 texS = tex2Dproj (_Decal, i.uvShadow3);
                    return texS;
                }

            }
            ENDCG
        }
    }
}