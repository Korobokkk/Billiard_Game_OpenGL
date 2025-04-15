using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

internal class Game: GameWindow
{
    int VAO;
    int VBO;
    int shaderProgram;
    float[] vertices = {
    0f, 0.5f, 0f,
    -0.5f, -0.5f, 0f,
    0.5f, -0.5f, 0f
    };
    int width, height;
    public Game(int width, int height):base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        this.CenterWindow(new Vector2i(width, height));
        this.height = height;
        this.width = width;
    }
    protected override void OnLoad()
    {
        //Create VAO
        VAO = GL.GenVertexArray();
        //Create VBO
        VBO = GL.GenBuffer();
        //Bind the VBO
        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
        //Copy vertices data to the buffer
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length *
        sizeof(float), vertices, BufferUsageHint.StaticDraw);
        //Bind the VAO
        GL.BindVertexArray(VAO);
        //Bind a slot number 0
        
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
        0, 0);
        //Enable the slot
        GL.EnableVertexArrayAttrib(VAO, 0);
        //Unbind the VBO
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        shaderProgram = GL.CreateProgram();
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, LoadShaderSource("shader.vert"));
        GL.CompileShader(vertexShader);
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, LoadShaderSource("shader.frag"));
        GL.CompileShader(fragmentShader);
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);


        base.OnLoad();
    }

    public static string LoadShaderSource(string filepath)
    {
        string shaderSource = "";
        try
        {
            using (StreamReader reader = new StreamReader("../../../Shaders/" + filepath))
            {
                shaderSource = reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to load shader source file:" + e.Message);
            
        }
        return shaderSource;

    }

    protected override void OnUnload()
    {
        base.OnUnload();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {

        GL.ClearColor(0.3f, 0.3f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.UseProgram(shaderProgram);
        GL.BindVertexArray(VAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        Context.SwapBuffers();

        base.OnRenderFrame(args);
    }
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if( KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
        base.OnUpdateFrame(args);
    }
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        this.width = e.Width;
        this.height = e.Height; 
    }
}