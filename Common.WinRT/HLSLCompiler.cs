// Modified on 20-Jun-2013 by Justin Stenning
// From original code by Alexandre Mutel.
// -------------------------------------------------------------------
// Original source in SharpDX.Toolkit.Graphics.FileIncludeHandler
// -------------------------------------------------------------------
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;

namespace Common
{
    public static class HLSLCompiler
    {
        /// <summary>
        /// Compile the HLSL file using the provided <paramref name="entryPoint"/>, shader <paramref name="profile"/> and optionally conditional <paramref name="defines"/>
        /// </summary>
        /// <param name="hlslFile">Absolute path to HLSL file, or path relative to application installation location</param>
        /// <param name="entryPoint">e.g. shader function name e.g. VSMain</param>
        /// <param name="profile">Shader profile, e.g. vs_5_0</param>
        /// <param name="defines">An optional list of conditional defines.</param>
        /// <returns>The compiled ShaderBytecode</returns>
        /// <exception cref="CompilationException">Thrown if the compilation failed</exception>
        public static ShaderBytecode CompileFromFile(string hlslFile, string entryPoint, string profile, ShaderMacro[] defines = null)
        {
            if (!Path.IsPathRooted(hlslFile))
                hlslFile = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, hlslFile);
            var shaderSource = SharpDX.IO.NativeFile.ReadAllText(hlslFile);
            CompilationResult result = null;

            // Compile the shader file
            ShaderFlags flags = ShaderFlags.None;
#if DEBUG
            flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            var includeHandler = new HLSLFileIncludeHandler(Path.GetDirectoryName(hlslFile));
            result = ShaderBytecode.Compile(shaderSource, entryPoint, profile, flags, EffectFlags.None, defines, includeHandler, Path.GetFileName(hlslFile));

            if (!String.IsNullOrEmpty(result.Message))
                throw new CompilationException(result.ResultCode, result.Message);

            return result;
        }

        public static async Task<ShaderBytecode> CompileFromFileAsync(string hlslFile, string entryPoint, string profile, ShaderMacro[] defines = null)
        {
            if (!Path.IsPathRooted(hlslFile))
                hlslFile = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, hlslFile);

            CompilationResult result = null;
            
            await Task.Run(() =>
            {
                var shaderSource = SharpDX.IO.NativeFile.ReadAllText(hlslFile);

                // Compile the shader file
                ShaderFlags flags = ShaderFlags.None;
#if DEBUG
                flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
                var includeHandler = new HLSLFileIncludeHandler(Path.GetDirectoryName(hlslFile));
                result = ShaderBytecode.Compile(shaderSource, entryPoint, profile, flags, EffectFlags.None, defines, includeHandler, Path.GetFileName(hlslFile));

                if (!String.IsNullOrEmpty(result.Message))
                    throw new CompilationException(result.ResultCode, result.Message);
            });

            return result;
        }
    }
}
