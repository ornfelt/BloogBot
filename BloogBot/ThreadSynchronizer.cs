using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// This namespace contains the ThreadSynchronizer class which handles thread synchronization.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// This class provides methods for synchronizing threads.
    /// </summary>
    static public class ThreadSynchronizer
    {
        /// <summary>
        /// Sets a new value for a specified window long value.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// note over Developer: DllImport is used to import user32.dll
        /// Developer -> SetWindowLong: Calls function with parameters hWnd, nIndex, dwNewLong
        /// SetWindowLong --> Developer: Returns IntPtr
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        /// <summary>
        /// Calls the window procedure for the specified window.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// note over Developer: DllImport is used to import user32.dll
        /// Developer -> user32.dll: CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int Msg, int wParam, int lParam)
        /// user32.dll --> Developer: Returns int
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern int CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int Msg, int wParam, int lParam);

        /// <summary>
        /// Retrieves the identifier of the process that owns the specified window.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> user32.dll: GetWindowThreadProcessId(handle, processId)
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        /// <summary>
        /// Determines whether the specified window is visible or hidden.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// note over user32.dll: DLL Import
        /// user32.dll -> IsWindowVisible: Call function with hWnd parameter
        /// IsWindowVisible --> user32.dll: Returns boolean result
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Retrieves the length, in characters, of the specified window's title bar text (if it has one). If the specified window is a control, the function retrieves the length of the text within the control. However, GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> user32.dll: GetWindowTextLength(hWnd)
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Retrieves the text of the specified window's title bar (if it has one) and stores it in the specified string builder.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> user32.dll: DllImport
        /// user32.dll --> :Program: GetWindowText(hWnd, lpString, nMaxCount)
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Retrieves all top-level windows on the screen by passing the handle of each window, in turn, to an application-defined callback function.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> user32.dll: EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam)
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Sends the specified message to a window or windows.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Application -> "user32.dll": SendMessage(hWnd, Msg, wParam, lParam)
        /// \enduml
        /// </remarks>
        [DllImport("user32.dll")]
        static extern int SendMessage(
                            int hWnd,
                            uint Msg,
                            int wParam,
                            int lParam
                        );

        /// <summary>
        /// Retrieves the identifier of the current thread.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// note over Developer: Calls GetCurrentThreadId function from kernel32.dll
        /// Developer -> kernel32.dll: GetCurrentThreadId()
        /// kernel32.dll --> Developer: Returns thread ID
        /// \enduml
        /// </remarks>
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        /// <summary>
        /// Delegate for enumerating windows.
        /// </summary>
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Represents a delegate that defines the signature for a window procedure.
        /// </summary>
        delegate int WindowProc(IntPtr hWnd, int Msg, int wParam, int lParam);

        /// <summary>
        /// Represents a static readonly queue of actions.
        /// </summary>
        static readonly Queue<Action> actionQueue = new Queue<Action>();
        /// <summary>
        /// Represents a static readonly queue of delegates.
        /// </summary>
        static readonly Queue<Delegate> delegateQueue = new Queue<Delegate>();
        /// <summary>
        /// The static readonly queue that stores the returned values.
        /// </summary>
        static readonly Queue<object> returnValueQueue = new Queue<object>();

        /// <summary>
        /// The constant value representing the window procedure address for the GetWindowLong function.
        /// </summary>
        const int GWL_WNDPROC = -4;
        /// <summary>
        /// The WM_USER constant represents the Windows message identifier for user-defined messages.
        /// </summary>
        const int WM_USER = 0x0400;
        /// <summary>
        /// Represents the old callback function pointer.
        /// </summary>
        static IntPtr oldCallback;
        /// <summary>
        /// Represents a static callback function for handling window procedures.
        /// </summary>
        static WindowProc newCallback;
        /// <summary>
        /// Represents the handle of a window.
        /// </summary>
        static int windowHandle;

        /// <summary>
        /// Initializes the ThreadSynchronizer class by calling EnumWindows, passing in the FindWindowProc method and IntPtr.Zero as parameters. 
        /// It then assigns the WndProc method to the newCallback variable and sets the window procedure for the specified window handle to the newCallback method using SetWindowLong. 
        /// The oldCallback variable is assigned the function pointer for the newCallback method using Marshal.GetFunctionPointerForDelegate.
        /// </summary>
        static ThreadSynchronizer()
        {
            EnumWindows(FindWindowProc, IntPtr.Zero);
            newCallback = WndProc;
            oldCallback = SetWindowLong((IntPtr)windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(newCallback));
        }

        /// <summary>
        /// Runs the specified action on the main thread.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "MainThread" as A
        /// participant "OtherThread" as B
        /// B -> A: RunOnMainThread(action)
        /// alt GetCurrentThreadId() == MainThread
        ///     A -> A: action()
        /// else
        ///     A -> A: actionQueue.Enqueue(action)
        ///     A -> A: SendUserMessage()
        /// end
        /// \enduml
        /// </remarks>
        static public void RunOnMainThread(Action action)
        {
            if (GetCurrentThreadId() == Process.GetCurrentProcess().Threads[0].Id)
            {
                action();
                return;
            }
            actionQueue.Enqueue(action);
            SendUserMessage();
        }

        /// <summary>
        /// Executes the specified function on the main thread and returns the result.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller Thread" as CT
        /// participant "Main Thread" as MT
        /// CT -> MT : RunOnMainThread(function)
        /// alt Current Thread is Main Thread
        ///     MT -> MT : function()
        /// else Current Thread is not Main Thread
        ///     MT -> MT : Enqueue(function)
        ///     MT -> MT : SendUserMessage()
        ///     MT -> MT : Dequeue()
        /// end
        /// \enduml
        /// </remarks>
        static public T RunOnMainThread<T>(Func<T> function)
        {
            if (GetCurrentThreadId() == Process.GetCurrentProcess().Threads[0].Id)
                return function();

            delegateQueue.Enqueue(function);
            SendUserMessage();
            return (T)returnValueQueue.Dequeue();
        }

        /// <summary>
        /// Handles the window procedure for the specified window.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "WndProc Function" as WndProc
        /// participant "Action Queue" as AQ
        /// participant "Delegate Queue" as DQ
        /// participant "Return Value Queue" as RVQ
        /// participant "Logger" as Log
        /// 
        /// WndProc -> AQ: Dequeue action
        /// loop while actionQueue.Count > 0
        ///     AQ -> WndProc: Invoke action
        /// end
        /// WndProc -> DQ: Dequeue delegate
        /// loop while delegateQueue.Count > 0
        ///     DQ -> WndProc: DynamicInvoke delegate
        ///     WndProc -> RVQ: Enqueue return value
        /// end
        /// WndProc -> WndProc: Return 0
        /// WndProc -> Log: Log exception (if any)
        /// WndProc -> WndProc: CallWindowProc
        /// \enduml
        /// </remarks>
        static int WndProc(IntPtr hWnd, int msg, int wParam, int lParam)
        {
            try
            {
                if (msg != WM_USER) return CallWindowProc(oldCallback, hWnd, msg, wParam, lParam);

                while (actionQueue.Count > 0)
                    actionQueue.Dequeue()?.Invoke();
                while (delegateQueue.Count > 0)
                {
                    var invokeTarget = delegateQueue.Dequeue();
                    returnValueQueue.Enqueue(invokeTarget?.DynamicInvoke());
                }
                return 0;
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }

            return CallWindowProc(oldCallback, hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Finds a window procedure based on the provided window handle and lParam.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// FindWindowProc -> GetWindowThreadProcessId: Get process ID of window
        /// GetWindowThreadProcessId --> FindWindowProc: Return process ID
        /// FindWindowProc -> Process: Get current process ID
        /// Process --> FindWindowProc: Return current process ID
        /// FindWindowProc -> IsWindowVisible: Check if window is visible
        /// IsWindowVisible --> FindWindowProc: Return visibility status
        /// FindWindowProc -> GetWindowTextLength: Get length of window text
        /// GetWindowTextLength --> FindWindowProc: Return text length
        /// FindWindowProc -> StringBuilder: Create new StringBuilder
        /// FindWindowProc -> GetWindowText: Get window text
        /// GetWindowText --> FindWindowProc: Return window text
        /// FindWindowProc -> windowHandle: Assign handle if window text is "World of Warcraft"
        /// \enduml
        /// </remarks>
        static bool FindWindowProc(IntPtr hWnd, IntPtr lParam)
        {
            GetWindowThreadProcessId(hWnd, out int procId);
            if (procId != Process.GetCurrentProcess().Id) return true;
            if (!IsWindowVisible(hWnd)) return true;
            var l = GetWindowTextLength(hWnd);
            if (l == 0) return true;
            var builder = new StringBuilder(l + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            if (builder.ToString() == "World of Warcraft")
                windowHandle = (int)hWnd;
            return true;
        }

        /// <summary>
        /// Sends a user message using the specified window handle and parameters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Program: -> :SendUserMessage(): SendMessage(windowHandle, WM_USER, 0, 0);
        /// \enduml
        /// </remarks>
        static void SendUserMessage() => SendMessage(windowHandle, WM_USER, 0, 0);
    }
}
