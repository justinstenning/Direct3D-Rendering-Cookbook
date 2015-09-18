using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class CopyTexture
    {
        public static void CopyToTexture(DeviceContext1 context, Texture2D source, Texture2D destination, int subResource = 0)
        {
            if (source.Description.SampleDescription.Count > 1 || source.Description.SampleDescription.Quality > 0)
            {
                context.ResolveSubresource(source, subResource, destination, 0, destination.Description.Format);
            }
            else
            {
                // Not multisampled, so just copy to the destination
                context.CopySubresourceRegion(source, subResource, null, destination, 0);
                //context.CopyResource(source, destination);
            }
        }

        public static Guid PixelFormatFromFormat(SharpDX.DXGI.Format format)
        {
            switch (format)
            {
                case SharpDX.DXGI.Format.R32G32B32A32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                    return SharpDX.WIC.PixelFormat.Format128bppRGBAFloat;
                case SharpDX.DXGI.Format.R32G32B32A32_UInt:
                case SharpDX.DXGI.Format.R32G32B32A32_SInt:
                    return SharpDX.WIC.PixelFormat.Format128bppRGBAFixedPoint;
                case SharpDX.DXGI.Format.R32G32B32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32_Float:
                    return SharpDX.WIC.PixelFormat.Format96bppRGBFloat;
                case SharpDX.DXGI.Format.R32G32B32_UInt:
                case SharpDX.DXGI.Format.R32G32B32_SInt:
                    return SharpDX.WIC.PixelFormat.Format96bppRGBFixedPoint;
                case SharpDX.DXGI.Format.R16G16B16A16_Typeless:
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_UInt:
                case SharpDX.DXGI.Format.R16G16B16A16_SNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_SInt:
                    return SharpDX.WIC.PixelFormat.Format64bppRGBA;
                case SharpDX.DXGI.Format.R32G32_Typeless:
                case SharpDX.DXGI.Format.R32G32_Float:
                case SharpDX.DXGI.Format.R32G32_UInt:
                case SharpDX.DXGI.Format.R32G32_SInt:
                case SharpDX.DXGI.Format.R32G8X24_Typeless:
                case SharpDX.DXGI.Format.D32_Float_S8X24_UInt:
                case SharpDX.DXGI.Format.R32_Float_X8X24_Typeless:
                case SharpDX.DXGI.Format.X32_Typeless_G8X24_UInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R10G10B10A2_Typeless:
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                case SharpDX.DXGI.Format.R10G10B10A2_UInt:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA1010102;
                case SharpDX.DXGI.Format.R11G11B10_Float:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R8G8B8A8_Typeless:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.R8G8B8A8_UInt:
                case SharpDX.DXGI.Format.R8G8B8A8_SNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_SInt:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA;
                case SharpDX.DXGI.Format.R16G16_Typeless:
                case SharpDX.DXGI.Format.R16G16_Float:
                case SharpDX.DXGI.Format.R16G16_UNorm:
                case SharpDX.DXGI.Format.R16G16_UInt:
                case SharpDX.DXGI.Format.R16G16_SNorm:
                case SharpDX.DXGI.Format.R16G16_SInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R32_Typeless:
                case SharpDX.DXGI.Format.D32_Float:
                case SharpDX.DXGI.Format.R32_Float:
                case SharpDX.DXGI.Format.R32_UInt:
                case SharpDX.DXGI.Format.R32_SInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R24G8_Typeless:
                case SharpDX.DXGI.Format.D24_UNorm_S8_UInt:
                case SharpDX.DXGI.Format.R24_UNorm_X8_Typeless:
                    return SharpDX.WIC.PixelFormat.Format32bppGrayFloat;
                case SharpDX.DXGI.Format.X24_Typeless_G8_UInt:
                case SharpDX.DXGI.Format.R9G9B9E5_Sharedexp:
                case SharpDX.DXGI.Format.R8G8_B8G8_UNorm:
                case SharpDX.DXGI.Format.G8R8_G8B8_UNorm:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                case SharpDX.DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm:
                    return SharpDX.WIC.PixelFormat.Format32bppBGR101010;
                case SharpDX.DXGI.Format.B8G8R8A8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8X8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                case SharpDX.DXGI.Format.R8G8_Typeless:
                case SharpDX.DXGI.Format.R8G8_UNorm:
                case SharpDX.DXGI.Format.R8G8_UInt:
                case SharpDX.DXGI.Format.R8G8_SNorm:
                case SharpDX.DXGI.Format.R8G8_SInt:
                    return Guid.Empty;
                case SharpDX.DXGI.Format.R16_Typeless:
                case SharpDX.DXGI.Format.R16_Float:
                case SharpDX.DXGI.Format.D16_UNorm:
                case SharpDX.DXGI.Format.R16_UNorm:
                case SharpDX.DXGI.Format.R16_SNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppGrayHalf;
                case SharpDX.DXGI.Format.R16_UInt:
                case SharpDX.DXGI.Format.R16_SInt:
                    return SharpDX.WIC.PixelFormat.Format16bppGrayFixedPoint;
                case SharpDX.DXGI.Format.B5G6R5_UNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppBGR565;
                case SharpDX.DXGI.Format.B5G5R5A1_UNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppBGRA5551;
                case SharpDX.DXGI.Format.B4G4R4A4_UNorm:
                    return Guid.Empty;

                case SharpDX.DXGI.Format.R8_Typeless:
                case SharpDX.DXGI.Format.R8_UNorm:
                case SharpDX.DXGI.Format.R8_UInt:
                case SharpDX.DXGI.Format.R8_SNorm:
                case SharpDX.DXGI.Format.R8_SInt:
                    return SharpDX.WIC.PixelFormat.Format8bppGray;
                case SharpDX.DXGI.Format.A8_UNorm:
                    return SharpDX.WIC.PixelFormat.Format8bppAlpha;
                case SharpDX.DXGI.Format.R1_UNorm:
                    return SharpDX.WIC.PixelFormat.Format1bppIndexed;

                default:
                    return Guid.Empty;
            }
        }

        static int BitsPerPixel(SharpDX.DXGI.Format fmt)
        {
            switch (fmt)
            {
                case SharpDX.DXGI.Format.R32G32B32A32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                case SharpDX.DXGI.Format.R32G32B32A32_UInt:
                case SharpDX.DXGI.Format.R32G32B32A32_SInt:
                    return 128;

                case SharpDX.DXGI.Format.R32G32B32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32_Float:
                case SharpDX.DXGI.Format.R32G32B32_UInt:
                case SharpDX.DXGI.Format.R32G32B32_SInt:
                    return 96;

                case SharpDX.DXGI.Format.R16G16B16A16_Typeless:
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_UInt:
                case SharpDX.DXGI.Format.R16G16B16A16_SNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_SInt:
                case SharpDX.DXGI.Format.R32G32_Typeless:
                case SharpDX.DXGI.Format.R32G32_Float:
                case SharpDX.DXGI.Format.R32G32_UInt:
                case SharpDX.DXGI.Format.R32G32_SInt:
                case SharpDX.DXGI.Format.R32G8X24_Typeless:
                case SharpDX.DXGI.Format.D32_Float_S8X24_UInt:
                case SharpDX.DXGI.Format.R32_Float_X8X24_Typeless:
                case SharpDX.DXGI.Format.X32_Typeless_G8X24_UInt:
                    return 64;

                case SharpDX.DXGI.Format.R10G10B10A2_Typeless:
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                case SharpDX.DXGI.Format.R10G10B10A2_UInt:
                case SharpDX.DXGI.Format.R11G11B10_Float:
                case SharpDX.DXGI.Format.R8G8B8A8_Typeless:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.R8G8B8A8_UInt:
                case SharpDX.DXGI.Format.R8G8B8A8_SNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_SInt:
                case SharpDX.DXGI.Format.R16G16_Typeless:
                case SharpDX.DXGI.Format.R16G16_Float:
                case SharpDX.DXGI.Format.R16G16_UNorm:
                case SharpDX.DXGI.Format.R16G16_UInt:
                case SharpDX.DXGI.Format.R16G16_SNorm:
                case SharpDX.DXGI.Format.R16G16_SInt:
                case SharpDX.DXGI.Format.R32_Typeless:
                case SharpDX.DXGI.Format.D32_Float:
                case SharpDX.DXGI.Format.R32_Float:
                case SharpDX.DXGI.Format.R32_UInt:
                case SharpDX.DXGI.Format.R32_SInt:
                case SharpDX.DXGI.Format.R24G8_Typeless:
                case SharpDX.DXGI.Format.D24_UNorm_S8_UInt:
                case SharpDX.DXGI.Format.R24_UNorm_X8_Typeless:
                case SharpDX.DXGI.Format.X24_Typeless_G8_UInt:
                case SharpDX.DXGI.Format.R9G9B9E5_Sharedexp:
                case SharpDX.DXGI.Format.R8G8_B8G8_UNorm:
                case SharpDX.DXGI.Format.G8R8_G8B8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm:
                case SharpDX.DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm:
                case SharpDX.DXGI.Format.B8G8R8A8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8X8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb:
                    return 32;

                case SharpDX.DXGI.Format.R8G8_Typeless:
                case SharpDX.DXGI.Format.R8G8_UNorm:
                case SharpDX.DXGI.Format.R8G8_UInt:
                case SharpDX.DXGI.Format.R8G8_SNorm:
                case SharpDX.DXGI.Format.R8G8_SInt:
                case SharpDX.DXGI.Format.R16_Typeless:
                case SharpDX.DXGI.Format.R16_Float:
                case SharpDX.DXGI.Format.D16_UNorm:
                case SharpDX.DXGI.Format.R16_UNorm:
                case SharpDX.DXGI.Format.R16_UInt:
                case SharpDX.DXGI.Format.R16_SNorm:
                case SharpDX.DXGI.Format.R16_SInt:
                case SharpDX.DXGI.Format.B5G6R5_UNorm:
                case SharpDX.DXGI.Format.B5G5R5A1_UNorm:
                case SharpDX.DXGI.Format.B4G4R4A4_UNorm:
                    return 16;

                case SharpDX.DXGI.Format.R8_Typeless:
                case SharpDX.DXGI.Format.R8_UNorm:
                case SharpDX.DXGI.Format.R8_UInt:
                case SharpDX.DXGI.Format.R8_SNorm:
                case SharpDX.DXGI.Format.R8_SInt:
                case SharpDX.DXGI.Format.A8_UNorm:
                    return 8;

                case SharpDX.DXGI.Format.R1_UNorm:
                    return 1;

                case SharpDX.DXGI.Format.BC1_Typeless:
                case SharpDX.DXGI.Format.BC1_UNorm:
                case SharpDX.DXGI.Format.BC1_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC4_Typeless:
                case SharpDX.DXGI.Format.BC4_UNorm:
                case SharpDX.DXGI.Format.BC4_SNorm:
                    return 4;

                case SharpDX.DXGI.Format.BC2_Typeless:
                case SharpDX.DXGI.Format.BC2_UNorm:
                case SharpDX.DXGI.Format.BC2_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC3_Typeless:
                case SharpDX.DXGI.Format.BC3_UNorm:
                case SharpDX.DXGI.Format.BC3_UNorm_SRgb:
                case SharpDX.DXGI.Format.BC5_Typeless:
                case SharpDX.DXGI.Format.BC5_UNorm:
                case SharpDX.DXGI.Format.BC5_SNorm:
                case SharpDX.DXGI.Format.BC6H_Typeless:
                case SharpDX.DXGI.Format.BC6H_Uf16:
                case SharpDX.DXGI.Format.BC6H_Sf16:
                case SharpDX.DXGI.Format.BC7_Typeless:
                case SharpDX.DXGI.Format.BC7_UNorm:
                case SharpDX.DXGI.Format.BC7_UNorm_SRgb:
                    return 8;

                default:
                    return 0;
            }
        }

        public static void SaveBitmap(DeviceManager dev, SharpDX.WIC.Bitmap bm, string filename)
        {
            System.Diagnostics.Debug.Assert(bm != null);
            Guid containerFormat = Guid.Empty;
            string lowerName = filename.ToLower();
            if (lowerName.Contains(".png"))
                containerFormat = SharpDX.WIC.ContainerFormatGuids.Png;
            else if (lowerName.Contains(".bmp"))
                containerFormat = SharpDX.WIC.ContainerFormatGuids.Bmp;
            else if (lowerName.Contains(".jpg"))
                containerFormat = SharpDX.WIC.ContainerFormatGuids.Jpeg;
            else if (lowerName.Contains(".jpeg"))
                containerFormat = SharpDX.WIC.ContainerFormatGuids.Jpeg;
            else if (lowerName.Contains(".tif"))
                containerFormat = SharpDX.WIC.ContainerFormatGuids.Tiff;
            else if (lowerName.Contains(".gif"))
                containerFormat = SharpDX.WIC.ContainerFormatGuids.Gif;

            Guid format = bm.PixelFormat;
            using (var stream = System.IO.File.OpenWrite(filename))
            {
                stream.Position = 0;
                using (SharpDX.WIC.BitmapEncoder enc = new SharpDX.WIC.BitmapEncoder(dev.WICFactory, containerFormat, stream))
                using (SharpDX.WIC.BitmapFrameEncode bfe = new SharpDX.WIC.BitmapFrameEncode(enc))
                {
                    bfe.Initialize();
                    bfe.SetPixelFormat(ref format);
                    bfe.SetSize(bm.Size.Width, bm.Size.Height);
                    bfe.WriteSource(bm);
                    bfe.Commit();
                    enc.Commit();
                }
            }
        }

        public static System.Drawing.Bitmap SaveToBitmap(DeviceManager deviceManager, Texture2D source)
        {
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;
            
            var txDesc = source.Description;
            txDesc.Usage = ResourceUsage.Staging;
            txDesc.CpuAccessFlags = CpuAccessFlags.Read;
            txDesc.BindFlags = BindFlags.None;
            txDesc.OptionFlags = ResourceOptionFlags.None;
            txDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

            Guid pixelFormat = PixelFormatFromFormat(txDesc.Format);
            if (pixelFormat == Guid.Empty)
            {
                return null;
            }

            System.Diagnostics.Debug.Assert(BitsPerPixel(txDesc.Format) == SharpDX.WIC.PixelFormat.GetBitsPerPixel(pixelFormat), "Error with DXGI.Format -> PixelFormat");
            
            using (var dest = new Texture2D(device, txDesc))
            {
                if (source.Description.SampleDescription.Count > 1 || source.Description.SampleDescription.Quality > 0)
                {
                    // In order to copy a multisampled texture to a CPU readable texture, it must first be resolved into a GPU only Texture
                    // Initialize a target to resolve multi-sampled render target
                    var resolvedDesc = source.Description;
                    resolvedDesc.BindFlags = BindFlags.ShaderResource;
                    resolvedDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

                    // if depth stencil needs to be typeless
                    if (resolvedDesc.Format == SharpDX.DXGI.Format.D24_UNorm_S8_UInt)
                        resolvedDesc.Format = SharpDX.DXGI.Format.R24G8_Typeless;

                    using (var resolvedTarget = new Texture2D(device, resolvedDesc))
                    {
                        CopyToTexture(context, source, resolvedTarget);
                        // Now we can copy to the destination
                        CopyToTexture(context, source, dest);
                    }
                }
                else 
                    CopyToTexture(context, source, dest);

                int width = txDesc.Width;
                int height = txDesc.Height;
                // Get the desktop capture texture
                var mapSource = device.ImmediateContext.MapSubresource(dest, 0, MapMode.Read, MapFlags.None);

                // Create Drawing.Bitmap
                var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var boundsRect = new System.Drawing.Rectangle(0, 0, width, height);

                // Copy pixels from screen capture Texture to GDI bitmap
                var mapDest = bitmap.LockBits(boundsRect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var sourcePtr = mapSource.DataPointer;
                var destPtr = mapDest.Scan0;
                for (int y = 0; y < height; y++)
                {
                    // Copy a single line 
                    SharpDX.Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                    // Advance pointers
                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                // Release source and dest locks
                bitmap.UnlockBits(mapDest);
                device.ImmediateContext.UnmapSubresource(dest, 0);

                return bitmap;
            }
        }

        public static void SaveToFile(DeviceManager deviceManager, Texture2D source, string filename, SharpDX.DXGI.Format? format = null, int subResource = 0)
        {
            
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;
            
            var txDesc = source.Description;
            txDesc.ArraySize = 1;
            txDesc.Usage = ResourceUsage.Staging;
            txDesc.CpuAccessFlags = CpuAccessFlags.Read;
            txDesc.BindFlags = BindFlags.None;
            txDesc.OptionFlags = ResourceOptionFlags.None;
            txDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

            //Guid pixelFormat = PixelFormatFromFormat(txDesc.Format);
            //if (pixelFormat == Guid.Empty)
            //{
            //    return;
            //}

            //System.Diagnostics.Debug.Assert(BitsPerPixel(txDesc.Format) == SharpDX.WIC.PixelFormat.GetBitsPerPixel(pixelFormat), "Error with DXGI.Format -> PixelFormat");

            using (var dest = new Texture2D(device, txDesc))
            {
                if (source.Description.SampleDescription.Count > 1 || source.Description.SampleDescription.Quality > 0)
                {
                    // In order to copy a multisampled texture to a CPU readable texture, it must first be resolved into a GPU only Texture
                    // Initialize a target to resolve multi-sampled render target
                    var resolvedDesc = source.Description;
                    resolvedDesc.BindFlags = BindFlags.ShaderResource;
                    resolvedDesc.OptionFlags = ResourceOptionFlags.None;
                    resolvedDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

                    // if depth stencil needs to be typeless
                    if (resolvedDesc.Format == SharpDX.DXGI.Format.D24_UNorm_S8_UInt)
                        resolvedDesc.Format = SharpDX.DXGI.Format.R24G8_Typeless;

                    using (var resolvedTarget = new Texture2D(device, resolvedDesc))
                    {
                        CopyToTexture(context, source, resolvedTarget, subResource);
                        // Now we can copy to the destination
                        CopyToTexture(context, source, dest, subResource);
                    }
                }
                else
                    CopyToTexture(context, source, dest, subResource);

                var sourceData = context.MapSubresource(dest, 0, MapMode.Read, MapFlags.None);

                using (SharpDX.Toolkit.Graphics.Image image = SharpDX.Toolkit.Graphics.Image.New(new SharpDX.Toolkit.Graphics.ImageDescription()
                {
                    ArraySize = 1,
                    Depth = 1,
                    Dimension = SharpDX.Toolkit.Graphics.TextureDimension.Texture2D,
                    Format = format ?? dest.Description.Format,
                    Width = source.Description.Width,
                    MipLevels = 1,
                    Height = source.Description.Height
                }, sourceData.DataPointer))
                {
                    image.Save(filename);
                }
            }
        }

        public static SharpDX.WIC.Bitmap SaveToWICBitmap(DeviceManager deviceManager, Texture2D source)
        {
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;
            
            var txDesc = source.Description;
            
            txDesc.Usage = ResourceUsage.Staging;
            txDesc.CpuAccessFlags = CpuAccessFlags.Read;
            txDesc.BindFlags = BindFlags.None;
            txDesc.OptionFlags = ResourceOptionFlags.None;
            txDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

            Guid pixelFormat = PixelFormatFromFormat(txDesc.Format);
            if (pixelFormat == Guid.Empty)
            {
                return null;
            }

            System.Diagnostics.Debug.Assert(BitsPerPixel(txDesc.Format) == SharpDX.WIC.PixelFormat.GetBitsPerPixel(pixelFormat), "Error with DXGI.Format -> PixelFormat");
            
            using (var dest = new Texture2D(device, txDesc))
            {
                if (source.Description.SampleDescription.Count > 1 || source.Description.SampleDescription.Quality > 0)
                {
                    // In order to copy a multisampled texture to a CPU readable texture, it must first be resolved into a GPU only Texture
                    // Initialize a target to resolve multi-sampled render target
                    var resolvedDesc = source.Description;
                    resolvedDesc.BindFlags = BindFlags.ShaderResource;
                    resolvedDesc.SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0);

                    // if depth stencil needs to be typeless
                    if (resolvedDesc.Format == SharpDX.DXGI.Format.D24_UNorm_S8_UInt)
                        resolvedDesc.Format = SharpDX.DXGI.Format.R24G8_Typeless;

                    using (var resolvedTarget = new Texture2D(device, resolvedDesc))
                    {
                        CopyToTexture(context, source, resolvedTarget);
                        // Now we can copy to the destination
                        CopyToTexture(context, source, dest);
                    }
                }
                else
                    CopyToTexture(context, source, dest);
                var sourceData = context.MapSubresource(dest, 0, MapMode.Read, MapFlags.None);
                
                var encoder = new SharpDX.WIC.PngBitmapEncoder(deviceManager.WICFactory);

                var formatConverter = new SharpDX.WIC.FormatConverter(deviceManager.WICFactory);


                SharpDX.WIC.Bitmap bm = new SharpDX.WIC.Bitmap(deviceManager.WICFactory, txDesc.Width, txDesc.Height, pixelFormat, SharpDX.WIC.BitmapCreateCacheOption.CacheOnLoad);

                var bytesPerPixel = BitsPerPixel(txDesc.Format) / 8;
                using (var l = bm.Lock(SharpDX.WIC.BitmapLockFlags.Write))
                {
                    var destPtr = l.Data.DataPointer;
                    var sourcePtr = sourceData.DataPointer;
                    for (int y = 0; y < bm.Size.Height; y++)
                    {
                        SharpDX.Utilities.CopyMemory(destPtr, sourcePtr, bm.Size.Width * bytesPerPixel);

                        sourcePtr = IntPtr.Add(sourcePtr, sourceData.RowPitch);
                        destPtr = IntPtr.Add(destPtr, l.Data.Pitch);
                    }
                }
                context.UnmapSubresource(dest, 0);

                return bm;
            }
        }
    }
}
