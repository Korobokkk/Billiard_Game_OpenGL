using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;


namespace Open_TK
{

    public class Shader
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

        public void UseShader()
        {
            GL.UseProgram(shaderHandle);
        }
        public void DeleteShader()
        {
            GL.DeleteProgram(shaderHandle);
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


    }

    public class Sphere
    {
        public Shader shaderProgram = new Shader();
        public int VAO, VBO, EBO, texVBO, textureID;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> texCoords = new List<Vector2>();
        List<uint> indices = new List<uint>();

        int sectorCount = 36; // Долгота
        int stackCount = 18;  // Широта
        float radius = 0.25f;

        public void Initialize()
        {
            GenerateSphereData();
            OnLoad();
        }

        private void GenerateSphereData()
        {
            for (int i = 0; i <= stackCount; ++i)
            {
                float stackAngle = MathF.PI / 2 - i * MathF.PI / stackCount; // от pi/2 до -pi/2
                float xy = radius * MathF.Cos(stackAngle);
                float z = radius * MathF.Sin(stackAngle);

                for (int j = 0; j <= sectorCount; ++j)
                {
                    float sectorAngle = j * 2 * MathF.PI / sectorCount; // от 0 до 2pi

                    float x = xy * MathF.Cos(sectorAngle);
                    float y = xy * MathF.Sin(sectorAngle);
                    vertices.Add(new Vector3(x, y, z));

                    // Текстурные координаты
                    float s = (float)j / sectorCount;
                    float t = (float)i / stackCount;
                    texCoords.Add(new Vector2(s, t));
                }
            }

            for (int i = 0; i < stackCount; ++i)
            {
                int k1 = i * (sectorCount + 1); // начальная вершина текущей широты
                int k2 = k1 + sectorCount + 1;  // начальная вершина следующей широты

                for (int j = 0; j < sectorCount; ++j, ++k1, ++k2)
                {
                    if (i != 0)
                    {
                        indices.Add((uint)k1);
                        indices.Add((uint)k2);
                        indices.Add((uint)(k1 + 1));
                    }

                    if (i != (stackCount - 1))
                    {
                        indices.Add((uint)(k1 + 1));
                        indices.Add((uint)k2);
                        indices.Add((uint)(k2 + 1));
                    }
                }
            }
        }

        public void OnLoad()
        {
            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // VBO для вершин
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);

            // VBO для текстурных координат
            texVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, texVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector2.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(1);

            // EBO
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(uint), indices.ToArray(), BufferUsageHint.StaticDraw);

            // Отвязываем VAO
            GL.BindVertexArray(0);

            shaderProgram.LoadShader();

            // Загрузка текстуры
            textureID = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult texture = ImageResult.FromStream(File.OpenRead("../../../Textures/2.jpg"), ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texture.Width, texture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, texture.Data);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            shaderProgram.UseShader();

