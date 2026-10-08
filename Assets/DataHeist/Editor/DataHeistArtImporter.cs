using UnityEditor;
using UnityEngine;

namespace DataHeist.EditorTools
{
    /// <summary>
    /// Forces correct pixel-art import settings for everything under
    /// Assets/DataHeist/Resources/DataHeist: Sprite, 32 PPU, point filter, no compression,
    /// full-rect meshes (needed for tiled rooftops) and folder-based pivots.
    /// </summary>
    public class DataHeistArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/DataHeist/Resources/DataHeist/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            string rel = assetPath.Substring(Root.Length);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = DH.PPU;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.isReadable = false;
            ti.wrapMode = rel.StartsWith("Parallax") || rel.StartsWith("Tiles") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = SpriteLibrary.PivotFor(rel);
            s.spriteExtrude = 0;
            s.spriteBorder = rel.StartsWith("UI/panel") ? new Vector4(12, 12, 12, 12) : Vector4.zero;
            ti.SetTextureSettings(s);
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ai = (AudioImporter)assetImporter;
            var settings = ai.defaultSampleSettings;
            if (assetPath.Contains("/Music/"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            ai.defaultSampleSettings = settings;
        }
    }
}
