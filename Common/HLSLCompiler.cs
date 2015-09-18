using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX;

namespace Common
{
    public static class HLSLCompiler
    {
        /// <summary>
        /// Compile the HLSL file using the provided <paramref name="entryPoint"/>, shader <paramref name="profile"/> and optionally conditional <paramref name="defines"/>
        /// </summary>
        /// <param name="hlslFile">Absolute path to HLSL file, or path relative to application installation location</param>
        /// <param name="entryPoint">Shader function name e.g. VSMain</param>
        /// <param name="profile">Shader profile, e.g. vs_5_0</param>
        /// <param name="defines">An optional list of conditional defines.</param>
        /// <returns>The compiled ShaderBytecode</returns>
        /// <exception cref="CompilationException">Thrown if the compilation failed</exception>
        public static ShaderBytecode CompileFromFile(string hlslFile, string entryPoint, string profile, ShaderMacro[] defines = null)
        {
            if (!Path.IsPathRooted(hlslFile))
                hlslFile = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), hlslFile);
            var shaderSource = SharpDX.IO.NativeFile.ReadAllText(hlslFile);
            CompilationResult result = null;

            // Compile the shader file
            ShaderFlags flags = ShaderFlags.None;
#if DEBUG
            flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            var includeHandler = new HLSLFileIncludeHandler(Path.GetDirectoryName(hlslFile));
            result = ShaderBytecode.Compile(shaderSource, entryPoint, profile, flags, EffectFlags.None, defines, includeHandler, Path.GetFileName(hlslFile));

            if (result.ResultCode.Failure)
                throw new CompilationException(result.ResultCode, result.Message);

            return result;
        }

        public static SharpDX.Direct3D11.PixelShader PixelShader(SharpDX.Direct3D11.Device device, string hlslFile, string entryPoint, ShaderMacro[] defines = null, string profile = "ps_5_0")
        {
            using (var bytecode = CompileFromFile(hlslFile, entryPoint, profile, defines))
            {
                return new SharpDX.Direct3D11.PixelShader(device, bytecode);
            }
        }

        public static SharpDX.Direct3D11.VertexShader VertexShader(SharpDX.Direct3D11.Device device, string hlslFile, string entryPoint, ShaderMacro[] defines = null, string profile = "vs_5_0")
        {
            using (var bytecode = CompileFromFile(hlslFile, entryPoint, profile, defines))
            {
                return new SharpDX.Direct3D11.VertexShader(device, bytecode);
            }
        }

        public static SharpDX.Direct3D11.VertexShader VertexShader(SharpDX.Direct3D11.Device device, string hlslFile, string entryPoint, out ShaderBytecode bytecode, ShaderMacro[] defines = null, string profile = "vs_5_0")
        {
            bytecode = CompileFromFile(hlslFile, entryPoint, profile, defines);
            return new SharpDX.Direct3D11.VertexShader(device, bytecode);
        }

        public static SharpDX.Direct3D11.HullShader HullShader(SharpDX.Direct3D11.Device device, string hlslFile, string entryPoint, ShaderMacro[] defines = null, string profile = "hs_5_0")
        {
            using (var bytecode = CompileFromFile(hlslFile, entryPoint, profile, defines))
            {
                return new SharpDX.Direct3D11.HullShader(device, bytecode);
            }
        }

        public static SharpDX.Direct3D11.DomainShader DomainShader(SharpDX.Direct3D11.Device device, string hlslFile, string entryPoint, ShaderMacro[] defines = null, string profile = "ds_5_0")
        {
            using (var bytecode = CompileFromFile(hlslFile, entryPoint, profile, defines))
            {
                return new SharpDX.Direct3D11.DomainShader(device, bytecode);
            }
        }

        public static SharpDX.Direct3D11.GeometryShader GeometryShader(SharpDX.Direct3D11.Device device, string hlslFile, string entryPoint, ShaderMacro[] defines = null, string profile = "gs_5_0")
        {
            using (var bytecode = CompileFromFile(hlslFile, entryPoint, profile, defines))
            {
                return new SharpDX.Direct3D11.GeometryShader(device, bytecode);
            }
        }
    }
}