            Matrix4 model = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref model);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "view"), true, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "projection"), true, ref projection);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }


    public class PlatformWall1//придумать норм название
    {
        public Shader shaderProgram = new Shader();
        public int VAO, VBO, EBO, textureVBO, textureID;
        List<Vector3> vertices = new List<Vector3>()
        {
            new Vector3(-2.51f,  0.35f, -5f), //top-left vertice
			new Vector3(-2.51f,  0.35f, 5f), //top-right vertice
			new Vector3(-2.51f,  0f, 5f), //bottom-right vertice
			new Vector3(-2.51f,  0f, -5f), //botom-left vertice
        };


        List<Vector2> texCoords = new List<Vector2>()
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };

        uint[] indices =
        {
            0, 1, 2,	//top triangle
			2, 3, 0,    //bottom triangle
        };


        //ортогональ
        public int VAO1, VBO1, EBO1, textureVBO1 ;
        List<Vector3> vertices1 = new List<Vector3>()
        {
            new Vector3(-2.51f,  0.35f, -5.01f), //top-left vertice
			new Vector3(2.51f,  0.35f, -5.01f), //top-right vertice
			new Vector3(2.51f,  0f, -5.01f), //bottom-right vertice
			new Vector3(-2.51f,  0f, -5.01f), //botom-left vertice
        };


        List<Vector2> texCoords1 = new List<Vector2>()
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };
        uint[] indices1 =
        {
            0, 1, 2,	//top triangle
			2, 3, 0,    //bottom triangle
        };
        public void Initialize()
        {
            this.OnLoad();
        }

        public void OnLoad()
        {

            //Создаем вершинный буфер и буфер с данными вершин
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer,
                vertices.Count * Vector3.SizeInBytes,
                vertices.ToArray(),
                BufferUsageHint.StaticDraw);
            GL.BindVertexArray(VAO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(VAO, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            //EBO
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create, bind texture
            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector3.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
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
            ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/wall.png"), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            //Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);
            //__________________Ортогональ_____________________________________
            VAO1 = GL.GenVertexArray();
            VBO1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO1);
            GL.BufferData(BufferTarget.ArrayBuffer,
                vertices1.Count * Vector3.SizeInBytes,
                vertices1.ToArray(),
                BufferUsageHint.StaticDraw);
            GL.BindVertexArray(VAO1);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(VAO1, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            //EBO
            EBO1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO1);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices1.Length * sizeof(uint), indices1, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create, bind texture
            textureVBO1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO1);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords1.Count * Vector3.SizeInBytes, texCoords1.ToArray(), BufferUsageHint.StaticDraw);
            //Point a slot number 1
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
            //Enable the slot
            GL.EnableVertexArrayAttrib(VAO1, 1);


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
            ImageResult boxTexture1 = ImageResult.FromStream(File.OpenRead("../../../Textures/wall.png"), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture1.Width, boxTexture1.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture1.Data);

            //Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            shaderProgram.UseShader();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            Matrix4 model = Matrix4.Identity; // платформа уже внизу сцены

            int modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            //ВТОРОЙ

            Vector3 tmp = new Vector3(5.02f, 0f, 0f);
            Matrix4 model2 =  Matrix4.CreateTranslation(tmp);
            //model2 *= Matrix4.CreateScale(1f, 1f, -1f);

            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref model2);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);


            //ортогональ


            //ближняя стенка
            Matrix4 modelFront = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref modelFront);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "view"), true, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "projection"), true, ref projection);

            GL.BindVertexArray(VAO1);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO1);
            GL.DrawElements(PrimitiveType.Triangles, indices1.Length, DrawElementsType.UnsignedInt, 0);

            //дальняя стенка
            Vector3 tmp1 = new Vector3(0f, 0f, 10.02f);
            Matrix4 modelFront1 = Matrix4.CreateScale(-1f, 1f, 1f) * Matrix4.CreateTranslation(tmp1);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref modelFront1);
            GL.DrawElements(PrimitiveType.Triangles, indices1.Length, DrawElementsType.UnsignedInt, 0);
        }

    }
    public class PlatformWall()
    {
        public Shader shaderProgram = new Shader();
        public int VAO, VBO, EBO, textureVBO, textureID;

        public int VAO1, VBO1, EBO1, textureVBO1;

        // Толщина стенки xo начальная координата по x, lделаем x1 
        const float wideWall = 0.5f;
        const float x0 = -2.5f;
        const float x1 = x0 + wideWall;

        //первый бортик
        List<Vector3> vertices = new List<Vector3>()
        {
            // перед
            new Vector3(x0, 0.35f, -4f), // 0 top-left
            new Vector3(x0,  0.35f,  -0.5f), // 1 top-right
            new Vector3(x0, 0f,  -0.5f), // 2 bottom-right
            new Vector3(x0, 0f, -4f), // 3 bottom-left

            // зад 
            new Vector3(x1,  0.35f, -4f), // 4
            new Vector3(x1,  0.35f,  -0.5f), // 5
            new Vector3(x1, 0f,  -0.5f), // 6
            new Vector3(x1, 0f, -4f), // 7
        };

        List<Vector2> texCoords = new List<Vector2>()
        {
            new Vector2(0f,1f), new Vector2(1f,1f),
            new Vector2(1f,0f), new Vector2(0f,0f),
            new Vector2(0f,1f), new Vector2(1f,1f),
            new Vector2(1f,0f), new Vector2(0f,0f),
        };

        uint[] indices = new uint[]
        {
            0,1,2,   2,3,0,//front
            4,7,6,   6,5,4,//back
            4,5,1,   1,0,4,//соединяем фронт и бэк
            3,2,6,   6,7,3,
            4,0,3,   3,7,4,
            1,5,6,   6,2,1,
        };


        //ортогональный бортик другой длины
        const float z0 = -5f;
        const float z1 = z0 + wideWall;
        List<Vector3> vertices1 = new List<Vector3>()
        {
            // перед
            new Vector3(-1.5f, 0.35f, z0), // 0 top-left
            new Vector3(1.5f,  0.35f,  z0), // 1 top-right
            new Vector3(1.5f, 0f,  z0), // 2 bottom-right
            new Vector3(-1.5f, 0f, z0), // 3 bottom-left

            // зад 
            new Vector3(-1.5f,  0.35f, z1), // 4
            new Vector3(1.5f,  0.35f,  z1), // 5
            new Vector3(1.5f, 0f, z1), // 6
            new Vector3(-1.5f, 0f, z1), // 7
        };

        List<Vector2> texCoords1 = new List<Vector2>()
        {
            new Vector2(0f,1f), new Vector2(1f,1f),
            new Vector2(1f,0f), new Vector2(0f,0f),
            new Vector2(0f,1f), new Vector2(1f,1f),
            new Vector2(1f,0f), new Vector2(0f,0f),
        };

        uint[] indices1 = new uint[]
        {
            0,1,2,   2,3,0,//front
            4,7,6,   6,5,4,//back
            4,5,1,   1,0,4,//соединяем фронт и бэк
            3,2,6,   6,7,3,
            4,0,3,   3,7,4,
            1,5,6,   6,2,1,
        };


        public void Initialize()
        {
            this.OnLoad();
        }

        public void OnLoad()
        {

            //Создаем вершинный буфер и буфер с данными вершин
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer,
                vertices.Count * Vector3.SizeInBytes,
                vertices.ToArray(),
                BufferUsageHint.StaticDraw);
            GL.BindVertexArray(VAO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(VAO, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            //EBO
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create, bind texture
            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector3.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
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
            ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/wall1.jpg"), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            //Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);
            //________________________________________NEW VAO_______________________________________________________
            //Создаем вершинный буфер и буфер с данными вершин
            VAO1 = GL.GenVertexArray();
            VBO1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO1);
            GL.BufferData(BufferTarget.ArrayBuffer,
                vertices1.Count * Vector3.SizeInBytes,
                vertices1.ToArray(),
                BufferUsageHint.StaticDraw);
            GL.BindVertexArray(VAO1);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(VAO1, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            //EBO
            EBO1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO1);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices1.Length * sizeof(uint), indices1, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create, bind texture
            textureVBO1 = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO1);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords1.Count * Vector3.SizeInBytes, texCoords1.ToArray(), BufferUsageHint.StaticDraw);
            //Point a slot number 1
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 0, 0);
            //Enable the slot
            GL.EnableVertexArrayAttrib(VAO1, 1);


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
            ImageResult boxTexture1 = ImageResult.FromStream(File.OpenRead("../../../Textures/wall1.jpg"), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture1.Width, boxTexture1.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture1.Data);

            //Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

        }
        public void Render(Matrix4 view, Matrix4 projection)
        {
            shaderProgram.UseShader();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            // первой стенки
            Matrix4 model1 = Matrix4.Identity;
            model1 *= Matrix4.CreateScale(-1f, 1f, 1f); // инвертируем
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref model1);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "view"), true, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "projection"), true, ref projection);

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            // 1 стенка со смещением
            Vector3 shift = new Vector3(0f, 0f, 4.5f);
            Matrix4 model1Shift = Matrix4.CreateTranslation(shift);
            model1Shift *= Matrix4.CreateScale(-1f, 1f, 1f);

            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref model1Shift);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            //Вторая стенка
            Vector3 tmp = new Vector3(-4.5f, 0f, 0f);
            Matrix4 model2 = Matrix4.CreateScale(-1f, 1f, 1f) * Matrix4.CreateTranslation(tmp);
            model2 *= Matrix4.CreateScale(1f, 1f, -1f);

            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref model2);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            //Вторая стенка+ shift
            Matrix4 model2Shift = Matrix4.CreateScale(-1f, 1f, 1f) * Matrix4.CreateTranslation(tmp) * Matrix4.CreateTranslation(shift);
            model2 *= Matrix4.CreateScale(1f, 1f, -1f);

            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref model2);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);


            //ближняя стенка
            Matrix4 modelFront = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref modelFront);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "view"), true, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "projection"), true, ref projection);

            GL.BindVertexArray(VAO1);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO1);
            GL.DrawElements(PrimitiveType.Triangles, indices1.Length, DrawElementsType.UnsignedInt, 0);

            //дальняя стенка
            Vector3 tmp1 = new Vector3(0f, 0f, 9.5f);
            Matrix4 modelFront1 = Matrix4.CreateScale(-1f, 1f, 1f) * Matrix4.CreateTranslation(tmp1);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram.shaderHandle, "model"), true, ref modelFront1);
            GL.DrawElements(PrimitiveType.Triangles, indices1.Length, DrawElementsType.UnsignedInt, 0);

        }
    }

   public class Platform
   {
        public Shader shaderProgram= new Shader();
        public int VAO, VBO, EBO, textureVBO, textureID;
        List<Vector3> vertices = new List<Vector3>()
        {
            new Vector3(-2.51f,  0f, 5.01f), //top-left vertice
		    new Vector3(2.51f,  0f, 5.01f), //top-right vertice
		    new Vector3(2.51f,  0f, -5.01f), //bottom-right vertice
		    new Vector3(-2.51f,  0f, -5.01f), //botom-left vertice
        };
        

        List<Vector2> texCoords = new List<Vector2>()
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };
        uint[] indices =
        {
            0, 1, 2,	//top triangle
		    2, 3, 0,    //bottom triangle
        };
        public void Initialize()
        {
            this.OnLoad();
        }

        public void OnLoad()
        {

            //Создаем вершинный буфер и буфер с данными вершин
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, 
                vertices.Count * Vector3.SizeInBytes, 
                vertices.ToArray(),
                BufferUsageHint.StaticDraw);
            GL.BindVertexArray(VAO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(VAO, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            //EBO
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create, bind texture
            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector3.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
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
            ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/qqq.jpg"), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            //Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            shaderProgram.UseShader();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            Matrix4 model = Matrix4.Identity; // платформа уже внизу сцены

            int modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        }
        
}

    internal class Game : GameWindow
    {
        int width, height;

        PlatformWall wall = new PlatformWall();
        Platform platform=new Platform();
        PlatformWall1 platform1= new PlatformWall1();
        Sphere sphere = new Sphere();

        List<Vector3> vertices = new List<Vector3>()
        {	
			//front face
			new Vector3(-0.5f,  0.5f, 0.5f), //top-left vertice
			new Vector3( 0.5f,  0.5f, 0.5f), //top-right vertice
			new Vector3( 0.5f, -0.5f, 0.5f), //bottom-right vertice
			new Vector3(-0.5f, -0.5f, 0.5f), //botom-left vertice
			//right face
			new Vector3( 0.5f,  0.5f, 0.5f), //top-left vertice
			new Vector3( 0.5f,  0.5f, -0.5f), //top-right vertice
			new Vector3( 0.5f, -0.5f, -0.5f), //bottom-right vertice
			new Vector3( 0.5f, -0.5f, 0.5f), //botom-left vertice
			//back face
			new Vector3(-0.5f,  0.5f, -0.5f), //top-left vertice
			new Vector3( 0.5f,  0.5f, -0.5f), //top-right vertice
			new Vector3( 0.5f, -0.5f, -0.5f), //bottom-right vertice
			new Vector3(-0.5f, -0.5f, -0.5f), //botom-left vertice
			//left face
			new Vector3( -0.5f,  0.5f, 0.5f), //top-left vertice
			new Vector3( -0.5f,  0.5f, -0.5f), //top-right vertice
			new Vector3( -0.5f, -0.5f, -0.5f), //bottom-right vertice
			new Vector3( -0.5f, -0.5f, 0.5f), //botom-left vertice
			// top face
			new Vector3(-0.5f,  0.5f, -0.5f), //top-left vertice
			new Vector3( 0.5f,  0.5f, -0.5f), //top-right vertice
			new Vector3( 0.5f, 0.5f, 0.5f), //bottom-right vertice
			new Vector3(-0.5f, 0.5f, 0.5f), //botom-left vertice
			//bottom face
			new Vector3(-0.5f,  -0.5f, -0.5f), //top-left vertice
			new Vector3( 0.5f,  -0.5f, -0.5f), //top-right vertice
			new Vector3( 0.5f, -0.5f, 0.5f), //bottom-right vertice
			new Vector3(-0.5f, -0.5f, 0.5f), //botom-left vertice
		};

        List<Vector2> texCoords = new List<Vector2>()
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),

            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
        };

        uint[] indices =
        {
            0, 1, 2,	//top triangle
			2, 3, 0,	//bottom triangle

			4, 5, 6,
            6, 7, 4,

            8, 9, 10,
            10, 11, 8,

            12, 13, 14,
            14, 15, 12,

            16, 17, 18,
            18, 19, 16,

            20, 21, 22,
            22, 23, 20
        };


        int VAO;
        int VBO;
        int EBO;
        Shader shaderProgram = new Shader();
        int textureID;
        int textureVBO;
        Camera camera;


        float yRot = 0f;

        public Game(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            this.CenterWindow(new Vector2i(width, height));
            this.height = height;
            this.width = width;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            platform.Initialize();
            wall.Initialize();
            platform1.Initialize();
            sphere.Initialize();
            //Create VAO
            VAO = GL.GenVertexArray();
            //Create VBO
            VBO = GL.GenBuffer();
            //Bind the VBO 
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            //Copy vertices data to the buffer
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * Vector3.SizeInBytes, vertices.ToArray(), BufferUsageHint.StaticDraw);
            //Bind the VAO
            GL.BindVertexArray(VAO);
            //Point a slot number 0
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            //Enable the slot
            GL.EnableVertexArrayAttrib(VAO, 0);
            //Unbind the VBO
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            //EBO 
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Create, bind texture
            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Count * Vector3.SizeInBytes, texCoords.ToArray(), BufferUsageHint.StaticDraw);
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
            ImageResult boxTexture = ImageResult.FromStream(File.OpenRead("../../../Textures/box.jpg"), ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, boxTexture.Width, boxTexture.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, boxTexture.Data);

            //Unbind the texture
            GL.BindTexture(TextureTarget.Texture2D, 0);


            GL.Enable(EnableCap.DepthTest);

            camera = new Camera(width, height, Vector3.Zero);
            CursorState = CursorState.Grabbed;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteBuffer(VAO);
            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteTexture(textureID);

            shaderProgram.DeleteShader();

        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {           
            GL.ClearColor(0.3f, 0.3f, 1f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shaderProgram.UseShader();
            GL.BindTexture(TextureTarget.Texture2D, textureID);

            //Transformation
            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjection();
            Matrix4 translation = Matrix4.CreateTranslation(0f, 0f, -3f);

            model = Matrix4.CreateRotationY(yRot);
            yRot += 0.001f;

            model *= translation;

            int modelLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "model");
            int viewLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "view");
            int projectionLocation = GL.GetUniformLocation(shaderProgram.shaderHandle, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            //отрисовка куба
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            
            //отрисовка платформы
            platform.Render(view, projection);
            wall.Render(view, projection);
            platform1.Render(view, projection);
            sphere.Render(view, projection);

            //свапчик
            Context.SwapBuffers();

            base.OnRenderFrame(args);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }
            MouseState mouse = MouseState;
            KeyboardState input = KeyboardState;

            base.OnUpdateFrame(args);
            camera.Update(input, mouse, args);

        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            this.width = e.Width;
            this.height = e.Height;
        }




    }


}