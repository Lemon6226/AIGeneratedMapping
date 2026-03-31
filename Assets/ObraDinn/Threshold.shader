Shader "Effects/Threshold"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BG ("Background", Color) = (0,0,0,0)
        _FG ("Foreground", Color) = (1,1,1,1)
        _ThresholdOffset ("Threshold Offset", Range(-0.5, 0.5)) = 0
        _ColorBlend ("Color Blend", Range(0, 1)) = 1
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            float4 _BG;
            float4 _FG;
            float _ThresholdOffset;
            float _ColorBlend;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float threshold = 0.5 + _ThresholdOffset;
                
                // Применяем пороговое значение с учетом смещения
                float value = round(col.r - threshold + 0.5);
                
                // Смешиваем цвета с учетом ColorBlend
                float3 finalColor = lerp(col.rgb, lerp(_BG.rgb, _FG.rgb, value), _ColorBlend);
                
                return fixed4(finalColor, 1);
            }
            ENDCG
        }
    }
}
