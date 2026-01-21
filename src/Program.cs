using System;
using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

class Program
{
    static void Main()
    {
        int width = 800;
        int height = 600;

        SetTraceLogLevel(TraceLogLevel.Warning);

        InitWindow(width, height, "test");

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

            if (IsMouseButtonDown(MouseButton.Right)) unsafe { UpdateCamera(&camera, CameraMode.ThirdPerson); }

            if (IsKeyPressed(KeyboardKey.F)) simulation.IterateSpaceColonization();

            // rendering

            BeginDrawing();
            ClearBackground(new Color(88, 88, 88, 255));

            BeginMode3D(camera);

            DrawGrid(10, 1);

            for (int i = 0; i < simulation.passages.Count; i++)
            {
                bool extremity = simulation.extremities.Contains(simulation.passages[i]);
                Color color = extremity ? Color.Green : Color.White;
                DrawLine3D(simulation.passages[i].start, simulation.passages[i].end, color);
            }

            EndMode3D();

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