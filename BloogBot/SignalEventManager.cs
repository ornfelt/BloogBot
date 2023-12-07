using BloogBot.Game;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// This class handles the management of signal events.
/// </summary>
namespace BloogBot
{
    /// <summary>
    /// Represents a class that manages signal events.
    /// </summary>
    /// <summary>
    /// Represents a class that manages signal events.
    /// </summary>
    public class SignalEventManager
    {
        /// <summary>
        /// Represents a delegate that handles signal events.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="format">The format of the event.</param>
        /// <param name="firstArgPtr">The pointer to the first argument.</param>
        delegate void SignalEventDelegate(string eventName, string format, uint firstArgPtr);
        /// <summary>
        /// Represents a delegate that is used to signal an event with no arguments.
        /// </summary>
        delegate void SignalEventNoArgsDelegate(string eventName);

        /// <summary>
        /// Initializes the SignalEventManager class.
        /// </summary>
        static SignalEventManager()
        {
            //InitializeSignalEventHook();
            //InitializeSignalEventHookNoArgs();
        }

        /// <summary>
        /// Initializes the signal event hook.
        /// </summary>
        #region InitializeSignalEventHook
        static SignalEventDelegate signalEventDelegate;

        /// <summary>
        /// Initializes the signal event hook.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "InitializeSignalEventHook()" as A
        /// participant "SignalEventDelegate" as B
        /// participant "Marshal" as C
        /// participant "MemoryManager" as D
        /// A -> B: Create new SignalEventDelegate
        /// A -> C: Get function pointer for delegate
        /// A -> D: Inject assembly "SignalEventDetour"
        /// A -> D: Inject assembly "SignalEventHook"
        /// \enduml
        /// </remarks>
        static void InitializeSignalEventHook()
        {
            signalEventDelegate = new SignalEventDelegate(SignalEventHook);
            var addrToDetour = Marshal.GetFunctionPointerForDelegate(signalEventDelegate);

            var instructions = new[]
            {
                "push ebx",
                "push esi",
                "call 0x007040D0",
                "pushfd",
                "pushad",
                "mov eax, ebp",
                "add eax, 0x10",
                "push eax",
                "mov eax, [ebp + 0xC]",
                "push eax",
                "mov edi, [edi]",
                "push edi",
                $"call 0x{((uint) addrToDetour).ToString("X")}",
                "popad",
                "popfd",
                $"jmp 0x{((uint) (MemoryAddresses.SignalEventFunPtr + 7)).ToString("X")}"
            };
            var signalEventDetour = MemoryManager.InjectAssembly("SignalEventDetour", instructions);
            MemoryManager.InjectAssembly("SignalEventHook", (uint)MemoryAddresses.SignalEventFunPtr, "jmp " + signalEventDetour);
        }

        /// <summary>
        /// Signals an event hook with the specified event name, types argument, and first argument pointer.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// SignalEventHook -> Logger : LogVerbose(eventName)
        /// SignalEventHook -> MemoryManager : ReadInt((IntPtr)tmpPtr)
        /// MemoryManager --> SignalEventHook : return ptr
        /// SignalEventHook -> MemoryManager : ReadString((IntPtr)ptr)
        /// MemoryManager --> SignalEventHook : return str
        /// SignalEventHook -> Logger : LogVerbose(str)
        /// SignalEventHook -> MemoryManager : ReadFloat((IntPtr)tmpPtr)
        /// MemoryManager --> SignalEventHook : return val
        /// SignalEventHook -> Logger : LogVerbose(val)
        /// SignalEventHook -> MemoryManager : ReadUint((IntPtr)tmpPtr)
        /// MemoryManager --> SignalEventHook : return val
        /// SignalEventHook -> Logger : LogVerbose(val)
        /// SignalEventHook -> MemoryManager : ReadInt((IntPtr)tmpPtr)
        /// MemoryManager --> SignalEventHook : return val
        /// SignalEventHook -> Logger : LogVerbose(val)
        /// SignalEventHook -> MemoryManager : ReadInt((IntPtr)tmpPtr)
        /// MemoryManager --> SignalEventHook : return val
        /// SignalEventHook -> Logger : LogVerbose(val)
        /// SignalEventHook -> Logger : LogVerbose("")
        /// SignalEventHook -> OnNewEventSignalEvent : eventName, list
        /// \enduml
        /// </remarks>
        static void SignalEventHook(string eventName, string typesArg, uint firstArgPtr)
        {
            Logger.LogVerbose(eventName);

            var types = typesArg.TrimStart('%').Split('%');
            var list = new object[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                var tmpPtr = firstArgPtr + (uint)i * 4;
                if (types[i] == "s")
                {
                    var ptr = MemoryManager.ReadInt((IntPtr)tmpPtr);
                    var str = MemoryManager.ReadString((IntPtr)ptr);
                    if (!string.IsNullOrWhiteSpace(str))
                        Logger.LogVerbose(str);
                    else
                        Logger.LogVerbose("null");
                    list[i] = str;
                }
                else if (types[i] == "f")
                {
                    var val = MemoryManager.ReadFloat((IntPtr)tmpPtr);
                    Logger.LogVerbose(val);
                    list[i] = val;
                }
                else if (types[i] == "u")
                {
                    var val = MemoryManager.ReadUint((IntPtr)tmpPtr);
                    Logger.LogVerbose(val);
                    list[i] = val;
                }
                else if (types[i] == "d")
                {
                    var val = MemoryManager.ReadInt((IntPtr)tmpPtr);
                    Logger.LogVerbose(val);
                    list[i] = val;
                }
                else if (types[i] == "b")
                {
                    var val = MemoryManager.ReadInt((IntPtr)tmpPtr);
                    Logger.LogVerbose(val);
                    list[i] = Convert.ToBoolean(val);
                }
            }

            Logger.LogVerbose("");

            OnNewEventSignalEvent(eventName, list);
        }

