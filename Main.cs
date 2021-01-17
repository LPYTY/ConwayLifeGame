﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;


namespace ConwayLifeGame
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Map.Initialize();
            Program.control = new Control();
        }

        private void HelpAbout_Click(object sender, EventArgs e)
        {
            About aboutDlg = new About();
            aboutDlg.ShowDialog();
        }

        private void HelpHelp_Click(object sender, EventArgs e)
        {
            Help helpDlg = new Help();
            helpDlg.ShowDialog();
        }

        private void FileNewWindow_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        private void EditShowWindow_Click(object sender, EventArgs e)
        {
            Program.control.Show();
        }

        private void FileExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private static class PaintTools
        {
            public static Factory factory;
            public static RenderTargetProperties renderProps;
            public static HwndRenderTargetProperties hwndProps;
            public static WindowRenderTarget renderTarget;
            public static Brush bkgndPen;
            public static Brush selectRectBrush;
            public static Brush selectRectPen;
            public static Brush selectCellPen;
            public static Brush copyBrush;
            public static Brush copyPen;

            public static void Init()
            {
                factory = new Factory(FactoryType.SingleThreaded);
                renderProps = new RenderTargetProperties()
                {
                    PixelFormat = new PixelFormat { AlphaMode = AlphaMode.Ignore, Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm },
                    Usage = RenderTargetUsage.None,
                    Type = RenderTargetType.Default
                };
                hwndProps = new HwndRenderTargetProperties()
                {
                    Hwnd = Program.main.MainPanel.Handle,
                    PixelSize = new Size2(Program.main.MainPanel.Width, Program.main.MainPanel.Height),
                    PresentOptions = PresentOptions.None
                };
                renderTarget = new WindowRenderTarget(factory, renderProps, hwndProps)
                {
                    AntialiasMode = AntialiasMode.PerPrimitive
                };
                bkgndPen = new SolidColorBrush(renderTarget, new RawColor4(0.3f, 0.3f, 0.3f, 1.0f));
                selectRectBrush = new SolidColorBrush(renderTarget, new RawColor4(0x00 / 256.0f, 0x97 / 256.0f, 0xA7 / 256.0f, 0.5f));
                selectRectPen = new SolidColorBrush(renderTarget, new RawColor4(0x1A / 256.0f, 0x23 / 256.0f, 0x7E / 256.0f, 1));
                selectCellPen = new SolidColorBrush(renderTarget, new RawColor4(0x1B / 256.0f, 0x5E / 256.0f, 0x20 / 256.0f, 1));
                copyBrush = new SolidColorBrush(renderTarget, new RawColor4(0x66 / 256.0f, 0xBB / 256.0f, 0x6A / 256.0f, 0.5f));
                copyPen = selectRectPen;
            }
        }

        private void MainPanel_Paint()
        {
            //  Init D2D
            if (PaintTools.renderTarget == null)
            { PaintTools.Init(); }

            //  Begin draw
            RenderTarget target = PaintTools.renderTarget;
            target.BeginDraw();
            target.Clear(new RawColor4(1, 1, 1, 1));

            System.Drawing.Size size = MainPanel.Size;
            int mid_x = size.Width / 2, mid_y = size.Height / 2, Scale = Map.Scale;

            //  Lines
            for (int i = mid_x % Scale; i <= size.Width; i += Scale)
                target.DrawLine(new RawVector2(i, 0), new RawVector2(i, size.Height), PaintTools.bkgndPen, 1f);
            for (int i = mid_y % Scale; i <= size.Height; i += Scale)
                target.DrawLine(new RawVector2(0, i), new RawVector2(size.Width, i), PaintTools.bkgndPen, 1f);

            //  Blocks
            Map.Draw(target);

            {
                int l = Math.Min(Map.MouseInfo.select_first.X, Map.MouseInfo.select_second.X);
                int r = Math.Max(Map.MouseInfo.select_first.X, Map.MouseInfo.select_second.X);
                int t = Math.Min(Map.MouseInfo.select_first.Y, Map.MouseInfo.select_second.Y);
                int b = Math.Max(Map.MouseInfo.select_first.Y, Map.MouseInfo.select_second.Y);
                //  Select (rect)  
                if (r - l != 0 || b - t != 0)
                {
                    RawRectangleF rect = new RawRectangleF(l, t, r, b);
                    target.FillRectangle(rect, PaintTools.selectRectBrush);
                    target.DrawRectangle(rect, PaintTools.selectRectPen);
                }
                //  Select (cell)  
                else if (!Map.MouseInfo.select_first.IsEmpty)
                {
                    int rl = (l - mid_x % Map.Scale) / Map.Scale * Map.Scale + mid_x % Map.Scale;
                    int rt = (t - mid_y % Map.Scale) / Map.Scale * Map.Scale + mid_y % Map.Scale;
                    RawRectangleF rect = new RawRectangleF(rl, rt, rl + Map.Scale, rt + Map.Scale);
                    target.DrawRectangle(rect, PaintTools.selectCellPen);
                }
            }

            //  Copy  
            if (Map.CopyInfo.state)
            {
                int l = Math.Min(Map.CopyInfo.first.X, Map.CopyInfo.second.X);
                int r = Math.Max(Map.CopyInfo.first.X, Map.CopyInfo.second.X);
                int t = Math.Min(Map.CopyInfo.first.Y, Map.CopyInfo.second.Y);
                int b = Math.Max(Map.CopyInfo.first.Y, Map.CopyInfo.second.Y);
                RawRectangleF rect = new RawRectangleF((l - Map.XPivot) * Map.Scale + mid_x, (t - Map.YPivot) * Map.Scale + mid_y, (r - Map.XPivot + 1) * Map.Scale + mid_x, (b - Map.YPivot + 1) * Map.Scale + mid_y);
                target.DrawRectangle(rect, PaintTools.copyPen);
                target.FillRectangle(rect, PaintTools.copyBrush);
            }

            target.EndDraw();
        }

        private void MainPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) MainPanel_LButtonDown(e);
            else if (e.Button == MouseButtons.Right) MainPanel_RButtonDown(e);
        }

        private void MainPanel_LButtonDown(MouseEventArgs e)
        {
            int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
            int xc = (e.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot;
            int yc = (e.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot;
            switch (Map.MouseInfo.state)
            {
                case Map.MouseState.click:
                    {
                        Map.Change(xc, yc);
                        break;
                    }
                case Map.MouseState.pen:
                    {
                        Map.Change(xc, yc);
                        Map.MouseInfo.previous = new System.Drawing.Point(xc, yc);
                        break;
                    }
                case Map.MouseState.eraser:
                    {
                        Map.MouseInfo.previous = new System.Drawing.Point(xc, yc);
                        Map.Change(xc, yc, 2);
                        break;
                    }
                case Map.MouseState.drag:
                    {
                        Map.MouseInfo.previous = new System.Drawing.Point(xc, yc);
                        break;
                    }
                case Map.MouseState.select:
                    {
                        if (Map.Started) Program.control.StartStop_Click(null, null);
                        Map.MouseInfo.select_first = new System.Drawing.Point(e.X, e.Y);
                        Map.MouseInfo.select_second = new System.Drawing.Point(e.X, e.Y);
                        break;
                    }
            }
        }

        private void MainPanel_RButtonDown(MouseEventArgs e)
        {
            int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
            int xc = (e.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot;
            int yc = (e.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot;
            Map.AddPreset(xc, yc);
        }

        private void MainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Map.MouseInfo.state != Map.MouseState.click)
            {
                int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
                int xc = (int)((e.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                int yc = (int)((e.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point pcur = new System.Drawing.Point(xc, yc);
                switch (Map.MouseInfo.state)
                {
                    case Map.MouseState.drag:
                        {
                            Program.control.XPivot.Value = Map.MouseInfo.previous.X - xc + Map.XPivot;
                            Program.control.YPivot.Value = Map.MouseInfo.previous.Y - yc + Map.YPivot;
                            break;
                        }
                    case Map.MouseState.pen:
                        {
                            {
                                System.Drawing.Point s = (pcur.X <= Map.MouseInfo.previous.X) ? pcur : Map.MouseInfo.previous, t = (s == pcur) ? Map.MouseInfo.previous : pcur;
                                double k = ((double)t.Y - s.Y) / ((double)t.X - s.X);
                                for (int i = s.X; i <= t.X; i++)
                                    Map.Change(i, (int)(s.Y + ((double)i - s.X) * k), 1);
                            }
                            {
                                System.Drawing.Point s = (pcur.Y <= Map.MouseInfo.previous.Y) ? pcur : Map.MouseInfo.previous, t = (s == pcur) ? Map.MouseInfo.previous : pcur;
                                double k = ((double)t.X - s.X) / ((double)t.Y - s.Y);
                                for (int i = s.Y; i <= t.Y; i++)
                                    Map.Change((int)(s.X + ((double)i - s.Y) * k), i, 1);
                            }
                            Map.MouseInfo.previous = pcur;
                            break;
                        }
                    case Map.MouseState.eraser:
                        {
                            {
                                System.Drawing.Point s = (pcur.X <= Map.MouseInfo.previous.X) ? pcur : Map.MouseInfo.previous, t = (s == pcur) ? Map.MouseInfo.previous : pcur;
                                double k = ((double)t.Y - s.Y) / ((double)t.X - s.X);
                                for (int i = s.X; i <= t.X; i++)
                                    Map.Change(i, (int)(s.Y + ((double)i - s.X) * k), 2);
                            }
                            {
                                System.Drawing.Point s = (pcur.Y <= Map.MouseInfo.previous.Y) ? pcur : Map.MouseInfo.previous, t = (s == pcur) ? Map.MouseInfo.previous : pcur;
                                double k = ((double)t.X - s.X) / ((double)t.Y - s.Y);
                                for (int i = s.Y; i <= t.Y; i++)
                                    Map.Change((int)(s.X + ((double)i - s.Y) * k), i, 2);
                            }
                            Map.MouseInfo.previous = pcur;
                            break;
                        }
                    case Map.MouseState.select:
                        {
                            Map.MouseInfo.select_second = new System.Drawing.Point(e.X, e.Y);
                            break;
                        }
                }
            }
        }

        private void MainPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (Map.AddRgnInfo.state != Map.AddRegionState.normal)
            {
                int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
                int xc = (int)((Map.MouseInfo.select_first.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                int yc = (int)((Map.MouseInfo.select_first.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p1 = new System.Drawing.Point(xc, yc);
                xc = (int)((Map.MouseInfo.select_second.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                yc = (int)((Map.MouseInfo.select_second.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p2 = new System.Drawing.Point(xc, yc);
                Map.AddDeleteRegion(p1, p2);
                Map.MouseInfo.select_first = Map.MouseInfo.select_second = new System.Drawing.Point();
                Map.AddRgnInfo.state = Map.AddRegionState.normal;
                
                switch (Map.AddRgnInfo.lastMouseState)
                {
                    case Map.MouseState.click:
                        Program.control.MouseStateClick.Checked = true;
                        break;
                    case Map.MouseState.pen:
                        Program.control.MouseStatePen.Checked = true;
                        break;
                    case Map.MouseState.eraser:
                        Program.control.MouseStateEraser.Checked = true;
                        break;
                    case Map.MouseState.drag:
                        Program.control.MouseStateDrag.Checked = true;
                        break;
                    default: break;
                }
            }
        }

        private void MainPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            int i = (int)Program.control.MapScale.Value;
            if (!( i <= 2) || !(e.Delta < 0))
            {
                i += e.Delta / 40;
                if (i < 2) i = 3;
                if (i > 999) i = 999;
            }
            Program.control.MapScale.Value = i;
        }

        private void ClacTimer_Tick(object sender, EventArgs e)
        {
            Map.Calc();
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            int move_length = 40;
            if (e.Control || e.Alt || e.Shift) { e.Handled = false; return; }
            switch (e.KeyCode)
            {
                case Keys.B:
                    {
                        Map.KeybdInputState = Map.KeyboardInputState.bulitin;
                        break;
                    }
                case Keys.D:
                    {
                        Map.KeybdInputState = Map.KeyboardInputState.direction;
                        break;
                    }
                case Keys.C:
                    {
                        EditShowWindow_Click(null, null);
                        break;
                    }
                case Keys.Space:
                    {
                        Program.control.StartStop_Click(null, null);
                        break;
                    }
                case Keys.Delete:
                    {
                        Program.control.Reset_Click(null, null);
                        break;
                    }
                case Keys.Left:
                    {
                        Program.control.XPivot.Value -= move_length / Map.Scale;
                        break;
                    }
                case Keys.Right:
                    {
                        Program.control.XPivot.Value += move_length / Map.Scale;
                        break;
                    }
                case Keys.Up:
                    {
                        Program.control.YPivot.Value -= move_length / Map.Scale;
                        break;
                    }
                case Keys.Down:
                    {
                        Program.control.YPivot.Value += move_length / Map.Scale;
                        break;
                    }
                case Keys.Oemplus:
                    {
                        try { Program.control.Timer.Value -= 5; }
                        catch (ArgumentOutOfRangeException)
                        { Program.control.Timer.Value = Program.control.Timer.Minimum; }
                        break;
                    }
                case Keys.OemMinus:
                    {
                        try { Program.control.Timer.Value += 5; }
                        catch (ArgumentOutOfRangeException)
                        { Program.control.Timer.Value = Program.control.Timer.Maximum; }
                        break;
                    }
                default:
                    {
                        if (!(e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)) { e.Handled = false; return; }

                        switch (Map.KeybdInputState)
                        {
                            case Map.KeyboardInputState.normal: { e.Handled = false; return; }
                            case Map.KeyboardInputState.bulitin:
                                {
                                    try { Program.control.PresetSelect.Value = e.KeyCode - Keys.D0; }
                                    catch (ArgumentOutOfRangeException) { }
                                    break;
                                }
                            case Map.KeyboardInputState.direction:
                                {
                                    Program.control.DirectionSelect.Value = e.KeyCode - Keys.D0;
                                    break;
                                }
                        }
                        Map.KeybdInputState = Map.KeyboardInputState.normal;
                        break;
                    }
            }
            e.Handled = true;
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e) { }
        /*{
            PaintTools.graphics.Dispose();
            PaintTools.bkgndPen.Dispose();
            PaintTools.bkgndBitmap.Dispose();
            PaintTools.mainPicBitmap.Dispose();
            PaintTools.copyPen.Dispose();
            PaintTools.copyBrush.Dispose();
            PaintTools.mapPicBitmap.Dispose();
            PaintTools.selectCellPen.Dispose();
            PaintTools.selectRectPen.Dispose();
            PaintTools.selectRectBrush.Dispose();
        }*/

        private void EditCreateRandom_Click(object sender, EventArgs e)
        {
            Map.AddRgnInfo.state = Map.AddRegionState.random;
            if (Map.MouseInfo.select_first.IsEmpty)
            {
                Map.AddRgnInfo.lastMouseState = Map.MouseInfo.state;
                Program.control.MouseStateSelect.Checked = true;
            }
            else
            {
                int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
                int xc = (int)((Map.MouseInfo.select_first.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                int yc = (int)((Map.MouseInfo.select_first.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p1 = new System.Drawing.Point(xc, yc);
                xc = (int)((Map.MouseInfo.select_second.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                yc = (int)((Map.MouseInfo.select_second.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p2 = new System.Drawing.Point(xc, yc);
                Map.AddDeleteRegion(p1, p2);
                Map.MouseInfo.select_first = Map.MouseInfo.select_second = new System.Drawing.Point();
                Map.AddRgnInfo.state = Map.AddRegionState.normal;
            }
        }

        private void EditCreateSolid_Click(object sender, EventArgs e)
        {
            Map.AddRgnInfo.state = Map.AddRegionState.insert;
            if (Map.MouseInfo.select_first.IsEmpty)
            {
                Map.AddRgnInfo.lastMouseState = Map.MouseInfo.state;
                Program.control.MouseStateSelect.Checked = true;
            }
            else
            {
                int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
                int xc = (int)((Map.MouseInfo.select_first.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                int yc = (int)((Map.MouseInfo.select_first.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p1 = new System.Drawing.Point(xc, yc);
                xc = (int)((Map.MouseInfo.select_second.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                yc = (int)((Map.MouseInfo.select_second.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p2 = new System.Drawing.Point(xc, yc);
                Map.AddDeleteRegion(p1, p2);
                Map.MouseInfo.select_first = Map.MouseInfo.select_second = new System.Drawing.Point();
                Map.AddRgnInfo.state = Map.AddRegionState.normal;
            }
        }

        private void EditDeleteRegion_Click(object sender, EventArgs e)
        {
            Map.AddRgnInfo.state = Map.AddRegionState.delete;
            if (Map.MouseInfo.select_first.IsEmpty)
            {
                Map.AddRgnInfo.lastMouseState = Map.MouseInfo.state;
                Program.control.MouseStateSelect.Checked = true;
            }
            else
            {
                int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
                int xc = (int)((Map.MouseInfo.select_first.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                int yc = (int)((Map.MouseInfo.select_first.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p1 = new System.Drawing.Point(xc, yc);
                xc = (int)((Map.MouseInfo.select_second.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
                yc = (int)((Map.MouseInfo.select_second.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
                System.Drawing.Point p2 = new System.Drawing.Point(xc, yc);
                Map.AddDeleteRegion(p1, p2);
                Map.MouseInfo.select_first = Map.MouseInfo.select_second = new System.Drawing.Point();
                Map.AddRgnInfo.state = Map.AddRegionState.normal;
            }
        }

        private void FileOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "JSON Life File|*.lfs|Life File|*.lf||",
                DefaultExt = ".lfs"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fname = openFileDialog.FileName;
                if (fname.EndsWith(".lfs")) Map.LoadLFS(fname);
                if (fname.EndsWith(".lf")) Map.LoadLF(fname);
            }
        }

        private void FileSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Life File|*.lfs||",
                AddExtension = true
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                Map.DumpLFS(saveFileDialog.FileName);
        }

        private void EditCopy_Click(object sender, EventArgs e)
        {
            if (Map.MouseInfo.select_first.IsEmpty && Map.MouseInfo.select_second.IsEmpty) return;
            int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
            int xc = (int)((Map.MouseInfo.select_first.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
            int yc = (int)((Map.MouseInfo.select_first.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
            Map.CopyInfo.first = new System.Drawing.Point(xc, yc);
            xc = (int)((Map.MouseInfo.select_second.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
            yc = (int)((Map.MouseInfo.select_second.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
            Map.CopyInfo.second = new System.Drawing.Point(xc, yc);
            Map.CopyInfo.state = true;
            Map.MouseInfo.select_first = Map.MouseInfo.select_second = new System.Drawing.Point();
        }

        private void EditPaste_Click(object sender, EventArgs e)
        {
            if (!Map.CopyInfo.state) return;
            if (Map.MouseInfo.select_first != Map.MouseInfo.select_second) return; 
            int mid_x = MainPanel.Width / 2, mid_y = MainPanel.Height / 2;
            int xc = (int)((Map.MouseInfo.select_first.X - mid_x + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.XPivot);
            int yc = (int)((Map.MouseInfo.select_first.Y - mid_y + 0x1000 * Map.Scale) / Map.Scale - 0x1000 + Map.YPivot);
            Map.Paste(xc, yc);
            Map.CopyInfo.state = false;
        }

        private void MainPanel_SizeChanged(object sender, EventArgs e)
        {
            PaintTools.renderTarget.Resize(new Size2(MainPanel.Width, MainPanel.Height));
        }

        private void PaintTimer_Tick(object sender, EventArgs e)
        {
            MainPanel_Paint();
        }
    }
}
