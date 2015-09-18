// Copyright (c) 2013 Justin Stenning
// Adapted from original code by Alexandre Mutel
// 
//----------------------------------------------------------------------------
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
using System.Diagnostics;

using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Matrix = SharpDX.Matrix;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace Common
{
    /// <summary>
    /// Renders text using Direct2D to the back buffer
    /// </summary>
    public class TextRenderer : RendererBase
    {
        TextFormat textFormat;
        Brush sceneColorBrush;
        protected string font;
        protected Color4 color;
        protected int lineLength;

        /// <summary>
        /// Initializes a new instance of <see cref="TextRenderer"/> class.
        /// </summary>
        public TextRenderer(string font, Color4 color, Point location, int size = 16, int lineLength = 500)
            : base()
        {
            if (!String.IsNullOrEmpty(font))
                this.font = font;
            else
                this.font = "Calibri";

            this.color = color;
            this.Location = location;
            this.Size = size;
            this.lineLength = lineLength;
        }

        public int Size { get; set; }
        public string Text { get; set; }
        public Point Location { get; set; }

        /// <summary>
        /// Create any device resources
        /// </summary>
        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            RemoveAndDispose(ref sceneColorBrush);
            RemoveAndDispose(ref textFormat);

            sceneColorBrush = ToDispose(new SolidColorBrush(this.DeviceManager.Direct2DContext, this.color));
            textFormat = ToDispose(new TextFormat(this.DeviceManager.DirectWriteFactory, font, Size) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Center });

            this.DeviceManager.Direct2DContext.TextAntialiasMode = TextAntialiasMode.Grayscale;
        }

        /// <summary>
        /// Render
        /// </summary>
        /// <param name="target">The target to render to (the same device manager must be used in both)</param>
        protected override void DoRender()
        {
            if (String.IsNullOrEmpty(Text))
                return;

            var context2D = DeviceManager.Direct2DContext;

            context2D.BeginDraw();
            context2D.Transform = Matrix.Identity;
            context2D.DrawText(Text, textFormat, new RectangleF(Location.X, Location.Y, Location.X + lineLength, Location.Y + 16), sceneColorBrush);
            context2D.EndDraw();
        }
    }
}
