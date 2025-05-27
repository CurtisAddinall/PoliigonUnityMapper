using System;
using System.IO;
using Xunit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using PoliigonUnityMapper;

public class MetallicTests
{
    [Fact]
    public void InvertNormalGreenChannel_InvertsGreenChannelAndSavesImage()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "normal.png");
        var outputPath = Path.Combine(tempDir, "UnityNormal.png");

        using (var img = new Image<Rgb24>(2, 1))
        {
            img[0, 0] = new Rgb24(10, 20, 30);
            img[1, 0] = new Rgb24(40, 50, 60);
            img.Save(inputPath);
        }

        // Act
        PoliiMapper.InvertNormalGreenChannel(inputPath, tempDir);

        // Assert
        Assert.True(File.Exists(outputPath));
        using (var result = Image.Load<Rgb24>(outputPath))
        {
            Assert.Equal(10, result[0, 0].R);
            Assert.Equal(235, result[0, 0].G); // 255-20
            Assert.Equal(30, result[0, 0].B);

            Assert.Equal(40, result[1, 0].R);
            Assert.Equal(205, result[1, 0].G); // 255-50
            Assert.Equal(60, result[1, 0].B);
        }
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void MetallicConversion_CombinesImagesAndSavesResult()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var metalPath = Path.Combine(tempDir, "metal.png");
        var roughPath = Path.Combine(tempDir, "rough.png");
        var outputPath = Path.Combine(tempDir, "MetalSmooth.png");

        using (var img = new Image<Rgb24>(1, 1))
        {
            img[0, 0] = new Rgb24(1, 2, 3);
            img.Save(metalPath);
        }
        using (var img = new Image<L8>(1, 1))
        {
            img[0, 0] = new L8(10);
            img.Save(roughPath);
        }

        // Act
        PoliiMapper.MetallicConversion(metalPath, roughPath, tempDir);

        // Assert
        Assert.True(File.Exists(outputPath));
        using (var result = Image.Load<Rgba32>(outputPath))
        {
            var px = result[0, 0];
            Assert.Equal(1, px.R);
            Assert.Equal(2, px.G);
            Assert.Equal(3, px.B);
            Assert.Equal(245, px.A); // 255-10
        }
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void ConvertAllFilesInDir_ProcessesAllFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var normalPath = Path.Combine(tempDir, "test_Normal.png");
        var metalPath = Path.Combine(tempDir, "test_Metallic.png");
        var roughPath = Path.Combine(tempDir, "test_Roughness.png");

        using (var img = new Image<Rgb24>(1, 1)) { img[0, 0] = new Rgb24(1, 2, 3); img.Save(normalPath); }
        using (var img = new Image<Rgb24>(1, 1)) { img[0, 0] = new Rgb24(4, 5, 6); img.Save(metalPath); }
        using (var img = new Image<L8>(1, 1)) { img[0, 0] = new L8(7); img.Save(roughPath); }

        // Act
        PoliiMapper.ConvertAllFilesInDir(tempDir, tempDir);

        // Assert
        Assert.True(File.Exists(Path.Combine(tempDir, "UnityNormal.png")));
        Assert.True(File.Exists(Path.Combine(tempDir, "MetalSmooth.png")));
        Directory.Delete(tempDir, true);
    }
}
