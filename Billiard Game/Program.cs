﻿using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

class Program
{
    static void Main(string[] args)
    {
        using (Game game = new Game(800, 400))
        {
            game.Run();
        }
    }
}