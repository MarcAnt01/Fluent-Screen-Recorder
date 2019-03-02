using System;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace CaptureEncoder
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    interface IDirect3DDxgiInterfaceAccess
    {
        uint GetInterface(Guid iid, out IntPtr p);
    }

    public static class Direct3D11Helpers
    {
        public static IDirect3DDevice CreateDevice()
        {
            return CreateDevice(false);
        }

        public static IDirect3DDevice CreateDevice(bool useWARP)
        {
            var d3dDevice = new SharpDX.Direct3D11.Device(
                useWARP ? SharpDX.Direct3D.DriverType.Software : SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);
            return new SharpDXWinRTDevice(d3dDevice.QueryInterface<SharpDX.Direct3D11.Device5>());
        }

        internal static SharpDX.Direct3D11.Device GetSharpDXDevice(IDirect3DDevice device)
        {
            var access = (IDirect3DDxgiInterfaceAccess)device;
            // guid taken from d3d11.h
            var d3dPointer = new IntPtr();
            var result = access.GetInterface(new Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140"), out d3dPointer);
            if (result != 0)
            {
                throw new Exception($"0x{result:X8}");
            }
            var d3dDevice = new SharpDX.Direct3D11.Device(d3dPointer);
            return d3dDevice;
        }

        internal static SharpDX.Direct3D11.Texture2D GetSharpDXTexture2D(IDirect3DSurface surface)
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            // guid taken from d3d11.h
            var d3dPointer = new IntPtr();
            var result = access.GetInterface(new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c"), out d3dPointer);
            if (result != 0)
            {
                throw new Exception($"0x{result:X8}");
            }
            var d3dSurface = new SharpDX.Direct3D11.Texture2D(d3dPointer);
            return d3dSurface;
        }
    }

    class SharpDXWinRTWrapper<T> : IDirect3DDxgiInterfaceAccess where T : SharpDX.ComObject
    {
        public SharpDXWinRTWrapper(T resource)
        {
            _resource = resource;
        }

        public void Dispose()
        {
            _resource?.Dispose();
            _resource = null;
        }

        public uint GetInterface(Guid iid, out IntPtr p)
        {
            CheckClosed();

            try
            {
                _resource.QueryInterface(iid, out p);
            }
            catch (Exception ex)
            {
                p = new IntPtr();
                return (uint)ex.HResult;
            }
            
            return 0;
        }

        protected void CheckClosed()
        {
            if (_resource == null)
            {
                throw new ObjectDisposedException(nameof(SharpDXWinRTDevice));
            }
        }

        protected T _resource;
    }


    class SharpDXWinRTDevice : SharpDXWinRTWrapper<SharpDX.Direct3D11.Device5>, IDirect3DDevice
    {
        public SharpDXWinRTDevice(SharpDX.Direct3D11.Device5 device) : base(device)
        {
        }

        public void Trim()
        {
            CheckClosed();

            var dxgiDevice = _resource.QueryInterface<SharpDX.DXGI.Device3>();
            dxgiDevice.Trim();
        }
    }

    class SharpDXWinRTSurface : SharpDXWinRTWrapper<SharpDX.Direct3D11.Texture2D>, IDirect3DSurface
    {
        public SharpDXWinRTSurface(SharpDX.Direct3D11.Texture2D surface) : base(surface)
        {
            var sharpDXDescription = _resource.Description;
            Description = new Direct3DSurfaceDescription()
            {
                Width = sharpDXDescription.Width,
                Height = sharpDXDescription.Height,
                Format = (DirectXPixelFormat)sharpDXDescription.Format,
                MultisampleDescription = new Direct3DMultisampleDescription()
                {
                    Count = sharpDXDescription.SampleDescription.Count,
                    Quality = sharpDXDescription.SampleDescription.Quality
                }
            };
        }

        public Direct3DSurfaceDescription Description { get; }
    }
}
