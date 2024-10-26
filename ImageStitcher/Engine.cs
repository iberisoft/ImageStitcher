using Python.Runtime;
using System.IO;
using System.Text;
using System.Windows;

namespace ImageStitcher;

static class Engine
{
    static nint m_ThreadState;

    public static void Initialize()
    {
        Runtime.PythonDLL = Settings.Default.PythonPath;
        PythonEngine.Initialize();
        m_ThreadState = PythonEngine.BeginAllowThreads();
    }

    public static void Shutdown()
    {
        if (m_ThreadState != default)
        {
            PythonEngine.EndAllowThreads(m_ThreadState);
        }
        PythonEngine.Shutdown();
    }

    public static string StitchImages(string sourceImagePath1, Rect? roi1, string sourceImagePath2, Rect? roi2, string resultImagePath)
    {
        using (Py.GIL())
        {
            var console = new PyConsole();

            var globals = new PyDict();
            globals["console"] = console.ToPython();
            var locals = new PyDict();
            locals["src_path1"] = new PyString(sourceImagePath1);
            locals["roi1"] = ConvertRect(roi1);
            locals["src_path2"] = new PyString(sourceImagePath2);
            locals["roi2"] = ConvertRect(roi2);
            locals["res_path"] = new PyString(resultImagePath);

            PythonEngine.Exec(File.ReadAllText(Settings.Default.ScriptPath), globals, locals);
            return console.ToString();
        }
    }

    private static PyObject ConvertRect(Rect? rect)
    {
        if (rect != null)
        {
            var dict = new PyDict();
            dict["x"] = new PyInt((int)rect.Value.X);
            dict["y"] = new PyInt((int)rect.Value.Y);
            dict["width"] = new PyInt((int)rect.Value.Width);
            dict["height"] = new PyInt((int)rect.Value.Height);
            return dict;
        }
        else
        {
            return PyObject.None;
        }
    }

    class PyConsole
    {
        readonly StringWriter m_Writer = new();

        public override string ToString() => m_Writer.ToString();

#pragma warning disable IDE1006 // Naming Styles

        public void print(PyObject value) => m_Writer.WriteLine(value.ToString());

#pragma warning restore IDE1006 // Naming Styles
    }
}
