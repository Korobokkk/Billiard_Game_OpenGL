using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
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
    int EBO;
    Shader shaderProgram = new Shader();
    int textureVBO;
    int textureID;
    float[] vertices = {
        -0.5f, 0.5f, 0f, // top left vertex - 0
        0.5f, 0.5f, 0f, // top right vertex - 1
        0.5f, -0.5f, 0f, // bottom right vertex - 2
        -0.5f, -0.5f, 0f // bottom left vertex - 3

    };

    uint[] indices =
    {
        0, 1, 2, //top triangle
        2, 3, 0 //bottom triangle
    };
    
    int width, height;
    public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        this.CenterWindow(new Vector2i(width, height));
        this.height = height;
        this.width = width;
    }
    float[] texCoords =
    {
        0f, 1f,
        1f, 1f,
        1f, 0f,
        0f, 0f
    };
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

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        //Enable the slot
        GL.EnableVertexArrayAttrib(VAO, 0);
        //Unbind the VBO
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        EBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length *
        sizeof(uint), indices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


        //Create, bind texture
        textureVBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
        GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Length * sizeof(float), texCoords, BufferUsageHint.StaticDraw);
        //Point a slot number 1
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
        //Enable the slot
        GL.EnableVertexArrayAttrib(VAO, 1);


        //Delete everything
        GL.BindVertexArray(0);

        shaderProgram.LoadShader();

        // Texture Loading
        textureID = GL.GenTexture(); //Generate empty texture
        GL.ActiveTexture(TextureUnit.Texture0); //Activate the texture in the unit
        GL.BindTexture(TextureTarget.Texture2D, textureID); //Bind texture

        //Texture parameters
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        //Load image
        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/2.jpg"), ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

        //Unbind the texture
        GL.BindTexture(TextureTarget.Texture2D, 0);


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
        GL.DeleteBuffer(EBO);

        GL.DeleteTexture(textureID);

        shaderProgram.DeleteShader();
        base.OnUnload();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {

        GL.ClearColor(0.3f, 0.3f, 1f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        shaderProgram.UseShader();
        GL.BindTexture(TextureTarget.Texture2D, textureID);
        GL.BindVertexArray(VAO);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length,
        DrawElementsType.UnsignedInt, 0);

        


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