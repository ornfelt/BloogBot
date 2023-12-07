using BloogBot.Game.Enums;
using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

/// <summary>
/// The BloogBot.Game.Objects namespace contains classes for handling World of Warcraft objects.
/// </summary>
namespace BloogBot.Game.Objects
{
    /// <summary>
    /// Represents a World of Warcraft object.
    /// </summary>
    /// <summary>
    /// Represents a World of Warcraft object.
    /// </summary>
    public unsafe abstract class WoWObject
    {
        /// <summary>
        /// Gets or sets the pointer.
        /// </summary>
        public virtual IntPtr Pointer { get; set; }
        /// <summary>
        /// Gets or sets the Guid.
        /// </summary>
        public virtual ulong Guid { get; set; }
        /// <summary>
        /// Gets or sets the type of the object.
        /// </summary>
        public virtual ObjectType ObjectType { get; set; }

        /// <summary>
        /// Represents a delegate for getting the position of an object.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void GetPositionDelegate(IntPtr objectPtr, ref XYZ pos);

        /// <summary>
        /// Represents a delegate used to get the facing of an object.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate float GetFacingDelegate(IntPtr objectPtr);

        /// <summary>
        /// Delegate used for interacting with objects in vanilla.
        /// </summary>
        // used for interacting in vanilla
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void RightClickObjectDelegate(IntPtr unitPtr, int autoLoot);

        /// <summary>
        /// Delegate used for interacting with others.
        /// </summary>
        // used for interacting in others
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate void InteractDelegate(IntPtr objectPtr);

        /// <summary>
        /// Represents a delegate that is used to get the name of an object pointer.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        delegate IntPtr GetNameDelegate(IntPtr objectPtr);

        /// <summary>
        /// Gets the position delegate.
        /// </summary>
        readonly GetPositionDelegate getPositionFunction;

        /// <summary>
        /// Gets the facing delegate.
        /// </summary>
        readonly GetFacingDelegate getFacingFunction;

        /// <summary>
        /// Delegate for handling right-click events on objects.
        /// </summary>
        readonly RightClickObjectDelegate rightClickObjectFunction;

        /// <summary>
        /// Gets or sets the readonly InteractDelegate function.
        /// </summary>
        readonly InteractDelegate interactFunction;

        /// <summary>
        /// Gets the name delegate.
        /// </summary>
        readonly GetNameDelegate getNameFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="WoWObject"/> class.
        /// </summary>
        public WoWObject() { }

