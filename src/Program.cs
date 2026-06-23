using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

unsafe class Program
{
    static void Main()
    {
        SetTraceLogLevel(TraceLogLevel.Warning);
        SetConfigFlags(ConfigFlags.ResizableWindow);
        InitWindow(1280, 720, "raylib window");

        var camera = new Camera3D()
        {
            Position = new(0, 2, 4),
            Target = Vector3.Zero,
            Up = Vector3.UnitY,
            FovY = 60f,
            Projection = (int)CameraProjection.Perspective
        };

        Simulation simulation = new Simulation();
        simulation.Initialize();

        while (!WindowShouldClose())
        {
            // update here

            if (IsMouseButtonDown(MouseButton.Right)) UpdateCamera(&camera, CameraMode.ThirdPerson);

            if (IsKeyPressed(KeyboardKey.F)) Repeater.ToggleRepeating(simulation.IterateSpaceColonization, 0.1f);

            if (IsKeyPressed(KeyboardKey.R))
            {
                simulation.Clear();
                simulation.Initialize();
            }

            var dir = Vector3.Zero - camera.Position;
            camera.Position += dir * GetMouseWheelMove() * GetFrameTime() * 20;

            // rendering 3d here

            BeginDrawing();
                ClearBackground(new Color(88, 88, 88, 255));

                BeginMode3D(camera);

                    DrawGrid(10, 1);

                    for (int i = 0; i < simulation.lines.Count; i++)
                    {
                        bool extremity = simulation.extremities.Contains(simulation.lines[i]);
                        Color color = extremity ? Color.Green : Color.White;
                        DrawLine3D(simulation.lines[i].start, simulation.lines[i].end, color);
                    }

                EndMode3D();

                // rendering 2d here

                for (int i = 0; i < simulation.nodes.Count; i++)
                {
                    Vector2 screenSpace = GetWorldToScreen(simulation.nodes[i], camera);
                    DrawCircleV(screenSpace, 2, Color.Red);
                }

                DrawText(GetFPS().ToString(), 8, 8, 14, Color.Black);

            EndDrawing();
        }

        CloseWindow();
    }
}