        /// <summary>
        /// Invokes the OnNewSignalEvent event with the specified event name and list of parameters.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// :Caller: -> OnNewEventSignalEvent: parEvent, parList
        /// OnNewEventSignalEvent -> OnNewSignalEvent: Invoke(parEvent, parList)
        /// \enduml
        /// </remarks>
        static internal void OnNewEventSignalEvent(string parEvent, params object[] parList) =>
                            OnNewSignalEvent?.Invoke(parEvent, parList);

        /// <summary>
        /// Represents a delegate that handles signal events.
        /// </summary>
        internal delegate void SignalEventEventHandler(string parEvent, params object[] parArgs);

        /// <summary>
        /// Event that is triggered when a new signal event occurs.
        /// </summary>
        internal static event SignalEventEventHandler OnNewSignalEvent;
        /// <summary>
        /// Initializes the signal event hook with no arguments.
        /// </summary>
        #endregion

        #region InitializeSignalEventHookNoArgs
        static SignalEventNoArgsDelegate signalEventNoArgsDelegate;

        /// <summary>
        /// Initializes the signal event hook with no arguments.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "InitializeSignalEventHookNoArgs()" as A
        /// participant "SignalEventNoArgsDelegate" as B
        /// participant "Marshal" as C
        /// participant "MemoryManager" as D
        /// A -> B: Create new SignalEventNoArgsDelegate
        /// A -> C: GetFunctionPointerForDelegate(signalEventNoArgsDelegate)
        /// A -> D: InjectAssembly("SignalEventNoArgsDetour", instructions)
        /// A -> D: InjectAssembly("SignalEventNoArgsHook", (uint)MemoryAddresses.SignalEventNoParamsFunPtr, "jmp " + signalEventNoArgsDetour)
        /// \enduml
        /// </remarks>
        static void InitializeSignalEventHookNoArgs()
        {
            signalEventNoArgsDelegate = new SignalEventNoArgsDelegate(SignalEventNoArgsHook);
            var addrToDetour = Marshal.GetFunctionPointerForDelegate(signalEventNoArgsDelegate);

            var instructions = new[]
            {
                "push esi",
                "call 0x007040D0",
                "pushfd",
                "pushad",
                "mov edi, [edi]",
                "push edi",
                $"call 0x{((uint) addrToDetour).ToString("X")}",
                "popad",
                "popfd",
                $"jmp 0x{((uint) MemoryAddresses.SignalEventNoParamsFunPtr + 6).ToString("X")}"
            };
            var signalEventNoArgsDetour = MemoryManager.InjectAssembly("SignalEventNoArgsDetour", instructions);
            MemoryManager.InjectAssembly("SignalEventNoArgsHook", (uint)MemoryAddresses.SignalEventNoParamsFunPtr, "jmp " + signalEventNoArgsDetour);
        }

        /// <summary>
        /// Signals an event with no arguments and invokes the corresponding event handler.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// participant "Caller" as C
        /// participant "SignalEventNoArgsHook" as S
        /// participant "Logger" as L
        /// participant "OnNewSignalEventNoArgs" as O
        /// 
        /// C -> S: Call(eventName)
        /// S -> L: LogVerbose(eventName)
        /// S -> O: Invoke(eventName)
        /// \enduml
        /// </remarks>
        static void SignalEventNoArgsHook(string eventName)
        {
            Logger.LogVerbose(eventName + "\n");
            OnNewSignalEventNoArgs?.Invoke(eventName);
        }

        /// <summary>
        /// Represents a delegate that handles an event with no arguments and returns no value.
        /// </summary>
        internal delegate void SignalEventNoArgsEventHandler(string parEvent, params object[] parArgs);

        /// <summary>
        /// Event that is triggered when a new signal event with no arguments occurs.
        /// </summary>
        internal static event SignalEventNoArgsEventHandler OnNewSignalEventNoArgs;
        #endregion
    }
}
