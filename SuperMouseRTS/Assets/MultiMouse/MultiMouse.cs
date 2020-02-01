using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MousePointer
{
    public long DeviceId { get; }

    public int PlayerIndex { get; set; }

    public float X { get; set; }
    public float Y { get; set; }

    public Vector3 ScreenPosition { get => new Vector3(X, Camera.main.pixelHeight - Y, 0f); }

    public bool LeftButtonDown { get => ButtonStates[0]; }
    public bool RightButtonDown { get => ButtonStates[1]; }
    public bool MiddleButtonDown { get => ButtonStates[2]; }

    public float Sensitivity { get; set; } = 1.0f;

    public bool[] ButtonStates;

    public MousePointer(long deviceId)
    {
        DeviceId = deviceId;
        ButtonStates = new bool[3];
    }
}

public static class MultiMouseLibrary
{
    /// <summary>
    /// Matching structure for the RawInputEvent that is defined in the C++ libmultimouse library.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputEvent
    {
        /// <summary>
        /// Unique device identifier.
        /// </summary>
        public long devHandle;

        // Delta values for movement
        public int x, y, wheel;

        /// <summary>
        /// 0 = nothing happened, 1 = left pressed, 2 = right pressed, 3 = middle pressed
        /// </summary>
        public byte press;

        /// <summary>
        /// 0 = nothing happened, 1 = left released, 2 = right released, 3 = middle released
        /// </summary>
        public byte release;

        /// <summary>
        /// This is RE_DEVICE_CONNECT, RE_DEVICE_DISCONNECT or RE_MOUSE.
        /// </summary>
        public byte type;
    }

    // Constants for event types
    public const byte RE_DEVICE_CONNECT = 0;
    public const byte RE_DEVICE_DISCONNECT = 1;
    public const byte RE_MOUSE = 2;

    [DllImport("libmultimouse", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool init();

    [DllImport("libmultimouse", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool kill();

    [DllImport("libmultimouse", CallingConvention = CallingConvention.Cdecl)]
    private static extern void poll_start(out ulong arraySize);

    [DllImport("libmultimouse", CallingConvention = CallingConvention.Cdecl)]
    private static extern void get_event_at(ulong index, out RawInputEvent element);

    [DllImport("libmultimouse", CallingConvention = CallingConvention.Cdecl)]
    private static extern void poll_end();

    public static void Init()
    {
        init();
    }

    public static void Kill()
    {
        kill();
    }

    /// <summary>
    /// Enumerates all events that were generated since the last polling.
    /// </summary>
    public static IEnumerable<RawInputEvent> PollEvents()
    {

        poll_start(out ulong arrayLength);

        for (ulong i = 0; i < arrayLength; i++)
        {
            get_event_at(i, out RawInputEvent inputEvent);
            yield return inputEvent;
        }

        poll_end();
    }
}


public class MultiMouse : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateSingleton()
    {
        var gameObject = new GameObject("MultiMouseSingleton");
        Instance = gameObject.AddComponent<MultiMouse>();
        DontDestroyOnLoad(gameObject);
    }

    public static MultiMouse Instance { get; private set; }

    public event Action<long> MousePointerConnected;
    public event Action<long> MousePointerDisconnected;

    public int MousePointerCount { get => mousePointersByDevice.Count; }

    public bool LimitToScreen { get; set; } = true;

    private Dictionary<long, MousePointer> mousePointersByDevice = new Dictionary<long, MousePointer>();
    private Dictionary<int, MousePointer> mousePointersByIndex = new Dictionary<int, MousePointer>();

    public IEnumerable<MousePointer> GetMousePointers()
    {
        foreach (var keyValue in mousePointersByDevice)
        {
            yield return keyValue.Value;
        }
    }

    public MousePointer GetMouseByIndex(int index)
    {
        if (mousePointersByIndex.TryGetValue(index, out MousePointer value))
        {
            return value;
        }
        return null;
    }

    public MousePointer GetMouseByDevice(long device)
    {
        if (mousePointersByDevice.TryGetValue(device, out MousePointer value))
        {
            return value;
        }
        return null;
    }

    private void Start()
    {
        Debug.Log("Screen size (" + Camera.main.pixelWidth + ", " + Camera.main.pixelHeight + ")");
        MultiMouseLibrary.Init();
    }

    private int GetNextFreePlayerIndex()
    {
        int index = 0;
        while (GetMouseByIndex(index) != null)
        {
            index++;
        }
        return index;
    }

    void Update()
    {
        // Mouses get stuck sometimes in unity editor, reinitializing the library fixes that
        if (Input.GetKey(KeyCode.F6))
        {
            MultiMouseLibrary.Init();
        }

        foreach(var ev in MultiMouseLibrary.PollEvents())
        {
            if (ev.type == MultiMouseLibrary.RE_DEVICE_DISCONNECT)
            {
                if (mousePointersByDevice.ContainsKey(ev.devHandle))
                {
                    var pointer = mousePointersByDevice[ev.devHandle];
                    mousePointersByDevice.Remove(ev.devHandle);
                    mousePointersByIndex.Remove(pointer.PlayerIndex);
                    Debug.Log("Removed mouse pointer " + ev.devHandle);
                    MousePointerDisconnected?.Invoke(ev.devHandle);
                }
            }
            else if (ev.type == MultiMouseLibrary.RE_MOUSE)
            {
                if (!mousePointersByDevice.ContainsKey(ev.devHandle))
                {
                    var newPointer = new MousePointer(ev.devHandle);
                    newPointer.PlayerIndex = GetNextFreePlayerIndex();
                    mousePointersByDevice.Add(newPointer.DeviceId, newPointer);
                    mousePointersByIndex.Add(newPointer.PlayerIndex, newPointer);
                    Debug.Log("Added new mouse pointer " + newPointer.DeviceId);
                    MousePointerConnected?.Invoke(newPointer.DeviceId);
                }

                var pointer = mousePointersByDevice[ev.devHandle];
                pointer.X += pointer.Sensitivity * ev.x;
                pointer.Y += pointer.Sensitivity * ev.y;

                if (LimitToScreen)
                {
                    pointer.X = Mathf.Clamp(pointer.X, 0, Camera.main.pixelWidth);
                    pointer.Y = Mathf.Clamp(pointer.Y, 0, Camera.main.pixelHeight);
                }

                if (ev.press > 0 && ev.press <= pointer.ButtonStates.Length)
                {
                    pointer.ButtonStates[ev.press - 1] = true;
                }

                if (ev.release > 0 && ev.release <= pointer.ButtonStates.Length)
                {
                    pointer.ButtonStates[ev.release - 1] = false;
                }
            }

        }
    }

    private void OnApplicationQuit()
    {
        MultiMouseLibrary.Kill();
    }

    private void OnGUI()
    {
        foreach (var pointer in GetMousePointers())
        {
            GUI.Label(new Rect(pointer.X, pointer.Y, 200, 20), "Mouse" + pointer.PlayerIndex + " (" + pointer.X + ", " + pointer.Y + ")");
        }
    }
}
