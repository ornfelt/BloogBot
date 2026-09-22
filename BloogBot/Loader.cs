// Ported from BloogBot/BloogBot/Loader.cs (.NET Framework 4.8 -> .NET 9). Replica - do not redesign.
using System;
using System.IO;
using System.Reflection;
using System.Threading;
// 'using BloogBot.UI;' dropped: the shell lives in BloogBot.UI.Wpf / BloogBot.UI.Avalonia, and both
// reach this assembly through BloogBot.UI.Core, so this assembly cannot reference them back.
// App.Main is resolved by reflection instead - see ShellMain below.

namespace BloogBot
{
    // The type and the method are public, and Load takes hostfxr's component_entry_point_fn
    // signature (void* arg, int32_t argSizeInBytes). Under .NET Framework Loader.dll reached this
    // through ICLRRuntimeHost::ExecuteInDefaultAppDomain, which accepted a non-public
    // 'static int Load(string args)'; hostfxr's load_assembly_and_get_function_pointer does not.
    // Original: 'class Loader' with 'static int Load(string args)'.
    public class Loader
    {
        static Thread thread;

        // The only place UI_WPF / UI_AVALONIA are used. Directory.Build.props defines exactly one.
#if UI_WPF
        const string ShellAssemblyName = "BloogBot.UI.Wpf";
#endif
#if UI_AVALONIA
        const string ShellAssemblyName = "BloogBot.UI.Avalonia";
#endif

        public static int Load(IntPtr arg, int argSize)
        {
            thread = new Thread(ShellMain());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return 1;
        }

        // Original: 'new Thread(App.Main)'. The shell assembly is loaded by path - every managed
        // project in the solution builds into the same output folder, so it sits next to this one.
        static ThreadStart ShellMain()
        {
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assembly = Assembly.LoadFrom(Path.Combine(currentFolder, ShellAssemblyName + ".dll"));
            var appType = assembly.GetType(ShellAssemblyName + ".App", true);
            var main = appType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            return (ThreadStart)Delegate.CreateDelegate(typeof(ThreadStart), main);
        }
    }
}
