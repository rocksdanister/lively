using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace Lively.Helpers.Theme
{
    //System-tray menu custom style, ref:
    //https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.toolstripprofessionalrenderer?view=netcore-3.1
    //https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.professionalcolortable?view=netcore-3.1
    class ContextMenuTheme
    {
        public class StripSeparatorCustom
        {
            public ToolStripSeparator stripSeparator;
            public StripSeparatorCustom()
            {
                stripSeparator = new ToolStripSeparator();
                stripSeparator.Paint += StripSeparator_Paint;
            }

            private void StripSeparator_Paint(object sender, PaintEventArgs e)
            {
                ToolStripSeparator stripSeparator = sender as ToolStripSeparator;
                ContextMenuStrip menuStrip = stripSeparator.Owner as ContextMenuStrip;
                e.Graphics.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, stripSeparator.Width, stripSeparator.Height));
                using (Pen pen = new Pen(Color.FromArgb(55, 55, 55), 1))
                {
                    e.Graphics.DrawLine(pen, new Point(23, stripSeparator.Height / 2), new Point(menuStrip.Width, stripSeparator.Height / 2));
                }
            }
        }

        public class RendererDark : ToolStripProfessionalRenderer
        {
            public RendererDark()
                : base(new DarkColorTable())
            {
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
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            Color foregroundGray = Color.FromArgb(43, 43, 43);
            Color backgroundGray = Color.FromArgb(50, 50, 50);
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
