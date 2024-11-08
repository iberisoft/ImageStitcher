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

    public static string StitchImages(string sourceImagePath1, Rect roi1, string sourceImagePath2, Rect roi2, string resultImagePath)
    {
        using (Py.GIL())
        {
            var locals = new PyDict();
            locals["src_path1"] = new PyString(sourceImagePath1);
            locals["roi1"] = ConvertRect(roi1);
            locals["src_path2"] = new PyString(sourceImagePath2);
            locals["roi2"] = ConvertRect(roi2);
            locals["res_path"] = new PyString(resultImagePath);

            using var scope = Py.CreateScope();
            foreach (var moduleName in Settings.Default.PythonModules.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                scope.Import(moduleName);
            }
            var console = new PyConsole();
            scope.Variables()["console"] = console.ToPython();
            scope.Exec(File.ReadAllText(Settings.Default.ScriptPath), locals);
            return console.ToString();
        }
    }

    private static PyObject ConvertRect(Rect rect)
    {
        var dict = new PyDict();
        dict["x"] = new PyInt((int)rect.X);
        dict["y"] = new PyInt((int)rect.Y);
        dict["width"] = new PyInt((int)rect.Width);
        dict["height"] = new PyInt((int)rect.Height);
        return dict;
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
