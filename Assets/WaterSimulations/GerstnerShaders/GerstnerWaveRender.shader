Shader "Custom/Ocean Render Buffer"
{
    Properties
    {
        _Color ("Color", Color) = (0.0, 0.2, 0.4, 1.0)
        _AmbientColor ("Ambient Color", Color) = (0.1, 0.1, 0.1, 1.0)
        _DeepColor ("Deep Color", Color) = (0.0, 0.1, 0.25, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" 
            
            struct VertexData
            {
                float3 position;
                float3 normal;
            };

            // To jest bufor ustawiony w C# (outputBuffer)
            StructuredBuffer<VertexData> VertexBuffer; 

            struct appdata
            {
                uint vertexID : SV_VertexID; 
            };

            struct v2f
            {
                float3 normal : NORMAL;      
                float4 posWorld : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            fixed4 _Color;
            fixed4 _AmbientColor;
            fixed4 _DeepColor;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Odczyt zaktualizowanej pozycji i normalnej z bufora
                VertexData data = VertexBuffer[v.vertexID];
                
                float4 pos = float4(data.position, 1.0);
                
                o.posWorld = mul(unity_ObjectToWorld, pos);
                o.vertex = UnityObjectToClipPos(pos);
                
                // U¿ycie normalnej z bufora (obliczonej w Compute Shaderze)
                o.normal = UnityObjectToWorldNormal(data.normal);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Oœwietlenie
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diffuse = saturate(dot(i.normal, lightDir));
                
                // Mieszanie kolorów w zale¿noœci od wysokoœci (prosta symulacja g³êbi)
                float height = i.posWorld.y;
                fixed4 finalColor = lerp(_DeepColor, _Color, saturate(height * 0.5 + 0.5));
                
                // Koñcowy kolor
                fixed4 col = finalColor * (diffuse + _AmbientColor);
                
                return col;
            }
            ENDCG
        }
    }
}