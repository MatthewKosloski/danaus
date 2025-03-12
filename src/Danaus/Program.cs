using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using System.Drawing;
using Danaus.Network;
using Danaus.HTML;

class Program
{

    static async Task Main(string[] args)
    {
        // var str = "http://example.org";
        // var request = HttpRequest.FromURLString(str);
        // var res = await RequestService.GetResponse(request);

        MemoryStream memoryStream = new MemoryStream();
        StreamWriter writer = new StreamWriter(memoryStream);

        writer.WriteLine("<p>hi</p>");
        writer.Flush(); // Ensure all data is written to the stream

        // Reset the position of the stream to the beginning
        memoryStream.Position = 0;

        StreamReader reader = new StreamReader(memoryStream);

        var htmlTokenizer = new HTMLTokenizer(reader);
        var tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
        tok = htmlTokenizer.NextToken();
    }

    private static void Run(SKCanvas canvas)
    {
        canvas.Clear(SKColor.Parse("#ff9900"));
    }

    private unsafe static Window* CreateWindow()
    {
        var settings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(800, 600),
            Title = "Danaus Browser",
        };

        var window = GLFW.CreateWindow(
            settings.ClientSize.X,
            settings.ClientSize.Y,
            settings.Title,
            null,
            (Window*)IntPtr.Zero);

        return window;
    }

    private static SKSurface GenerateSkiaSurface(GRContext skiaContext, Size surfaceSize)
    {
        var colorType = SKColorType.Rgba8888;
        var frameBufferInfo = new GRGlFramebufferInfo((uint)new UIntPtr(0), colorType.ToGlSizedFormat());
        var backendRenderTarget = new GRBackendRenderTarget(
            surfaceSize.Width,
            surfaceSize.Height,
            0,
            0,
            frameBufferInfo);
        return SKSurface.Create(
            skiaContext,
            backendRenderTarget,
            GRSurfaceOrigin.BottomLeft,
            colorType);
    }

    private static GRContext GenerateSkiaContext()
    {
        var glInterface = GRGlInterface.Create();
        return GRContext.CreateGl(glInterface);
    }

    private unsafe static void KeyCallback(Window* window, Keys key, int scancode, InputAction action, KeyModifiers mods)
    {
        switch (key)
        {
            case Keys.Space:
                Console.WriteLine("Spacebar pressed.");
            break;
        }
    }
}