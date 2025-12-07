using Microsoft.SqlServer.Server;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace PowerModes
{
    public partial class MainForm : Form
    {
        private struct SystemState
        {
            public bool IsIdle;
            public bool IsPluggedIn;
            public bool IsLocked;
        }

        // P/Invoke for GetLastInputInfo
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            [MarshalAs(UnmanagedType.U4)]
            public uint cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private Timer cpuSpeedTimer;
        private PerformanceCounter cpuActualFrequencyCounter;
        // Fallback: percent-based performance counter and base frequency (MHz)
        private PerformanceCounter cpuPercentPerformanceCounter;
        private int cpuBaseFrequencyMHz = 0;
        private List<PowerPlan> availablePowerPlans;
        private DateTime lastPowerPlanChange = DateTime.MinValue;
        private const int PowerPlanChangeCooldownMs = 1000; // 1 second cooldown
        private Timer powerPlanChangeTimer;
        private PowerPlan pendingPowerPlan;
        private Queue<float> cpuSpeedSamples;
        private const int MaxSamples = 70;
        private CpuSpeedOverlay cpuOverlay;
        private Timer idleTimeTimer;
        private bool isInitializingUI = false; // Flag to prevent event triggers during initialization
        private uint idleThresholdSeconds = 260; // Idle threshold in seconds (will be updated from config)
        private CheckBox chkShowNotifications; // Reference to show notifications checkbox

        SystemState currentState = new SystemState
        {
            IsIdle = false, // Not idle at startup
            IsPluggedIn = SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online,
            IsLocked = false // Best guess at startup
        };

        public MainForm()
        {
            InitializeComponent();
            
            Logger.Info("MainForm initializing...");
            
            // Get reference to the Show Notifications checkbox (checkBox1 from designer)
            chkShowNotifications = checkBox1;
            
            // Initialize CPU speed samples queue
            cpuSpeedSamples = new Queue<float>();
            
            LoadPowerPlans();


            // Validate and correct power plan configuration if needed
            ConfigManager.ValidateAndCorrectPowerPlanConfig(availablePowerPlans);

            InitializeCPUSpeedMonitor();
            InitializeCpuOverlay();
            this.FormClosing += MainForm_FormClosing;
            
            // Wire up the combobox selection changed event
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            
            // Initialize power plan change timer
            powerPlanChangeTimer = new Timer();
            powerPlanChangeTimer.Tick += PowerPlanChangeTimer_Tick;
            
            // Wire up notify icon events
            openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Wire up checkbox for CPU speed overlay visibility
            checkBoxCpuSpeed.CheckedChanged += CheckBoxCpuSpeed_CheckedChanged;
            isInitializingUI = true; // Prevent event from firing during initialization
            checkBoxCpuSpeed.Checked = true; // Default to checked/visible
            
            // Wire up checkbox for show notifications
            chkShowNotifications.CheckedChanged += ChkShowNotifications_CheckedChanged;
            chkShowNotifications.Checked = ConfigManager.ShowNotifications; // Load from config
            
            isInitializingUI = false;
            
            // Initialize idle time logger timer (5 second interval)
            idleTimeTimer = new Timer();
            idleTimeTimer.Interval = 5000; // 5 seconds
            idleTimeTimer.Tick += IdleTimeTimer_Tick;
            idleTimeTimer.Start();
            
            // Initialize auto-switch UI controls
            InitializeAutoSwitchUI();
            
            // Subscribe to session switch events (lock/unlock) & power mode changes 
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            SystemStateChanged();

            Logger.Info("MainForm initialization complete");
        }



        private void CheckBoxCpuSpeed_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            if (checkBoxCpuSpeed.Checked)
            {
                // Show the overlay
                if (cpuOverlay != null && !cpuOverlay.IsDisposed)
                {
                    cpuOverlay.ShowWindow();
                    Logger.Info("CPU speed overlay shown");
                }
            }
            else
            {
                // Hide the overlay
                if (cpuOverlay != null && !cpuOverlay.IsDisposed)
                {
                    cpuOverlay.HideWindow();
                    Logger.Info("CPU speed overlay hidden");        
                }
            }
        }

        private void ChkShowNotifications_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                bool showNotifications = chkShowNotifications.Checked;
                
                // Save to config
                ConfigManager.ShowNotifications = showNotifications;

                Logger.Info($"Show notifications {(showNotifications ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in ChkShowNotifications_CheckedChanged", ex);
            }
        }

        private void InitializeCpuOverlay()
        {
            cpuOverlay = new CpuSpeedOverlay();
            cpuOverlay.Show();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Prevent action during initial loading
            if (comboBox1.SelectedItem == null)
                return;

            PowerPlan selectedPlan = comboBox1.SelectedItem as PowerPlan;
            if (selectedPlan != null)
            {
                HandlePowerPlanChange(selectedPlan, true);
            }
        }

        private void PowerPlanChangeTimer_Tick(object sender, EventArgs e)
        {
            powerPlanChangeTimer.Stop();
            
            if (pendingPowerPlan != null)
            {
                SetPowerPlan(pendingPowerPlan);
                pendingPowerPlan = null;
            }
        }

        /// <summary>
        /// Handles power plan changes with cooldown management
        /// </summary>
        /// <param name="selectedPlan">The power plan to switch to</param>
        /// <param name="logUserAction">Whether to log this as a user-initiated action</param>
        private void HandlePowerPlanChange(PowerPlan selectedPlan, bool logUserAction = false)
        {
            if (selectedPlan == null)
                return;

            // Don't do anything if the selected plan is already active
            if (selectedPlan.IsActive)
                return;

            // Check if we're within the cooldown period
            TimeSpan timeSinceLastChange = DateTime.Now - lastPowerPlanChange;
            if (timeSinceLastChange.TotalMilliseconds < PowerPlanChangeCooldownMs)
            {
                // Queue the power plan change
                pendingPowerPlan = selectedPlan;
                
                // Calculate remaining time and set timer
                int remainingTime = (int)(PowerPlanChangeCooldownMs - timeSinceLastChange.TotalMilliseconds);
                powerPlanChangeTimer.Stop();
                powerPlanChangeTimer.Interval = remainingTime;
                powerPlanChangeTimer.Start();
            }
            else
            {
                // Apply immediately
                SetPowerPlan(selectedPlan);
                
                if (logUserAction)
                {
                    Logger.Info($"Power plan changed by user to: {selectedPlan.Name}");
                }
            }
        }

        private void UpdateNotifyIconTooltip(string activePowerPlanName)
        {
            notifyIcon.Text = "Power Mode Switcher\nActive: " + activePowerPlanName;
        }


        private void UpdateContextMenu()
        {
            // Remove existing power plan menu items (keep Open and Exit)
            // Remove items from index 1 onwards, but keep the last item (Exit)
            while (contextMenuStrip.Items.Count > 2)
            {
                contextMenuStrip.Items.RemoveAt(1);
            }

            // Add separator after Open
            contextMenuStrip.Items.Insert(1, new ToolStripSeparator());

            // Add power plan menu items
            int insertIndex = 2;
            foreach (PowerPlan plan in availablePowerPlans)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(plan.Name);
                menuItem.Tag = plan;
                menuItem.Checked = plan.IsActive;
                menuItem.Click += PowerPlanTrayMenuItem_Click;
                contextMenuStrip.Items.Insert(insertIndex++, menuItem);
            }

            // Add separator before Exit
            contextMenuStrip.Items.Insert(insertIndex, new ToolStripSeparator());
        }

        private void PowerPlanTrayMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            if (menuItem == null)
                return;

            PowerPlan selectedPlan = menuItem.Tag as PowerPlan;
            HandlePowerPlanChange(selectedPlan);
        }

        private bool TryInitActualFrequencyCounter()
        {
            try
            {
                cpuActualFrequencyCounter = new PerformanceCounter("Processor Information", "Actual Frequency", "_Total");
                // Prime the counter
                cpuActualFrequencyCounter.NextValue();
                Logger.Info("Initialized Actual Frequency performance counter");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to initialize Actual Frequency counter: {ex.Message}");
                cpuActualFrequencyCounter = null;
                return false;
            }
        }

        private int GetProcessorBaseFrequencyGHz()
        {
            // Try reading from registry first (fast, no extra refs)
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("~GHz");
                        if (val != null && int.TryParse(val.ToString(), out int ghz))
                        {
                            return ghz;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            // Fallback: use PowerShell to query WMI/CIM
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell";
                psi.Arguments = "-NoProfile -Command \"(Get-CimInstance -ClassName Win32_Processor | Select-Object -ExpandProperty MaxClockSpeed)\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (int.TryParse(line.Trim(), out int val))
                            return val;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return 0;
        }

        private bool TryInitPercentPerformanceFallback()
        {
            try
            {
                // Get base clock speed (GHz)
                cpuBaseFrequencyMHz = GetProcessorBaseFrequencyGHz();
                if (cpuBaseFrequencyMHz <= 0)
                {
                    Logger.Warning("Could not determine CPU base frequency");
                    return false;
                }

                Logger.Info($"CPU base frequency: {cpuBaseFrequencyMHz} GHz");

                // Try common instance names for Processor Information
                string[] instances = new[] { "0,_Total", "_Total", "0" };
                foreach (string inst in instances)
                {
                    try
                    {
                        cpuPercentPerformanceCounter = new PerformanceCounter("Processor Information", "% Processor Performance", inst);
                        // Prime the counter
                        cpuPercentPerformanceCounter.NextValue();
                        Logger.Info($"Initialized % Processor Performance counter with instance: {inst}");
                        return true;
                    }
                    catch
                    {
                        cpuPercentPerformanceCounter?.Dispose();
                        cpuPercentPerformanceCounter = null;
                    }
                }
                Logger.Warning("Failed to initialize % Processor Performance counter for all instances");
            }
            catch (Exception ex)
            {
                Logger.Warning($"Error initializing performance fallback: {ex.Message}");
            }

            return false;
        }

        private void InitializeCPUSpeedMonitor()
        {
            Logger.Info("Initializing CPU speed monitor...");
            
            // Attempt primary API first
            bool ok = TryInitActualFrequencyCounter();

            if (!ok)
            {
                // If primary counter unavailable, try percent-based fallback
                bool fallbackOk = TryInitPercentPerformanceFallback();

                if (!fallbackOk)
                {
                    // No CPU monitoring available; create timer to keep logic consistent but show unavailable
                    Logger.Warning("CPU monitoring unavailable - no counter initialized");
                    cpuSpeedTimer = new Timer();
                    cpuSpeedTimer.Interval = 1000; // 1s
                    cpuSpeedTimer.Tick += CpuSpeedTimer_Tick;
                    cpuSpeedTimer.Start();

                    tslblCPUSpeed.Text = "CPU Speed: Unavailable";
                    tslblRunningAvg.Text = "1 Min Average: Unavailable";
                    return;
                }
            }

            // Initialize and start the timer
            if (cpuSpeedTimer == null)
            {
                cpuSpeedTimer = new Timer();
                cpuSpeedTimer.Interval = 1000; // 1s
                cpuSpeedTimer.Tick += CpuSpeedTimer_Tick;
                cpuSpeedTimer.Start();
            }
            
            Logger.Info("CPU speed monitor initialized successfully");
        }

        private void CpuSpeedTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                float actualFrequencyMHz = 0f;

                // Prefer Actual Frequency counter if available
                if (cpuActualFrequencyCounter != null)
                {
                    actualFrequencyMHz = cpuActualFrequencyCounter.NextValue();
                }
                else if (cpuPercentPerformanceCounter != null && cpuBaseFrequencyMHz > 0)
                {
                    float percent = cpuPercentPerformanceCounter.NextValue();
                    // percent is percentage of base clock (100 == base frequency)
                    actualFrequencyMHz = (percent / 100.0f) * cpuBaseFrequencyMHz;
                }
                else
                {
                    // No monitoring available
                    tslblCPUSpeed.Text = "CPU Speed: Unavailable";
                    tslblRunningAvg.Text = "1 Min Average: Unavailable";
                    return;
                }

                // Convert to GHz
                float actualFrequencyGHz = actualFrequencyMHz / 1000.0f;

                // Update the label
                tslblCPUSpeed.Text = $"CPU Speed: {actualFrequencyGHz:F2} GHz";

                // Add to samples queue
                cpuSpeedSamples.Enqueue(actualFrequencyGHz);

                // Keep only the last MaxSamples samples
                if (cpuSpeedSamples.Count > MaxSamples)
                {
                    cpuSpeedSamples.Dequeue();
                }

                // Calculate running average
                float runningAverage = cpuSpeedSamples.Average();

                // Update running average label
                tslblRunningAvg.Text = $"1 Min Average: {runningAverage:F2} GHz";

                // Update overlay window
                if (cpuOverlay != null && !cpuOverlay.IsDisposed)
                {
                    cpuOverlay.UpdateCpuSpeed(actualFrequencyGHz, runningAverage);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error reading CPU speed", ex);
                tslblCPUSpeed.Text = "CPU Speed: ...";
                tslblRunningAvg.Text = "1 Min Average: ...";
            }
        }

        

        private void InitializeAutoSwitchUI()
        {
            try
            {
                isInitializingUI = true;

                // Populate plugged-in combo boxes with available power plans
                cbWhenInUse.DisplayMember = "Name";
                cbWhenInUse.ValueMember = "Guid";
                cbWhenInUse.DataSource = new List<PowerPlan>(availablePowerPlans);

                cbWhenIdle.DisplayMember = "Name";
                cbWhenIdle.ValueMember = "Guid";
                cbWhenIdle.DataSource = new List<PowerPlan>(availablePowerPlans);

                // Populate battery combo boxes with available power plans
                cbWhenInUseBatt.DisplayMember = "Name";
                cbWhenInUseBatt.ValueMember = "Guid";
                cbWhenInUseBatt.DataSource = new List<PowerPlan>(availablePowerPlans);

                cbWhenIdleBatt.DisplayMember = "Name";
                cbWhenIdleBatt.ValueMember = "Guid";
                cbWhenIdleBatt.DataSource = new List<PowerPlan>(availablePowerPlans);

                // Load configuration values
                bool autoSwitchEnabled = ConfigManager.IsAutoSwitchEnabled;
                string idlePlanGuid = ConfigManager.IdlePowerPlan;
                string activePlanGuid = ConfigManager.ActiveUsePowerPlan;
                string idlePlanBattGuid = ConfigManager.IdlePowerPlanBatt;
                string activePlanBattGuid = ConfigManager.ActiveUsePowerPlanBatt;
                bool switchWhenLocked = ConfigManager.SwitchOnlyWhenLocked;
                int idleTimeoutMinutes = ConfigManager.IdleTimeoutMinutes;

                // Convert idle timeout minutes to seconds for the idle threshold
                idleThresholdSeconds = (uint)(idleTimeoutMinutes * 60);
                Logger.Info($"Idle threshold set to {idleThresholdSeconds} seconds ({idleTimeoutMinutes} minutes)");

                // Set checkbox state
                chkboxEnableAutoSwitch.Checked =  autoSwitchEnabled;

                // Set plugged-in combo box selections
                if (!string.IsNullOrEmpty(idlePlanGuid) && Guid.TryParse(idlePlanGuid, out Guid idleGuid))
                {
                    cbWhenIdle.SelectedValue = idleGuid;
                }

                if (!string.IsNullOrEmpty(activePlanGuid) && Guid.TryParse(activePlanGuid, out Guid activeGuid))
                {
                    cbWhenInUse.SelectedValue = activeGuid;
                }

                // Set battery combo box selections
                if (!string.IsNullOrEmpty(idlePlanBattGuid) && Guid.TryParse(idlePlanBattGuid, out Guid idleBattGuid))
                {
                    cbWhenIdleBatt.SelectedValue = idleBattGuid;
                }

                if (!string.IsNullOrEmpty(activePlanBattGuid) && Guid.TryParse(activePlanBattGuid, out Guid activeBattGuid))
                {
                    cbWhenInUseBatt.SelectedValue = activeBattGuid;
                }

                // Set "When Locked" checkbox state
                chkWhenLocked.Checked = switchWhenLocked;

                // Set idle timeout trackbar (assuming trackBar1 has a range of 1-60 minutes)
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(idleTimeoutMinutes, trackBar1.Maximum));

                // Update label with idle timeout value
                lblIdleTimeOut.Text = $"{trackBar1.Value} min";

                // Update combo box enabled states
                UpdateAutoSwitchUIState();

                // Wire up event handlers for plugged-in combo boxes
                chkboxEnableAutoSwitch.CheckedChanged += chkboxEnableAutoSwitch_CheckedChanged;
                cbWhenInUse.SelectedValueChanged += CbWhenInUse_SelectedValueChanged;
                cbWhenIdle.SelectedValueChanged += CbWhenIdle_SelectedValueChanged;
                
                // Wire up event handlers for battery combo boxes
                cbWhenInUseBatt.SelectedValueChanged += CbWhenInUseBatt_SelectedValueChanged;
                cbWhenIdleBatt.SelectedValueChanged += CbWhenIdleBatt_SelectedValueChanged;
                
                // Note: chkWhenLocked.CheckedChanged is already wired up in Designer
                trackBar1.ValueChanged += TrackBar1_ValueChanged;

                Logger.Info("Auto-switch UI initialized");
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing auto-switch UI", ex);
            }
            finally
            {
                isInitializingUI = false;
            }
        }

        private void UpdateAutoSwitchUIState()
        {
            // Enable/disable combo boxes based on checkbox state
            // Enable/disable plugged-in combo boxes based on checkbox state
            cbWhenInUse.Enabled = chkboxEnableAutoSwitch.Checked;
            cbWhenIdle.Enabled = chkboxEnableAutoSwitch.Checked;
            
            // Enable/disable battery combo boxes based on checkbox state
            cbWhenInUseBatt.Enabled = chkboxEnableAutoSwitch.Checked;
            cbWhenIdleBatt.Enabled = chkboxEnableAutoSwitch.Checked;
            
            chkWhenLocked.Enabled = chkboxEnableAutoSwitch.Checked;

            // trackbar1 should be enabled if auto-switch is enabled and if chkWhenLocked is not checked
            trackBar1.Enabled = chkboxEnableAutoSwitch.Checked && !chkWhenLocked.Checked;

        }

        private void chkboxEnableAutoSwitch_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                bool isEnabled = chkboxEnableAutoSwitch.Checked;
                
                // Update UI state
                UpdateAutoSwitchUIState();

                // Save to config
                ConfigManager.IsAutoSwitchEnabled = isEnabled;

                if (ConfigManager.IsAutoSwitchEnabled)
                {
                    // Trigger a system state check immediately
                    SystemStateChanged();
                }

                Logger.Info($"Auto-switch {(isEnabled ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in chkboxEnableAutoSwitch_CheckedChanged", ex);
            }
        }

        private void CbWhenInUse_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                if (cbWhenInUse.SelectedValue is Guid selectedGuid)
                {
                    ConfigManager.ActiveUsePowerPlan = selectedGuid.ToString();
                    
                    var selectedPlan = availablePowerPlans.FirstOrDefault(p => p.Guid == selectedGuid);
                    if (selectedPlan != null)
                    {
                        Logger.Info($"Active use power plan changed to: {selectedPlan.Name}");
                        SystemStateChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in CbWhenInUse_SelectedValueChanged", ex);
            }
        }

        private void CbWhenIdle_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                if (cbWhenIdle.SelectedValue is Guid selectedGuid)
                {
                    ConfigManager.IdlePowerPlan = selectedGuid.ToString();
                    
                    var selectedPlan = availablePowerPlans.FirstOrDefault(p => p.Guid == selectedGuid);
                    if (selectedPlan != null)
                    {
                        Logger.Info($"Idle power plan changed to: {selectedPlan.Name}");
                        SystemStateChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in CbWhenIdle_SelectedValueChanged", ex);
            }
        }

        private void CbWhenInUseBatt_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                if (cbWhenInUseBatt.SelectedValue is Guid selectedGuid)
                {
                    ConfigManager.ActiveUsePowerPlanBatt = selectedGuid.ToString();
                    
                    var selectedPlan = availablePowerPlans.FirstOrDefault(p => p.Guid == selectedGuid);
                    if (selectedPlan != null)
                    {
                        Logger.Info($"Active use power plan (battery) changed to: {selectedPlan.Name}");
                        SystemStateChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in CbWhenInUseBatt_SelectedValueChanged", ex);
            }
        }

        private void CbWhenIdleBatt_SelectedValueChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                if (cbWhenIdleBatt.SelectedValue is Guid selectedGuid)
                {
                    ConfigManager.IdlePowerPlanBatt = selectedGuid.ToString();
                    
                    var selectedPlan = availablePowerPlans.FirstOrDefault(p => p.Guid == selectedGuid);
                    if (selectedPlan != null)
                    {
                        Logger.Info($"Idle power plan (battery) changed to: {selectedPlan.Name}");
                        SystemStateChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in CbWhenIdleBatt_SelectedValueChanged", ex);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If user clicked the close button, minimize to tray instead of closing
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }

            Logger.Info("MainForm closing, cleaning up resources...");

            // Unsubscribe from session switch events
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;


            // Clean up overlay
            if (cpuOverlay != null && !cpuOverlay.IsDisposed)
            {
                cpuOverlay.Close();
                cpuOverlay.Dispose();
            }

            // Clean up timers
            if (cpuSpeedTimer != null)
            {
                cpuSpeedTimer.Stop();
                cpuSpeedTimer.Dispose();
            }

            if (powerPlanChangeTimer != null)
            {
                powerPlanChangeTimer.Stop();
                powerPlanChangeTimer.Dispose();
            }

            if (idleTimeTimer != null)
            {
                idleTimeTimer.Stop();
                idleTimeTimer.Dispose();
            }

            // Clean up performance counters
            if (cpuActualFrequencyCounter != null)
            {
                cpuActualFrequencyCounter.Dispose();
            }

            if (cpuPercentPerformanceCounter != null)
            {
                cpuPercentPerformanceCounter.Dispose();
            }
            
            Logger.Info("Cleanup complete");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.Info("Exit requested from context menu");
            
            // Unsubscribe from session switch events
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            
            // Clean up overlay
            if (cpuOverlay != null && !cpuOverlay.IsDisposed)
            {
                cpuOverlay.Close();
                cpuOverlay.Dispose();
            }

            // Perform cleanup
            if (cpuSpeedTimer != null)
            {
                cpuSpeedTimer.Stop();
                cpuSpeedTimer.Dispose();
            }

            if (powerPlanChangeTimer != null)
            {
                powerPlanChangeTimer.Stop();
                powerPlanChangeTimer.Dispose();
            }

            if (idleTimeTimer != null)
            {
                idleTimeTimer.Stop();
                idleTimeTimer.Dispose();
            }

            if (cpuActualFrequencyCounter != null)
            {
                cpuActualFrequencyCounter.Dispose();
            }

            if (cpuPercentPerformanceCounter != null)
            {
                cpuPercentPerformanceCounter.Dispose();
            }

            // Hide notify icon
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }

            Logger.Info("Application exiting");
            
            // Force application exit
            Application.Exit();
        }
    
        // Helper class to store power plan information
        public class PowerPlan
        {
            public Guid Guid { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private void chkWhenLocked_CheckedChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                // if checked, lblOnIdle text should be "When Locked:", otherwise "When Idle:"
                if (chkWhenLocked.Checked)
                {
                    lblOnIdle.Text = lblOnIdleBatt.Text = "When Locked:";
                }
                else
                {
                    lblOnIdle.Text = lblOnIdleBatt.Text = "       When Idle:";
                }
                trackBar1.Enabled = lblIdleTimeOut.Enabled = !chkWhenLocked.Checked;

                // Save to config
                ConfigManager.SwitchOnlyWhenLocked = chkWhenLocked.Checked;

                Logger.Info($"Switch when locked setting changed to: {chkWhenLocked.Checked}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in chkWhenLocked_CheckedChanged", ex);
            }
        }

        private void TrackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (isInitializingUI)
                return;

            try
            {
                int idleTimeoutMinutes = trackBar1.Value;
                ConfigManager.IdleTimeoutMinutes = idleTimeoutMinutes;

                // Update the idle threshold in seconds
                idleThresholdSeconds = (uint)(idleTimeoutMinutes * 60);

                // Update label with formatted idle timeout
                lblIdleTimeOut.Text = $"{idleTimeoutMinutes} min";

                Logger.Info($"Idle timeout changed to: {idleTimeoutMinutes} minutes ({idleThresholdSeconds} seconds)");
            }
            catch (Exception ex)
            {
                Logger.Error("Error in TrackBar1_ValueChanged", ex);
            }
        }

        #region Main events

        private void SystemStateChanged()
        {
            // Return early if auto-switching is disabled
            if (!ConfigManager.IsAutoSwitchEnabled)
                return;

            // Determine power plan based on system state
            // Binary mapping: SwitchOnlyWhenLocked | IsIdle | IsLocked | IsPluggedIn

            bool S = ConfigManager.SwitchOnlyWhenLocked;
            bool I = currentState.IsIdle;
            bool L = currentState.IsLocked;
            bool P = currentState.IsPluggedIn;

            string planGuid;
            string planType;
            string triggerType;

            // Determine if we should treat as idle based on lock state and settings
            bool treatAsIdle = L || (I && !S);

            if (treatAsIdle)
            {
                // Switch to idle power plan
                if (P)
                {
                    planGuid = ConfigManager.IdlePowerPlan;
                    planType = "idle (plugged in)";
                }
                else
                {
                    planGuid = ConfigManager.IdlePowerPlanBatt;
                    planType = "idle (battery)";
                }
                
                if (L)
                {
                    triggerType = "locked";
                }
                else
                {
                    triggerType = "idle";
                }
            }
            else
            {
                // Switch to active power plan
                if (P)
                {
                    planGuid = ConfigManager.ActiveUsePowerPlan;
                    planType = "active (plugged in)";
                }
                else
                {
                    planGuid = ConfigManager.ActiveUsePowerPlanBatt;
                    planType = "active (battery)";
                }
                triggerType = "activity";
            }

            // Switch to the determined power plan
            SwitchToPowerPlan(planGuid, planType, triggerType);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == Microsoft.Win32.PowerModes.StatusChange)
            {
                CheckPowerStatus();
            }
        }

        private void CheckPowerStatus()
        {
            PowerLineStatus status = SystemInformation.PowerStatus.PowerLineStatus;

            if (currentState.IsPluggedIn == (status == PowerLineStatus.Online))
                return;

            // Update current state
            currentState.IsPluggedIn = (status == PowerLineStatus.Online);
            SystemStateChanged();
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
       
            bool newLockState = currentState.IsLocked;

            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                newLockState = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                newLockState = false;
            }

            // Check if lock state changed
            if (newLockState != currentState.IsLocked)
            {
                currentState.IsLocked = newLockState;
                SystemStateChanged();
            }
        }

        private void IdleTimeTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Only process if auto-switch is enabled
                if (!ConfigManager.IsAutoSwitchEnabled)
                    return;

                LASTINPUTINFO liI = new LASTINPUTINFO();
                liI.cbSize = (uint)Marshal.SizeOf(liI);

                if (GetLastInputInfo(ref liI))
                {
                    uint systemUptimeMs = (uint)Environment.TickCount;
                    uint idleTimeMs = systemUptimeMs - liI.dwTime;
                    uint idleTimeSeconds = idleTimeMs / 1000;

                    bool newIdleState = idleTimeSeconds >= idleThresholdSeconds;

                    // Check if idle state changed
                    if (newIdleState != currentState.IsIdle)
                    {
                        currentState.IsIdle = newIdleState;
                        SystemStateChanged();
                    }
                }
                else
                {
                    Logger.Warning("GetLastInputInfo call failed");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting idle time", ex);
            }
        }

        #endregion Main Events 

        #region Powerplan methods
        private void SwitchToPowerPlan(string planGuidString, string planType, string triggerType)
        {
            if (!string.IsNullOrEmpty(planGuidString) && Guid.TryParse(planGuidString, out Guid planGuid))
            {
                var plan = availablePowerPlans.FirstOrDefault(p => p.Guid == planGuid);
                if (plan != null && !plan.IsActive)
                {
                    Logger.Info($"{char.ToUpper(triggerType[0])}{triggerType.Substring(1)} event triggered. Switching to {planType} power plan...");
                    SetPowerPlan(plan);

                    // Show notification only if enabled
                    if (ConfigManager.ShowNotifications)
                    {
                        notifyIcon.ShowBalloonTip(
                            5000,
                            "Power Mode Switcher",
                            $"Power Mode Switcher has applied {plan.Name} power plan after detecting {triggerType}.",
                            ToolTipIcon.Info
                        );
                    }
                }
            }
        }

        private void LoadPowerPlans()
        {
            try
            {
                Logger.Info("Loading available power plans...");

                availablePowerPlans = new List<PowerPlan>();
                Guid? activeGuid = null;
                string activePlanName = string.Empty;

                // Use powercfg to get power plans
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powercfg.exe";
                psi.Arguments = "/list";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse the output
                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        // Look for lines with GUID pattern
                        if (line.Contains("Power Scheme GUID:"))
                        {
                            // Extract GUID
                            int guidStart = line.IndexOf("Power Scheme GUID:") + 18;
                            int guidEnd = line.IndexOf("(", guidStart);
                            if (guidEnd == -1) continue;

                            string guidStr = line.Substring(guidStart, guidEnd - guidStart).Trim();

                            // Extract name
                            int nameStart = line.IndexOf("(", guidEnd) + 1;
                            int nameEnd = line.IndexOf(")", nameStart);
                            if (nameEnd == -1) continue;

                            string name = line.Substring(nameStart, nameEnd - nameStart).Trim();

                            // Check if this is the active plan
                            bool isActive = line.Contains("*");

                            Guid guid;
                            if (Guid.TryParse(guidStr, out guid))
                            {
                                PowerPlan plan = new PowerPlan
                                {
                                    Guid = guid,
                                    Name = name,
                                    IsActive = isActive
                                };

                                availablePowerPlans.Add(plan);

                                if (isActive)
                                {
                                    activeGuid = guid;
                                    activePlanName = name;
                                }
                            }
                        }
                    }
                }

                Logger.Info($"Loaded {availablePowerPlans.Count} power plans. Active: {activePlanName}");

                isInitializingUI = true; // Prevent event triggers during initialization

                // Populate the combobox
                comboBox1.DisplayMember = "Name";
                comboBox1.ValueMember = "Guid";
                comboBox1.DataSource = availablePowerPlans;

                // Select the active power plan
                if (activeGuid.HasValue)
                {
                    for (int i = 0; i < availablePowerPlans.Count; i++)
                    {
                        if (availablePowerPlans[i].Guid == activeGuid.Value)
                        {
                            comboBox1.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Populate context menu with power plans
                UpdateContextMenu();

                // Update notify icon tooltip with active power plan
                if (!string.IsNullOrEmpty(activePlanName))
                {
                    UpdateNotifyIconTooltip(activePlanName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading power plans", ex);
                MessageBox.Show("Error loading power plans: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isInitializingUI = false;
            }
        }

        private void SetPowerPlan(PowerPlan plan)
        {
            try
            {
                // Use powercfg to set the active power plan
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powercfg.exe";
                psi.Arguments = "/setactive " + plan.Guid.ToString();
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        // Update the timestamp of last successful change
                        lastPowerPlanChange = DateTime.Now;

                        // Update the IsActive flag for all plans
                        foreach (PowerPlan powerPlan in availablePowerPlans)
                        {
                            powerPlan.IsActive = (powerPlan.Guid == plan.Guid);
                        }

                        // Update combobox selection
                        for (int i = 0; i < availablePowerPlans.Count; i++)
                        {
                            if (availablePowerPlans[i].Guid == plan.Guid)
                            {
                                comboBox1.SelectedIndex = i;
                                break;
                            }
                        }

                        // Update context menu to reflect the change
                        UpdateContextMenu();

                        // Update notify icon tooltip
                        UpdateNotifyIconTooltip(plan.Name);
                    }
                    else
                    {
                        Logger.Warning($"Failed to change power plan to {plan.Name}. Exit code: {process.ExitCode}");
                        MessageBox.Show("Failed to change power plan.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        // Reload to restore the correct selection
                        LoadPowerPlans();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error changing power plan to {plan.Name}", ex);
                MessageBox.Show("Error changing power plan: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Reload to restore the correct selection
                LoadPowerPlans();
            }
        }
        #endregion Powerplan methods

    }
}
