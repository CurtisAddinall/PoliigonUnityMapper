using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;

if (args.Length != 1)
{
    Console.WriteLine("Please provide only the path to the Roughness and Metalic files.");
    return;
}

string RoughnessFilePath;
string MetalicFilePath;

//Get files in current working directory
var files = Directory.GetFiles(args[0], "*.jpg");//Ignore .tx files

RoughnessFilePath = files.Single(f => f.Contains("Roughness", StringComparison.CurrentCultureIgnoreCase));
MetalicFilePath = files.Single(f => f.Contains("Metal", StringComparison.CurrentCultureIgnoreCase));

using var rgbImage = await Image.LoadAsync<Rgb24>(MetalicFilePath);
using var alphaImage = await Image.LoadAsync<L8>(RoughnessFilePath);

if (rgbImage.Width != alphaImage.Width || rgbImage.Height != alphaImage.Height)
    throw new InvalidOperationException("RGB and Alpha images must be the same dimensions.");

int width = rgbImage.Width;
int height = rgbImage.Height;

using var outImage = new Image<Rgba32>(width, height);

for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        var rgb = rgbImage[x, y];
        // read greyscale (L8.PackedValue is 0–255)
        byte alpha = alphaImage[x, y].PackedValue;
        byte invertedAlpha = (byte)(255 - alpha); // Invert the alpha value - Unity wants Smoothness but Poliigon provides Roughness

        // write RGBA
        outImage[x, y] = new Rgba32(rgb.R, rgb.G, rgb.B, invertedAlpha);

    }
}

var outputPngPath = $"{args[0]}/MetalSmooth.png";
await outImage.SaveAsPngAsync(outputPngPath);

Console.WriteLine($"Saved {outputPngPath}");

return;