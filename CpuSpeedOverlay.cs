using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PowerModes
{
    public class CpuSpeedOverlay : Form
    {
        private Label lblCpuSpeed;
        private Timer visibilityTimer;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        [DllImport("shell32.dll")]
        static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_SHOWWINDOW = 0x0040;

        const int ABM_GETTASKBARPOS = 5;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;

        public CpuSpeedOverlay()
        {
            InitializeComponent();
            SetupWindowStyles();
            PositionNearTaskbar();
            InitializeVisibilityTimer();
        }

        private void InitializeVisibilityTimer()
        {
            // Timer to periodically ensure the window stays visible and topmost
            visibilityTimer = new Timer();
            visibilityTimer.Interval = 5000; // Check every 5 seconds
            visibilityTimer.Tick += VisibilityTimer_Tick;
            visibilityTimer.Start();
        }

        private void VisibilityTimer_Tick(object sender, EventArgs e)
        {
            if (!this.IsDisposed && this.IsHandleCreated)
            {
                // Ensure window is visible
                if (!this.Visible)
                {
                    this.Show();
                }

                // Reapply topmost using SetWindowPos
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);

                // Check if layered style was lost and reapply if needed
                int currentStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                if ((currentStyle & WS_EX_LAYERED) == 0)
                {
                    SetupWindowStyles();
                }
            }
        }

        private void InitializeComponent()
        {
            this.lblCpuSpeed = new Label();
            this.SuspendLayout();

            // 
            // lblCpuSpeed
            // 
            this.lblCpuSpeed.AutoSize = false;
            this.lblCpuSpeed.Dock = DockStyle.Fill;
            this.lblCpuSpeed.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblCpuSpeed.ForeColor = Color.LightYellow;
            this.lblCpuSpeed.TextAlign = ContentAlignment.MiddleCenter;
            this.lblCpuSpeed.Text = "... (...)";

            // 
            // CpuSpeedOverlay
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ClientSize = new Size(80, 25);
            this.Controls.Add(this.lblCpuSpeed);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "CpuSpeedOverlay";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.Text = "CPU Speed";
            this.ResumeLayout(false);
        }

        private void SetupWindowStyles()
        {
            // Make the window layered and semi-transparent
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TOOLWINDOW);

            // Set opacity (50% opaque)
            this.Opacity = 0.5;
        }

        private void PositionNearTaskbar()
        {
            APPBARDATA data = new APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(APPBARDATA))
            };

            IntPtr res = SHAppBarMessage(ABM_GETTASKBARPOS, ref data);
            if (res != IntPtr.Zero)
            {
                var rc = data.rc;
                int taskbarWidth = rc.right - rc.left;
                int taskbarHeight = rc.bottom - rc.top;

                // Determine taskbar position
                if (taskbarHeight < taskbarWidth)
                {
                    // Horizontal taskbar (bottom or top)
                    if (rc.top > 0)
                    {
                        // Taskbar at bottom
                        this.Location = new Point(rc.right - this.Width - 60, rc.top - this.Height - 6);
                    }
                    else
                    {
                        // Taskbar at top
                        this.Location = new Point(rc.right - this.Width - 60, rc.bottom + 6);
                    }
                }
                else
                {
                    // Vertical taskbar (left or right)
                    if (rc.left > 0)
                    {
                        // Taskbar at right
                        this.Location = new Point(rc.left - this.Width - 6, rc.bottom - this.Height - 50);
                    }
                    else
                    {
                        // Taskbar at left
                        this.Location = new Point(rc.right + 6, rc.bottom - this.Height - 50);
                    }
                }
            }
            else
            {
                // Fallback: position at bottom right
                Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                this.Location = new Point(workingArea.Right - this.Width - 180, workingArea.Bottom - this.Height - 6);
            }
        }

        public void UpdateCpuSpeed(float currentSpeed, float averageSpeed)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<float, float>(UpdateCpuSpeed), currentSpeed, averageSpeed);
                return;
            }

            lblCpuSpeed.Text = $"{currentSpeed:F2} ({averageSpeed:F2})";
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Reapply window styles after handle is created
            SetupWindowStyles();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (visibilityTimer != null)
                {
                    visibilityTimer.Stop();
                    visibilityTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
