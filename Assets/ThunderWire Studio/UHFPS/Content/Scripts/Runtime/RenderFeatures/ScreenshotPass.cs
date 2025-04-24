using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace UHFPS.Rendering
{
    public class ScreenshotPass : CustomPass
    {
        public Vector2Int OutputImageSize = new(640, 360);
        private RTHandle temp;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            temp = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                useDynamicScale: true, name: "ScreenshotRT"
            );
        }

        protected override void Execute(CustomPassContext ctx)
        {
            CoreUtils.SetRenderTarget(ctx.cmd, temp);
            Blitter.BlitCameraTexture(ctx.cmd, ctx.cameraColorBuffer, temp);
        }

        protected override void Cleanup()
        {
            RTHandles.Release(temp);
        }

        public Texture2D CaptureScreen()
        {
            RenderTexture.active = temp.rt;
            Texture2D texture2D = new(temp.rt.width, temp.rt.height, TextureFormat.RGBA32, false);
            texture2D.ReadPixels(new Rect(0, 0, temp.rt.width, temp.rt.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;

            return texture2D;
        }

        public IEnumerator CaptureScreenToFile(string outputPath)
        {
            RenderTexture.active = temp.rt;
            Texture2D texture2D = new(temp.rt.width, temp.rt.height, TextureFormat.RGBA32, false);
            texture2D.ReadPixels(new Rect(0, 0, temp.rt.width, temp.rt.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = null;
            byte[] bytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);
            yield return null;
        }
    }
}