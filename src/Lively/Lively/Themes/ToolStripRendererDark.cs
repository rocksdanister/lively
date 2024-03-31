using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace Lively.Themes
{
    public class ToolStripRendererDark : ToolStripProfessionalRenderer
    {
        public ToolStripRendererDark()
              : base(new DarkColorTable())
        {
            this.RoundedEdges = true;
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(e.ArrowRectangle.Location, e.ArrowRectangle.Size);
            r.Inflate(-2, -6);
            e.Graphics.DrawLines(Pens.White, new Point[]{
                    new Point(r.Left, r.Top),
                    new Point(r.Right, r.Top + r.Height /2),
                    new Point(r.Left, r.Top+ r.Height)});
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
            r.Inflate(-4, -6);
            e.Graphics.DrawLines(Pens.White, new Point[]{
                    new Point(r.Left, r.Bottom - r.Height /2),
                    new Point(r.Left + r.Width /3,  r.Bottom),
                    new Point(r.Right, r.Top)});
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) base.OnRenderMenuItemBackground(e);
            else
            {
                var fillColor = new System.Drawing.SolidBrush(Color.FromArgb(75, 75, 75));
                var borderColor = new System.Drawing.Pen(Color.FromArgb(75, 75, 75));
                Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                e.Graphics.FillRectangle(fillColor, rc);
                e.Graphics.DrawRectangle(borderColor, 1, 0, rc.Width - 2, rc.Height - 1);
                fillColor.Dispose();
                borderColor.Dispose();
            }
        }

        //protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        //{
        //    base.OnRenderToolStripBorder(e);
        //    Rectangle borderRectangle = new Rectangle(Point.Empty, e.AffectedBounds.Size);
        //    using GraphicsPath borderPath = GetRoundedRectangle(borderRectangle, 5);
        //    using Pen borderPen = new Pen(Color.FromArgb(75, 75, 75), 1f);
        //    e.Graphics.DrawPath(borderPen, borderPath);
        //}

        //private static GraphicsPath GetRoundedRectangle(Rectangle rectangle, int cornerRadius)
        //{
        //    GraphicsPath path = new GraphicsPath();
        //    path.AddArc(rectangle.X, rectangle.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
        //    path.AddArc(rectangle.Right - cornerRadius * 2, rectangle.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
        //    path.AddArc(rectangle.Right - cornerRadius * 2, rectangle.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
        //    path.AddArc(rectangle.X, rectangle.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
        //    path.CloseFigure();
        //    return path;
        //}

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            readonly Color foregroundGray = Color.FromArgb(43, 43, 43);
            readonly Color backgroundGray = Color.FromArgb(50, 50, 50);

            public override Color ToolStripBorder
            {
                get { return foregroundGray; }
            }
            public override Color ToolStripDropDownBackground
            {
                get { return foregroundGray; }
            }
            public override Color ToolStripGradientBegin
            {
                get { return foregroundGray; }
            }
            public override Color ToolStripGradientEnd
            {
                get { return foregroundGray; }
            }
            public override Color ToolStripGradientMiddle
            {
                get { return foregroundGray; }
            }
            public override Color ImageMarginGradientBegin
            {
                get { return backgroundGray; }
            }
            public override Color ImageMarginGradientEnd
            {
                get { return backgroundGray; }
            }
            public override Color ImageMarginGradientMiddle
            {
                get { return backgroundGray; }
            }
            public override Color ImageMarginRevealedGradientBegin
            {
                get { return foregroundGray; }
            }
            public override Color ImageMarginRevealedGradientEnd
            {
                get { return foregroundGray; }
            }
            public override Color ImageMarginRevealedGradientMiddle
            {
                get { return foregroundGray; }
            }
            public override Color MenuItemSelected
            {
                get { return foregroundGray; }
            }
            public override Color MenuItemSelectedGradientBegin
            {
                get { return foregroundGray; }
            }
            public override Color MenuItemSelectedGradientEnd
            {
                get { return foregroundGray; }
            }
            public override Color MenuItemBorder
            {
                get { return foregroundGray; }
            }
            public override Color MenuBorder
            {
                get { return backgroundGray; }
            }
            public override Color ButtonCheckedGradientBegin
            {
                get { return foregroundGray; }
            }
        }
    }
}
