Shader "Hidden/PostProcess/SDFCubeMask"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"

    TEXTURE2D_X(_InputTexture);

    float3 _CubeCenter;
    float3 _CubeSize;
    float _EdgeFade;
    float4 _OutsideColor;

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // SDF for a box centered at origin
    float sdBox(float3 p, float3 b)
    {
        float3 q = abs(p) - b;
        return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
    }

    float4 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        
        float2 uv = input.texcoord;
        float4 sceneColor = LOAD_TEXTURE2D_X(_InputTexture, input.positionCS.xy);
        
        // Sample depth and reconstruct world position
        float depth = LOAD_TEXTURE2D_X(_CameraDepthTexture, input.positionCS.xy).x;
        
        // Handle sky (depth = 0 in reversed-Z)
        #if UNITY_REVERSED_Z
        bool isSky = depth == 0.0;
        #else
        bool isSky = depth == 1.0;
        #endif
        
        if (isSky)
        {
            return _OutsideColor;
        }
        
        // Reconstruct world position from depth
        float2 positionNDC = uv * 2.0 - 1.0;
        float4 positionCS = float4(positionNDC.x, positionNDC.y, depth, 1.0);
        
        #if UNITY_UV_STARTS_AT_TOP
        positionCS.y = -positionCS.y;
        #endif
        
        float4 positionWS = mul(UNITY_MATRIX_I_VP, positionCS);
        positionWS /= positionWS.w;
        
        // Calculate SDF distance from cube
        float3 localPos = positionWS.xyz - _CubeCenter;
        float dist = sdBox(localPos, _CubeSize * 0.5);
        
        // Create mask: inside cube = 1, outside = 0, with smooth fade
        float mask = 1.0 - saturate(dist / max(_EdgeFade, 0.001));
        
        // Blend between scene color (inside) and outside color
        return lerp(_OutsideColor, sceneColor, mask);
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        
        Pass
        {
            Name "SDF Cube Mask"
            
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
    
    Fallback Off
}
