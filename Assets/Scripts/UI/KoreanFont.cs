using UnityEngine;

namespace QuantumCrossy.UI
{
    public static class KoreanFont
    {
        const string ResourcePath = "Fonts/GothicA1-Regular";

        public static Font Load()
        {
            Font font = Resources.Load<Font>(ResourcePath);
            if (font != null)
                return font;

            try
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic" },
                    64);
            }
            catch (System.Exception)
            {
                font = null;
            }

            if (font != null)
                return font;

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static void Warmup(Font font, string characters, int size)
        {
            if (font == null || string.IsNullOrEmpty(characters))
                return;
            font.RequestCharactersInTexture(characters, size, FontStyle.Normal);
            font.RequestCharactersInTexture(characters, size, FontStyle.Bold);
        }
    }
}
