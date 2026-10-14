using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Converts the built-in-pipeline (Standard shader) materials of the Abandoned Asylum pack
// to Universal Render Pipeline/Lit. Run via Tools > Asylum > Convert Materials To URP.
public static class AsylumURPConverter
{
    const string MaterialsFolder = "Assets/Abandoned_Asylum";

    [MenuItem("Tools/Asylum/Convert Materials To URP")]
    public static void Convert()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogError("URP Lit shader not found. Is the Universal RP package installed?");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        int converted = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == lit) continue;
                if (mat.shader.name != "Standard" && mat.shader.name != "Standard (Specular setup)" && mat.shader.name != "Hidden/InternalErrorShader") continue;

                ConvertMaterial(mat, lit);
                EditorUtility.SetDirty(mat);
                converted++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Asylum URP conversion: converted {converted} of {guids.Length} materials.");
    }

    static void ConvertMaterial(Material m, Shader lit)
    {
        // Read everything from the old Standard material before swapping the shader.
        Color color = Get(m, "_Color", Color.white);
        Texture mainTex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        Vector2 scale = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 offset = m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;
        Texture bump = Tex(m, "_BumpMap");
        float bumpScale = Get(m, "_BumpScale", 1f);
        Texture metalMap = Tex(m, "_MetallicGlossMap");
        Texture specMap = Tex(m, "_SpecGlossMap");
        Color specColor = Get(m, "_SpecColor", new Color(0.2f, 0.2f, 0.2f, 1f));
        float metallic = Get(m, "_Metallic", 0f);
        float glossiness = Get(m, "_Glossiness", 0.5f);
        float glossMapScale = Get(m, "_GlossMapScale", 1f);
        Texture occlusion = Tex(m, "_OcclusionMap");
        float occlusionStrength = Get(m, "_OcclusionStrength", 1f);
        Texture emissionMap = Tex(m, "_EmissionMap");
        Color emission = Get(m, "_EmissionColor", Color.black);
        float cutoff = Get(m, "_Cutoff", 0.5f);
        int mode = (int)Get(m, "_Mode", 0f);
        bool specularWorkflow = m.shader.name == "Standard (Specular setup)";
        bool smoothnessFromAlbedo = (int)Get(m, "_SmoothnessTextureChannel", 0f) == 1;

        m.shader = lit;
        foreach (string kw in m.shaderKeywords) m.DisableKeyword(kw);

        m.SetColor("_BaseColor", color);
        m.SetTexture("_BaseMap", mainTex);
        m.SetTextureScale("_BaseMap", scale);
        m.SetTextureOffset("_BaseMap", offset);

        m.SetFloat("_WorkflowMode", specularWorkflow ? 0f : 1f);
        if (specularWorkflow)
        {
            m.EnableKeyword("_SPECULAR_SETUP");
            m.SetColor("_SpecColor", specColor);
            m.SetTexture("_SpecGlossMap", specMap);
            if (specMap != null) m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetFloat("_Smoothness", glossiness);
        }
        else
        {
            m.SetFloat("_Metallic", metallic);
            m.SetTexture("_MetallicGlossMap", metalMap);
            if (metalMap != null) m.EnableKeyword("_METALLICSPECGLOSSMAP");
            // With a metallic map the smoothness slider acts as a multiplier in URP, like GlossMapScale in Standard.
            m.SetFloat("_Smoothness", metalMap != null ? glossMapScale : glossiness);
        }
        m.SetFloat("_SmoothnessTextureChannel", smoothnessFromAlbedo ? 1f : 0f);
        if (smoothnessFromAlbedo) m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");

        m.SetTexture("_BumpMap", bump);
        m.SetFloat("_BumpScale", bumpScale);
        if (bump != null) m.EnableKeyword("_NORMALMAP");

        m.SetTexture("_OcclusionMap", occlusion);
        m.SetFloat("_OcclusionStrength", occlusionStrength);
        if (occlusion != null) m.EnableKeyword("_OCCLUSIONMAP");

        m.SetTexture("_EmissionMap", emissionMap);
        m.SetColor("_EmissionColor", emission);
        if (emissionMap != null || emission.maxColorComponent > 0.001f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        m.SetFloat("_Cutoff", cutoff);

        switch (mode)
        {
            case 1: // Cutout
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_SrcBlend", 1f);
                m.SetFloat("_DstBlend", 0f);
                m.SetFloat("_ZWrite", 1f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = (int)RenderQueue.AlphaTest;
                break;
            case 2: // Fade / 3 Transparent
            case 3:
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_AlphaClip", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                break;
            default: // Opaque
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_AlphaClip", 0f);
                m.SetFloat("_SrcBlend", 1f);
                m.SetFloat("_DstBlend", 0f);
                m.SetFloat("_ZWrite", 1f);
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = -1;
                break;
        }
    }

    static float Get(Material m, string prop, float fallback) => m.HasProperty(prop) ? m.GetFloat(prop) : fallback;
    static Color Get(Material m, string prop, Color fallback) => m.HasProperty(prop) ? m.GetColor(prop) : fallback;
    static Texture Tex(Material m, string prop) => m.HasProperty(prop) ? m.GetTexture(prop) : null;
}
