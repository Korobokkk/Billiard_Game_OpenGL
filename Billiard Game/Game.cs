﻿using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


class Shader
{
    public int shaderHandle;

    public void LoadShader()
    {
        shaderHandle = GL.CreateProgram();

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, LoadShaderSource("shader.vert"));
        GL.CompileShader(vertexShader);

        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success1);
        if (success1 == 0)
        {
            string infoLog = GL.GetShaderInfoLog(vertexShader);
            Console.WriteLine(infoLog);
        }

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, LoadShaderSource("shader.frag"));
        GL.CompileShader(fragmentShader);

        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int success2);
        if (success2 == 0)
        {
            string infoLog = GL.GetShaderInfoLog(fragmentShader);
            Console.WriteLine(infoLog);
        }

        GL.AttachShader(shaderHandle, vertexShader);
        GL.AttachShader(shaderHandle, fragmentShader);

        GL.LinkProgram(shaderHandle);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
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
            Console.WriteLine("Failed to load shader source file: " + e.Message);
        }
        return shaderSource;
    }

    public void UseShader()
    {
        GL.UseProgram(shaderHandle);
    }
    public void DeleteShader()
    {
        GL.DeleteProgram(shaderHandle);
    }

}


internal class Game : GameWindow
{
    int VAO;
    int VBO;
    Shader shaderProgram = new Shader();
    float[] vertices = {
    0f, 0.5f, 0f,
    -0.5f, -0.5f, 0f,
    0.5f, -0.5f, 0f
    };
    int width, height;
    public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
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

        shaderProgram.LoadShader();

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
        GL.DeleteBuffer(VAO);
        GL.DeleteBuffer(VBO);

        shaderProgram.DeleteShader();
        base.OnUnload();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {

        GL.ClearColor(0.3f, 0.3f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shaderProgram.UseShader();
        GL.BindVertexArray(VAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        Context.SwapBuffers();

        base.OnRenderFrame(args);
    }
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        if (KeyboardState.IsKeyDown(Keys.Escape))
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