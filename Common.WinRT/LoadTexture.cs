using SharpDX.Direct3D11;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class LoadTexture
    {
        public static BitmapSource LoadBitmap(ImagingFactory2 factory, string filename)
        {
            var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
                factory,
                filename,
                SharpDX.WIC.DecodeOptions.CacheOnDemand
                );

            var result = new SharpDX.WIC.FormatConverter(factory);

            result.Initialize(
                bitmapDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                SharpDX.WIC.BitmapDitherType.None,
                null,
                0.0,
                SharpDX.WIC.BitmapPaletteType.Custom);

            return result;
        }

        public static Texture2D CreateTexture2DFromBitmap(Device device, BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new SharpDX.Direct3D11.Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription()
                {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Immutable,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }

        private unsafe static Resource LoadDDSFromBuffer(Device device, byte[] buffer, out ShaderResourceView srv)
        {
            Resource result = null;
            srv = null;
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            int size = buffer.Length;

            // If buffer is allocated on Larget Object Heap, then we are going to pin it instead of making a copy.
            if (size > (85 * 1024))
            {
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                DDSHelper.CreateDDSTextureFromMemory(device, handle.AddrOfPinnedObject(), size, out result, out srv);
            }

            fixed (void* pbuffer = buffer)
            {
                DDSHelper.CreateDDSTextureFromMemory(device, (IntPtr)pbuffer, size, out result, out srv);
            }

            return result;
        }

        public static ShaderResourceView SRVFromFile(DeviceManager manager, string fileName)
        {
            ShaderResourceView srv = null;
            using (var texture = LoadFromFile(manager, fileName, out srv))
            { }
            return srv;
        }

        public static Resource LoadFromFile(DeviceManager manager, string fileName, out ShaderResourceView srv)
        {
            if (Path.GetExtension(fileName).ToLower() == ".dds")
            {
                var result = LoadDDSFromBuffer(manager.Direct3DDevice, SharpDX.IO.NativeFile.ReadAllBytes(fileName), out srv);
                return result;
            }
            else
            {
                var bs = LoadBitmap(manager.WICFactory, fileName);
                var texture = CreateTexture2DFromBitmap(manager.Direct3DDevice, bs);
                srv = new ShaderResourceView(manager.Direct3DDevice, texture);
                return texture;
            }
        }

        public static Resource LoadFromFile(DeviceManager manager, string fileName)
        {
            ShaderResourceView srv;
            var texture = LoadFromFile(manager, fileName, out srv);

            if (srv != null)
                srv.Dispose();

            return texture;
        }

    }
}
