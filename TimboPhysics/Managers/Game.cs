using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace TimboPhysics;

public class Game : GameWindow
{
    private List<RenderObject> _renderObjects = new ();
    private List<PhysicsObject> _physicsObjects = new ();
    private List<PhysicsParticle> _physicsParticles = new ();
    public List<RenderObject> _collisionObjects = new();
    private Shader _shader;
    private float _AspectRatio = 1;
    private Camera _camera;

    public Game(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings) 
        : base(gameSettings, nativeSettings)
    {
        _camera = new Camera(new Vector3(0, 5,10), _AspectRatio);
        CursorVisible = false;
        CursorGrabbed = true;
    }

    private void AddObject(RenderObject newObject, bool render, bool physics, bool particle, bool collision)
    {
        if(render){ _renderObjects.Add(newObject);}
        if(collision){_collisionObjects.Add(newObject);}
        if(physics){_physicsObjects.Add((PhysicsObject)newObject);}
        if(particle){_physicsParticles.Add((PhysicsParticle)newObject);}
    }
    
    protected override void OnLoad()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.ClearColor(new Color4(0.2f,0.2f,1f,1f));
        
        _shader = new Shader("Shaders/lighting.vert", "Shaders/lighting.frag");
        
        var floor = new Staticbody(
            new RectPrism(
                new Vector3d(0,-15,0), 
                300, 
                0.5, 
                300, 
                Quaterniond.FromEulerAngles(0, 0, 0)), 
            _shader);
        //_renderObjects.Add(floor);
        //_physicsObjects.Add(floor);
        
        
        for (int i = 0; i < 0; i++)
        {
            _physicsObjects.Add(new Staticbody(
                new RectPrism(
                    new Vector3d(i%2*13-5,10*i-10,0),
                    13, 
                    0.5, 
                    5, 
                    Quaterniond.FromEulerAngles(45*i%2>0?1:-1, 0, 0)), 
                _shader));
        }

        var rand = new Random();
        for (int i = 0; i < 600; i++)
        {
            //_physicsParticles.Add(new PhysicsParticle(new Vector3d(rand.NextDouble()*5 * (i%1-0.5), 2+i/5 , rand.NextDouble()*5 * (i%1-0.5)), 0.3, _shader));
            var pm = i % 2 == 1 ? 1 : -1;
            _physicsParticles.Add(new PhysicsParticle(pm,new Vector3d(rand.NextDouble()-0.5, rand.NextDouble()-0.5, rand.NextDouble()-0.5), 0.05, _shader));
        }
        
        base.OnLoad();
    }

    protected override void OnUnload()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        _shader.Dispose();
        base.OnUnload();
        Environment.Exit(0);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.AspectRatio = e.Width / (float)e.Height;
        base.OnResize(e);
    }
    
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        if (IsFocused)
        {
            _camera.MouseMove(e.Delta);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        _camera.Fov += e.OffsetY;
        base.OnMouseWheel(e);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        var input = KeyboardState;
        if (input.IsKeyDown(Keys.Escape))
        {
            Close();
            throw new Exception("closing program");
        }

        _camera.Move(input, (float)args.Time);

        var physicsObjectUpdateTasks = new Task[_physicsObjects.Count];
        var physicsParticleUpdateTasks = new Task[_physicsParticles.Count];
        
        for (int i = 0; i < _physicsObjects.Count; i++)
        {
            if (input.IsKeyDown(Keys.U))
            {
                for (uint j = 0; j < _physicsObjects[i]._vertexLookup.Count; j++)
                {
                    var vertex = _physicsObjects[i]._vertexLookup[j];
                    vertex.Speed += Vector3d.UnitY/10;
                    _physicsObjects[i]._vertexLookup[j] = vertex;
                }
            }
            var taskNum = i;
            physicsObjectUpdateTasks[taskNum] = Task.Factory.StartNew(() => _physicsObjects[taskNum].Update(_physicsObjects, args.Time));
        }
        for (int i = 0; i < _physicsParticles.Count; i++)
        {
            var taskNum = i;
            //physicsParticleUpdateTasks[taskNum] = Task.Factory.StartNew(() => _physicsParticles[taskNum].Update(_physicsParticles, args.Time));
            _physicsParticles[taskNum].Update(_physicsParticles, args.Time);
        }
        Collision.ResolveCollision(_physicsParticles);

        foreach (var task in physicsObjectUpdateTasks)
        {
            task.Wait();
        }
        foreach (var task in physicsParticleUpdateTasks)
        {
            //task.Wait();
        }

        base.OnUpdateFrame(args);
    }
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        // Console.WriteLine(_frames / _timer.Elapsed.TotalSeconds);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        for (int i = 0; i < _renderObjects.Count; i++)
        {
            _renderObjects[i].Render(_camera.GetViewMatrix(), _camera.GetProjectionMatrix(), _camera.Position);
        }
        for (int i = 0; i < _physicsObjects.Count; i++)
        {
            _physicsObjects[i].Render(_camera.GetViewMatrix(), _camera.GetProjectionMatrix(), _camera.Position);
        }
        for (int i = 0; i < _physicsParticles.Count; i++)
        {
            _physicsParticles[i].Render(_camera.GetViewMatrix(), _camera.GetProjectionMatrix(), _camera.Position);
        }
        
        Context.SwapBuffers();
        
        base.OnRenderFrame(args);
    }
}