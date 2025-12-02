namespace PowerModes
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslblCPUSpeed = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslblRunningAvg = new System.Windows.Forms.ToolStripStatusLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkBoxCpuSpeed = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbWhenInUse = new System.Windows.Forms.ComboBox();
            this.lblOnIdle = new System.Windows.Forms.Label();
            this.cbWhenIdle = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblIdleTimeOut = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.chkWhenLocked = new System.Windows.Forms.CheckBox();
            this.chkboxEnableAutoSwitch = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.statusStrip1.SuspendLayout();
            this.contextMenuStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel,
            this.toolStripStatusLabel1,
            this.tslblCPUSpeed,
            this.toolStripStatusLabel2,
            this.tslblRunningAvg});
            this.statusStrip1.Location = new System.Drawing.Point(0, 285);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(429, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(102, 17);
            this.toolStripStatusLabel.Text = "Sunil TG © 2025";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(11, 17);
            this.toolStripStatusLabel1.Text = "|";
            // 
            // tslblCPUSpeed
            // 
            this.tslblCPUSpeed.Name = "tslblCPUSpeed";
            this.tslblCPUSpeed.Size = new System.Drawing.Size(132, 17);
            this.tslblCPUSpeed.Text = "CPU Speed: 2.34 GHz";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(11, 17);
            this.toolStripStatusLabel2.Text = "|";
            // 
            // tslblRunningAvg
            // 
            this.tslblRunningAvg.Name = "tslblRunningAvg";
            this.tslblRunningAvg.Size = new System.Drawing.Size(150, 17);
            this.tslblRunningAvg.Text = "1 Min Average: 2.34 Ghz";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Current power plan:";
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(117, 16);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(289, 21);
            this.comboBox1.TabIndex = 3;
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "Power Mode Switcher";
            this.notifyIcon.Visible = true;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(106, 48);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.openToolStripMenuItem.Text = "&Open";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(105, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // checkBoxCpuSpeed
            // 
            this.checkBoxCpuSpeed.AutoSize = true;
            this.checkBoxCpuSpeed.Location = new System.Drawing.Point(102, 19);
            this.checkBoxCpuSpeed.Name = "checkBoxCpuSpeed";
            this.checkBoxCpuSpeed.Size = new System.Drawing.Size(121, 17);
            this.checkBoxCpuSpeed.TabIndex = 4;
            this.checkBoxCpuSpeed.Text = "CPU Speed Overlay";
            this.checkBoxCpuSpeed.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "While in use: ";
            // 
            // cbWhenInUse
            // 
            this.cbWhenInUse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbWhenInUse.FormattingEnabled = true;
            this.cbWhenInUse.Location = new System.Drawing.Point(95, 52);
            this.cbWhenInUse.Name = "cbWhenInUse";
            this.cbWhenInUse.Size = new System.Drawing.Size(296, 21);
            this.cbWhenInUse.TabIndex = 3;
            // 
            // lblOnIdle
            // 
            this.lblOnIdle.AutoSize = true;
            this.lblOnIdle.Location = new System.Drawing.Point(13, 82);
            this.lblOnIdle.Name = "lblOnIdle";
            this.lblOnIdle.Size = new System.Drawing.Size(79, 13);
            this.lblOnIdle.TabIndex = 2;
            this.lblOnIdle.Text = "       When idle:";
            // 
            // cbWhenIdle
            // 
            this.cbWhenIdle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbWhenIdle.FormattingEnabled = true;
            this.cbWhenIdle.Location = new System.Drawing.Point(95, 79);
            this.cbWhenIdle.Name = "cbWhenIdle";
            this.cbWhenIdle.Size = new System.Drawing.Size(296, 21);
            this.cbWhenIdle.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblIdleTimeOut);
            this.groupBox1.Controls.Add(this.trackBar1);
            this.groupBox1.Controls.Add(this.chkWhenLocked);
            this.groupBox1.Controls.Add(this.chkboxEnableAutoSwitch);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.lblOnIdle);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cbWhenIdle);
            this.groupBox1.Controls.Add(this.cbWhenInUse);
            this.groupBox1.Location = new System.Drawing.Point(15, 119);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(402, 156);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Power Plan Auto Switch";
            // 
            // lblIdleTimeOut
            // 
            this.lblIdleTimeOut.AutoSize = true;
            this.lblIdleTimeOut.Location = new System.Drawing.Point(354, 113);
            this.lblIdleTimeOut.Name = "lblIdleTimeOut";
            this.lblIdleTimeOut.Size = new System.Drawing.Size(32, 13);
            this.lblIdleTimeOut.TabIndex = 6;
            this.lblIdleTimeOut.Text = "5 min";
            // 
            // trackBar1
            // 
            this.trackBar1.LargeChange = 1;
            this.trackBar1.Location = new System.Drawing.Point(95, 107);
            this.trackBar1.Minimum = 1;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(258, 45);
            this.trackBar1.TabIndex = 5;
            this.trackBar1.Value = 5;
            // 
            // chkWhenLocked
            // 
            this.chkWhenLocked.AutoSize = true;
            this.chkWhenLocked.Location = new System.Drawing.Point(235, 28);
            this.chkWhenLocked.Name = "chkWhenLocked";
            this.chkWhenLocked.Size = new System.Drawing.Size(118, 17);
            this.chkWhenLocked.TabIndex = 4;
            this.chkWhenLocked.Text = "Only When Locked";
            this.chkWhenLocked.UseVisualStyleBackColor = true;
            this.chkWhenLocked.CheckedChanged += new System.EventHandler(this.chkWhenLocked_CheckedChanged);
            // 
            // chkboxEnableAutoSwitch
            // 
            this.chkboxEnableAutoSwitch.AutoSize = true;
            this.chkboxEnableAutoSwitch.Location = new System.Drawing.Point(95, 28);
            this.chkboxEnableAutoSwitch.Name = "chkboxEnableAutoSwitch";
            this.chkboxEnableAutoSwitch.Size = new System.Drawing.Size(119, 17);
            this.chkboxEnableAutoSwitch.TabIndex = 4;
            this.chkboxEnableAutoSwitch.Text = "Enable Auto Switch";
            this.chkboxEnableAutoSwitch.UseVisualStyleBackColor = true;
            this.chkboxEnableAutoSwitch.CheckedChanged += new System.EventHandler(this.chkboxEnableAutoSwitch_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(29, 107);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Idle timeout:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkBoxCpuSpeed);
            this.groupBox2.Location = new System.Drawing.Point(15, 52);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(402, 51);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "General Settings";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 307);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Power Mode Switcher 2.1";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.contextMenuStrip.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel tslblCPUSpeed;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel tslblRunningAvg;
        private System.Windows.Forms.CheckBox checkBoxCpuSpeed;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbWhenInUse;
        private System.Windows.Forms.Label lblOnIdle;
        private System.Windows.Forms.ComboBox cbWhenIdle;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkboxEnableAutoSwitch;
        private System.Windows.Forms.CheckBox chkWhenLocked;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblIdleTimeOut;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

