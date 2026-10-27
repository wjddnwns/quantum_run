using UnityEngine;

namespace QuantumCrossy.Visuals
{
    /// <summary>
    /// URP Lit 머티리얼을 런타임에 복제한다.
    /// Resources 원본이 프로젝트를 참조하고 있어서 빌드에서도 셰이더가 빠지지 않는다.
    /// </summary>
    public static class PrimitiveMaterials
    {
        const string TemplatePath = "Materials/URPLitEmissive";
        static Material template;

        public static Material Create(Color color, float emission)
        {
            Material source = Template();
            Material mat = source != null ? new Material(source) : new Material(FallbackShader());
            Apply(mat, color, emission);
            return mat;
        }

        public static void Apply(Material mat, Color color, float emission)
        {
            if (mat == null)
                return;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * Mathf.Max(0f, emission));
            }

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.35f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
        }

        static Material Template()
        {
            if (template == null)
                template = Resources.Load<Material>(TemplatePath);
            return template;
        }

        static Shader FallbackShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            return shader;
        }
    }
}
