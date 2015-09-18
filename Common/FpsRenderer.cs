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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Matrix = SharpDX.Matrix;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace Common
{
    /// <summary>
    /// Display an overlay text with FPS and ms/frame counters.
    /// </summary>
    public class FpsRenderer: TextRenderer
    {
        Stopwatch clock;
        //double totalTime;
        long frameCount;
        //double measuredFPS;
        //double measuredFrameTime;

        /// <summary>
        /// Initializes a new instance of <see cref="FpsRenderer"/> class.
        /// </summary>
        public FpsRenderer(): base("Calibri", Color.White, new Point(8, 8), 16)
        {
        }

        public FpsRenderer(string font, Color4 color, Point location, int size)
            : base(font, color, location, size)
        {
        }

        public override void Initialize(D3DApplicationBase app)
        {
            base.Initialize(app);

            clock = Stopwatch.StartNew();
        }


        const int MAXSAMPLES = 100;
        int tickindex=0;
        long ticksum=0;
        long[] ticklist = new long[MAXSAMPLES];

        /* need to zero out the ticklist array before starting */
        /* average will ramp up until the buffer is full */
        /* returns average ticks per frame over the MAXSAMPPLES last frames */
        //http://stackoverflow.com/questions/87304/calculating-frames-per-second-in-a-game/87732#87732
        double CalcAverageTick(long newtick)
        {
            ticksum-=ticklist[tickindex];  /* subtract value falling off */
            ticksum+=newtick;              /* add new value */
            ticklist[tickindex]=newtick;   /* save new value so it can be subtracted later */
            if(++tickindex==MAXSAMPLES)    /* inc buffer index */
                tickindex=0;

            /* return average */
            if (frameCount < MAXSAMPLES)
            {
                return (double)ticksum / frameCount;
            }
            else
            {
                return (double)ticksum / MAXSAMPLES;
            }
        }

        protected override void DoRender()
        {
            frameCount++;
            var averageTick = CalcAverageTick(clock.ElapsedTicks) / Stopwatch.Frequency;
            this.Text = string.Format("{0:F2} FPS ({1:F1} ms)", 1.0 / averageTick, averageTick * 1000.0);

            base.DoRender();

            clock.Restart();
        }
    }
}
