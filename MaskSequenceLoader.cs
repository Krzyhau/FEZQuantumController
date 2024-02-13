using Common;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QuantumController
{
    internal class MaskSequenceLoader
    {
        private readonly string path;

        private int width;
        private int height;
        private bool[,] mask;

        private int currentFrame;

        public MaskSequenceLoader(string path)
        {
            this.path = path;
            currentFrame = 0;
            LoadMask();
        }

        public void AdvanceFrame()
        {
            currentFrame++;
            LoadMask();
        }

        private void LoadMask()
        {
            var fileName = currentFrame.ToString("D4") + ".png";
            var filePath = Path.Combine(path, fileName);

            Logger.Log("QuantumController", filePath);

            if(!File.Exists(filePath))
            {
                currentFrame = 0;
                return;
            }

            using var image = Image.Load<Rgba32>(filePath);

            mask = new bool[image.Width, image.Height];

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        ref Rgba32 pixel = ref pixelRow[x];
                        mask[x, y] = (pixel.R > 64);
                    }
                }
            });

            width = image.Width;
            height = image.Height;
        }

        public bool IsMasked(Vector2 pos)
        {
            if (mask == null) return false;

            var pixelX = Math.Min(Math.Max((int)(pos.X * width), 0), width-1);
            var pixelY = Math.Min(Math.Max((int)(pos.Y * height), 0), height-1);

            return mask[pixelX, pixelY];
        }
    }
}
