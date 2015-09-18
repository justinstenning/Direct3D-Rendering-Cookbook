using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;

namespace Ch08_02Particles
{
    /// <summary>
    /// Toggles between two sets of resources for two textures.
    /// Useful when needing to chain together multiple input -> output passes that
    /// use the output from the previous stage.
    /// </summary>
    public class TexturePingPong
    {
        ShaderResourceView[] SRVs;
        UnorderedAccessView[] UAVs;
        Texture2D[] Textures;

        bool isFirst;

        public TexturePingPong()
        {
            SRVs = new ShaderResourceView[2];
            UAVs = new UnorderedAccessView[4];
            Textures = new Texture2D[2];
            isFirst = false;
        }

        private int GetCurrent()
        {
            return (isFirst ? 0 : 1);
        }

        private int GetNext()
        {
            isFirst = !isFirst;
            return GetCurrent();
        }

        public void SetSRVs(ShaderResourceView first, ShaderResourceView second)
        {
            SRVs[0] = first;
            SRVs[1] = second;
        }

        public void SetUAVs(UnorderedAccessView first, UnorderedAccessView second)
        {
            UAVs[0] = first;
            UAVs[1] = second;
        }

        public void SetUIntUAVs(UnorderedAccessView first, UnorderedAccessView second)
        {
            UAVs[2] = first;
            UAVs[3] = second;
        }

        public void SetTextures(Texture2D first, Texture2D second)
        {
            Textures[0] = first;
            Textures[1] = second;
        }

        public ShaderResourceView GetNextAsSRV()
        {
            return SRVs[GetNext()];
        }

        public UnorderedAccessView GetNextAsUAV()
        {
            return UAVs[GetNext()];
        }

        public UnorderedAccessView GetNextAsUIntUAV()
        {
            return UAVs[GetNext() + 2];
        }

        public Texture2D GetNextAsTexture()
        {
            return Textures[GetNext()];
        }

        public ShaderResourceView GetCurrentAsSRV()
        {
            return SRVs[GetCurrent()];
        }

        public UnorderedAccessView GetCurrentAsUAV()
        {
            return UAVs[GetCurrent()];
        }

        public UnorderedAccessView GetCurrentAsUIntUAV()
        {
            return UAVs[GetCurrent() + 2];
        }

        public Texture2D GetCurrentAsTexture()
        {
            return Textures[GetCurrent()];
        }

    }
}