        /// <summary>
        /// Initializes a new instance of the WoWObject class with the specified pointer, GUID, and object type.
        /// </summary>
        public WoWObject(IntPtr pointer, ulong guid, ObjectType objectType)
        {
            Pointer = pointer;
            Guid = guid;
            ObjectType = objectType;

            var vTableAddr = MemoryManager.ReadIntPtr(pointer);

            // TODO: I can't figure out how to get the vtable addresses for the Vanilla client (or if they even exist) so we do it this way for now
            if (ClientHelper.ClientVersion != ClientVersion.Vanilla)
            {
                var getPositionAddr = IntPtr.Add(vTableAddr, MemoryAddresses.WoWObject_GetPositionFunOffset);
                var getPositionFunPtr = MemoryManager.ReadIntPtr(getPositionAddr);
                getPositionFunction = Marshal.GetDelegateForFunctionPointer<GetPositionDelegate>(getPositionFunPtr);

                var getFacingAddr = IntPtr.Add(vTableAddr, MemoryAddresses.WoWObject_GetFacingFunOffset);
                var getFacingFunPtr = MemoryManager.ReadIntPtr(getFacingAddr);
                getFacingFunction = Marshal.GetDelegateForFunctionPointer<GetFacingDelegate>(getFacingFunPtr);

                var interactAddr = IntPtr.Add(vTableAddr, MemoryAddresses.WoWObject_InteractFunOffset);
                var interactFunPtr = MemoryManager.ReadIntPtr(interactAddr);
                interactFunction = Marshal.GetDelegateForFunctionPointer<InteractDelegate>(interactFunPtr);

                var getNameAddr = IntPtr.Add(vTableAddr, MemoryAddresses.WoWObject_GetNameFunOffset);
                var getNameFunPtr = MemoryManager.ReadIntPtr(getNameAddr);
                getNameFunction = Marshal.GetDelegateForFunctionPointer<GetNameDelegate>(getNameFunPtr);
            }
            else
            {
                rightClickObjectFunction = Marshal.GetDelegateForFunctionPointer<RightClickObjectDelegate>((IntPtr)0x60BEA0);
            }
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        public virtual Position Position => GetPosition();

        /// <summary>
        /// Retrieves the position of the object.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// autonumber
        /// ClientHelper -> MemoryManager: ClientVersion
        /// MemoryManager --> ClientHelper: Return ClientVersion
        /// ClientHelper -> GetPosition: Check ClientVersion
        /// GetPosition -> MemoryManager: ReadFloat
        /// MemoryManager --> GetPosition: Return float values
        /// GetPosition -> Position: Create new Position
        /// Position --> GetPosition: Return Position
        /// GetPosition -> MemoryManager: ReadInt
        /// MemoryManager --> GetPosition: Return int value
        /// GetPosition -> MemoryManager: ReadFloat
        /// MemoryManager --> GetPosition: Return float values
        /// GetPosition -> Position: Create new Position
        /// Position --> GetPosition: Return Position
        /// GetPosition -> MemoryManager: ReadInt
        /// MemoryManager --> GetPosition: Return int value
        /// GetPosition -> MemoryManager: ReadFloat
        /// MemoryManager --> GetPosition: Return float values
        /// GetPosition -> Position: Create new Position
        /// Position --> GetPosition: Return Position
        /// GetPosition -> MemoryManager: ReadFloat
        /// MemoryManager --> GetPosition: Return float values
        /// GetPosition -> Position: Create new Position
        /// Position --> GetPosition: Return Position
        /// GetPosition -> XYZ: Create new XYZ
        /// GetPosition -> getPositionFunction: Call getPositionFunction
        /// getPositionFunction --> GetPosition: Return Position
        /// GetPosition -> Position: Create new Position
        /// Position --> GetPosition: Return Position
        /// GetPosition -> Position: Create new Position
        /// Position --> GetPosition: Return Position
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        Position GetPosition()
        {
            try
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    if (ObjectType == ObjectType.Unit || ObjectType == ObjectType.Player)
                    {
                        var x = MemoryManager.ReadFloat(IntPtr.Add(Pointer, 0x9B8));
                        var y = MemoryManager.ReadFloat(IntPtr.Add(Pointer, 0x9BC));
                        var z = MemoryManager.ReadFloat(IntPtr.Add(Pointer, 0x9C0));

                        return new Position(x, y, z);
                    }
                    else
                    {
                        float x;
                        float y;
                        float z;
                        if (MemoryManager.ReadInt(GetDescriptorPtr() + 0x54) == 3)
                        {
                            x = MemoryManager.ReadFloat(GetDescriptorPtr() + 0x3C);
                            y = MemoryManager.ReadFloat(GetDescriptorPtr() + (0x3C + 4));
                            z = MemoryManager.ReadFloat(GetDescriptorPtr() + (0x3C + 8));
                            return new Position(x, y, z);
                        }
                        var v2 = MemoryManager.ReadInt(IntPtr.Add(Pointer, 0x210));
                        IntPtr xyzStruct;
                        if (v2 != 0)
                        {
                            var underlyingFuncPtr = MemoryManager.ReadInt(IntPtr.Add(MemoryManager.ReadIntPtr((IntPtr)v2), 0x44));
                            switch (underlyingFuncPtr)
                            {
                                case 0x005F5C10:
                                    x = MemoryManager.ReadFloat((IntPtr)(v2 + 0x2c));
                                    y = MemoryManager.ReadFloat((IntPtr)(v2 + 0x2c + 0x4));
                                    z = MemoryManager.ReadFloat((IntPtr)(v2 + 0x2c + 0x8));
                                    return new Position(x, y, z);
                                case 0x005F3690:
                                    v2 = (int)IntPtr.Add(MemoryManager.ReadIntPtr(IntPtr.Add(MemoryManager.ReadIntPtr((IntPtr)(v2 + 0x4)), 0x110)), 0x24);
                                    x = MemoryManager.ReadFloat((IntPtr)v2);
                                    y = MemoryManager.ReadFloat((IntPtr)(v2 + 0x4));
                                    z = MemoryManager.ReadFloat((IntPtr)(v2 + 0x8));
                                    return new Position(x, y, z);
                            }
                            xyzStruct = (IntPtr)(v2 + 0x44);
                        }
                        else
                        {
                            xyzStruct = IntPtr.Add(MemoryManager.ReadIntPtr(IntPtr.Add(Pointer, 0x110)), 0x24);
                        }
                        x = MemoryManager.ReadFloat(xyzStruct);
                        y = MemoryManager.ReadFloat(IntPtr.Add(xyzStruct, 0x4));
                        z = MemoryManager.ReadFloat(IntPtr.Add(xyzStruct, 0x8));
                        return new Position(x, y, z);
                    }
                }
                else
                {
                    var xyz = new XYZ();
                    getPositionFunction(Pointer, ref xyz);

                    return new Position(xyz);
                }

            }
            catch (AccessViolationException)
            {
                //Console.WriteLine("Access violation on WoWObject.Position. Swallowing.");
                return new Position(0, 0, 0);
            }
        }

        /// <summary>
        /// Gets the facing value.
        /// </summary>
        public float Facing => GetFacing();

