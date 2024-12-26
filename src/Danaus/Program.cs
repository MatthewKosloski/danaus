using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using System.Drawing;
using Danaus.Url;

class Program
{

    unsafe static int Main(string[] args)
    {

        var input = "https://google.com/";

        var result = URLParser.Parse(input);

        // var window = CreateWindow();

        // if (window == null)
        // {
        //     // Failed to create a window.
        //     GLFW.Terminate();
        //     return -1;
        // }

        // GLFW.SetKeyCallback(window, KeyCallback);

        // GLFW.MakeContextCurrent(window);

        // var skContext = GenerateSkiaContext();
        // var skSurface = GenerateSkiaSurface(skContext, new Size(800, 600));

        // var canvas = skSurface.Canvas;

        // while (!GLFW.WindowShouldClose(window))
        // {
        //     Run(canvas);
        //     canvas.Flush();
        //     GLFW.SwapBuffers(window);
        //     GLFW.PollEvents();
        // }

        // GLFW.Terminate();

        return 0;
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