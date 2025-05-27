using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.CommandLine;

namespace PoliigonUnityMapper
{
    public class PoliiMapper
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var convertAllCommand = new Command("all", "Convert Roughness and matallic maps in the specified directory to a combined MetallicSmoothness map, and also inverts the green channel for normal maps.");
            var convertMetalCommand = new Command("metal", "Provide a path to the metallic and roughness maps that you want converted to a combined MetallicSmoothness map");
            var convertNormalCommand = new Command("normal", "Provide a path to the normal map that you want to invert the green channel for.");

            var dirOption = new Option<string>(
                "--dir",
                description: "Path to the directory containing Roughness, Metalic and Normal maps.",
                getDefaultValue: () => Directory.GetCurrentDirectory()
            );

            var normalPathOption = new Option<string>(
                "--normal",
                description: "Path to the Normal map file."
            );

            var roughnessPathOption = new Option<string>(
                "--roughness",
                description: "Path to the Roughness map file."
            );
            var metallicPathOption = new Option<string>(
                "--metallic",
                description: "Path to the Metallic map file."
            );
            var outputOption = new Option<string>(
                "--output",
                description: "Path to the output directory where the converted files will be saved.",
                getDefaultValue: () => Directory.GetCurrentDirectory()
            );


            rootCommand.AddGlobalOption(outputOption);

            rootCommand.AddCommand(convertAllCommand);
            rootCommand.AddCommand(convertMetalCommand);
            rootCommand.AddCommand(convertNormalCommand);

            convertAllCommand.AddOption(dirOption);

            convertMetalCommand.AddOption(roughnessPathOption);
            convertMetalCommand.AddOption(metallicPathOption);

            convertNormalCommand.AddOption(normalPathOption);


            convertMetalCommand.SetHandler(
                (metalPath, roughnessPath, outputDirectory) =>
                {
                    if (string.IsNullOrWhiteSpace(metalPath) || string.IsNullOrWhiteSpace(roughnessPath))
                    {
                        Console.WriteLine("Metallic (--metallic) and roughness (--roughness) paths are required");
                        return;
                    }
                    MetallicConversion(metalPath, roughnessPath, outputDirectory);
                }, metallicPathOption, roughnessPathOption, outputOption);

            convertNormalCommand.SetHandler(
                (normalPath, outputDirectory) =>
                {
                    if (string.IsNullOrWhiteSpace(normalPath))
                    {
                        Console.WriteLine("Normal (--normal) path is required");
                        return;
                    }
                    InvertNormalGreenChannel(normalPath, outputDirectory);
                }, normalPathOption, outputOption);

            convertAllCommand.SetHandler(
                (directoryPath, outputDirectory) =>
                {
                    if (string.IsNullOrWhiteSpace(directoryPath))
                    {
                        Console.WriteLine("Directory (--dir) path is required");
                        return;
                    }
                    ConvertAllFilesInDir(directoryPath, outputDirectory);
                }, dirOption, outputOption);

            return await rootCommand.InvokeAsync(args);
        }


        internal static async void ConvertAllFilesInDir(string directoryPath, string outputDirectory)
        {

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory {directoryPath} does not exist.");
                return;
            }
            var files = Directory.GetFiles(directoryPath);
            string normalPath = null;
            string metallicPath = null;
            string roughnessPath = null;
            foreach (var file in files)
            {
                if (file.EndsWith("Normal.png", StringComparison.OrdinalIgnoreCase) || file.EndsWith("_Normal.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    normalPath = file;
                }
                else if (file.EndsWith("Metallic.png", StringComparison.OrdinalIgnoreCase) || file.EndsWith("_Metallic.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    metallicPath = file;
                }
                else if (file.EndsWith("Roughness.png", StringComparison.OrdinalIgnoreCase) || file.EndsWith("_Roughness.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    roughnessPath = file;
                }
            }
            if (normalPath != null)
            {
                InvertNormalGreenChannel(normalPath, outputDirectory);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No normal map found in the directory. Skipping normal map conversion.");
                Console.ResetColor();
            }
            if (metallicPath != null && roughnessPath != null)
            {
                MetallicConversion(metallicPath, roughnessPath, outputDirectory);
            }
            else
            {

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No metallic or roughness map found in the directory. Skipping metallic conversion.");
                Console.ResetColor();
            }

        }

        internal static void InvertNormalGreenChannel(string normalPath, string outputDirectory)
        {
            using var image = Image.Load<Rgb24>(normalPath);
            // Invert the green channel
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    pixel.G = (byte)(255 - pixel.G); // Invert green channel
                    image[x, y] = pixel;
                }
            }
            var filePath = $"{outputDirectory}/UnityNormal.png";
            image.SaveAsPng(filePath);
            Console.WriteLine($"Saved to {filePath}");
        }

        internal static void MetallicConversion(string metalPath, string roughnessPath, string outputDirectory)
        {
            using var rgbImage = Image.Load<Rgb24>(metalPath);
            using var alphaImage = Image.Load<L8>(roughnessPath);

            if (rgbImage.Width != alphaImage.Width || rgbImage.Height != alphaImage.Height)
                throw new InvalidOperationException($"metallic and roughness images must be the same dimensions. Current metallic dimensions: {rgbImage.Width}x{rgbImage.Height}. Current Roughness dimensions: {alphaImage.Width}x{alphaImage.Height}");

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


            var filePath = $"{outputDirectory}/MetalSmooth.png";
            outImage.SaveAsPng(filePath);

            Console.WriteLine($"Saved to {filePath}");

            return;
        }
    }
}