        /// <summary>
        /// Retrieves the facing angle of the WoWObject.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ClientHelper -> GetFacing: ClientVersion
        /// GetFacing -> MemoryManager: ReadFloat
        /// GetFacing -> GetFacing: getFacingFunction
        /// GetFacing -> GetFacing: AccessViolationException
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        float GetFacing()
        {
            try
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    if (ObjectType == ObjectType.Player || ObjectType == ObjectType.Unit)
                    {
                        return MemoryManager.ReadFloat(Pointer + 0x9C4);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return getFacingFunction(Pointer);
                }
            }
            catch (AccessViolationException)
            {
                //Console.WriteLine("Access violation on WoWObject.Facing. Swallowing.");
                return 0;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name => GetName();

        /// <summary>
        /// Retrieves the name of the WoWObject.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// ClientHelper -> GetName: ClientVersion
        /// GetName -> MemoryManager: ReadIntPtr
        /// MemoryManager --> GetName: namePtr
        /// GetName -> MemoryManager: ReadUlong
        /// MemoryManager --> GetName: nextGuid
        /// GetName -> MemoryManager: ReadIntPtr
        /// MemoryManager --> GetName: namePtr
        /// GetName -> MemoryManager: ReadString
        /// MemoryManager --> GetName: return
        /// GetName -> MemoryManager: ReadInt
        /// MemoryManager --> GetName: ptr1
        /// GetName -> MemoryManager: ReadInt
        /// MemoryManager --> GetName: ptr2
        /// GetName -> MemoryManager: ReadString
        /// MemoryManager --> GetName: return
        /// GetName -> MemoryManager: ReadIntPtr
        /// MemoryManager --> GetName: ptr1
        /// GetName -> MemoryManager: ReadIntPtr
        /// MemoryManager --> GetName: ptr2
        /// GetName -> MemoryManager: ReadString
        /// MemoryManager --> GetName: return
        /// GetName -> getNameFunction: Pointer
        /// getNameFunction --> GetName: ptr
        /// GetName -> MemoryManager: ReadString
        /// MemoryManager --> GetName: return
        /// GetName -> MemoryManager: ReadString
        /// MemoryManager --> GetName: return
        /// GetName --> ClientHelper: ""
        /// GetName --> ClientHelper: ""
        /// \enduml
        /// </remarks>
        [HandleProcessCorruptedStateExceptions]
        string GetName()
        {
            try
            {
                if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
                {
                    if (ObjectType == ObjectType.Player)
                    {
                        var namePtr = MemoryManager.ReadIntPtr((IntPtr)0xC0E230);
                        while (true)
                        {
                            var nextGuid = MemoryManager.ReadUlong(IntPtr.Add(namePtr, 0xC));

                            if (nextGuid != Guid)
                                namePtr = MemoryManager.ReadIntPtr(namePtr);
                            else
                                break;
                        }
                        return MemoryManager.ReadString(IntPtr.Add(namePtr, 0x14));
                    }
                    else if (ObjectType == ObjectType.Unit)
                    {
                        var ptr1 = MemoryManager.ReadInt(IntPtr.Add(Pointer, 0xB30));
                        var ptr2 = MemoryManager.ReadInt((IntPtr)ptr1);
                        return MemoryManager.ReadString((IntPtr)ptr2);
                    }
                    else
                    {
                        var ptr1 = MemoryManager.ReadIntPtr(IntPtr.Add(Pointer, 0x214));
                        var ptr2 = MemoryManager.ReadIntPtr(IntPtr.Add(ptr1, 0x8));
                        return MemoryManager.ReadString(ptr2);
                    }
                }
                else
                {
                    var ptr = getNameFunction(Pointer);

                    if (ptr == null)
                        return MemoryManager.ReadString(ptr);
                    if (ptr != IntPtr.Zero)
                        return MemoryManager.ReadString(ptr);
                    else
                        return "";
                }

            }
            catch (AccessViolationException)
            {
                //Console.WriteLine("Access violation on WoWObject.Name. Swallowing.");
                return "";
            }
        }

        /// <summary>
        /// Interacts with an object based on the client version.
        /// </summary>
        /// <remarks>
        /// \startuml
        /// Interact -> ClientHelper: Check ClientVersion
        /// alt ClientVersion.Vanilla
        ///   Interact -> rightClickObjectFunction: Call with Pointer, 0
        /// else
        ///   Interact -> interactFunction: Call with Pointer
        /// end
        /// \enduml
        /// </remarks>
        public void Interact()
        {
            if (ClientHelper.ClientVersion == ClientVersion.Vanilla)
            {
                rightClickObjectFunction(Pointer, 0);
            }
            else
            {
                interactFunction(Pointer);
            }
        }

        /// <summary>
        /// Retrieves the pointer to the descriptor of the WoW object by reading the IntPtr value from the memory address obtained by adding the offset of the WoWObject_DescriptorOffset to the current pointer.
        /// </summary>
        protected IntPtr GetDescriptorPtr() => MemoryManager.ReadIntPtr(IntPtr.Add(Pointer, MemoryAddresses.WoWObject_DescriptorOffset));
    }
}
