﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Threading;
using System.Text.RegularExpressions;

using MinimalisticTelnet;
using NationalInstruments;
using NationalInstruments.DAQmx;

namespace powercal
{
    enum BoardTypes { Humpback, Hooktooth, Milkshark, Zebrashark };

    public partial class FormMain : Form
    {
        CSSequencer _sq = null;
        MultiMeter _meter = null;
        RelayControler _relay_ctrl = new RelayControler();

        static string _app_data_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".calibration");

        string _log_file = Path.Combine(_app_data_dir, "runlog.txt");
        string _ember_batchfile_path = Path.Combine(_app_data_dir, "patchit.bat");

        double _voltage_low_limit = 0.0;
        double _voltage_high_limit = 0.0;
        double _current_high_limit = 0.0;
        double _current_low_limit = 0.0;
        double _voltage_reference = 0.0;
        double _current_reference = 0.0;

        string _cmd_prefix;

        Process _p_ember_isachan;


        public struct CS_Current_Voltage
        {
            public double Current;
            public double Voltage;

            public CS_Current_Voltage(double i, double v)
            {
                Current = i;
                Voltage = v;
            }
        }

        delegate void SetTextCallback(string txt);

        public FormMain()
        {

            // All dio port 0 lines to 0
            dio_write(0);

            InitializeComponent();

            // Create the app data folder
            if (!Directory.Exists(_app_data_dir))
            {
                Directory.CreateDirectory(_app_data_dir);
            }
            // Init the log file
            initLogFile();

            // Set the title to match assembly info from About dlg
            AboutBox1 aboutdlg = new AboutBox1();
            this.Text = aboutdlg.AssemblyTitle;
            aboutdlg.Dispose();

            // Init the status text box
            initTextBoxRunStatus();

            // Make sure we have a selection for board types
            this.comboBoxBoardTypes.Items.AddRange(Enum.GetNames(typeof(powercal.BoardTypes)));
            if (comboBoxBoardTypes.Items.Count > 0)
            {
                comboBoxBoardTypes.SelectedIndex = 0;
            }

            // Report COM ports found in system
            string[] ports = SerialPort.GetPortNames();
            string msg = "";
            bool detected_cs = false;
            foreach (string portname in ports)
            {
                msg += string.Format("{0}, ", portname);

                if (portname == Properties.Settings.Default.CS_COM_Port_Name)
                {
                    detected_cs = true;
                }
            }
            if (msg != "")
            {
                msg = msg.TrimEnd(new char[] { ' ', ',' });
                msg = string.Format("System serial ports: {0}", msg);
                updateOutputStatus(msg);
            }

            // Detect whether meter is connected to one of the ports
            bool detected_meter = autoDetectMeterCOMPort();
            if (!detected_cs && !detected_meter && ports.Length > 0)
            {
                Properties.Settings.Default.CS_COM_Port_Name = ports[0];
                Properties.Settings.Default.Save();
            }

            // Ember path
            msg = string.Format("Cirrus Logic comunications port = {0}", Properties.Settings.Default.CS_COM_Port_Name);
            updateOutputStatus(msg);

            if (!Directory.Exists(Properties.Settings.Default.Ember_BinPath))
            {
                msg = string.Format("Unable to find Ember bin path \"{0}\"", Properties.Settings.Default.Ember_BinPath);
            }
            else
            {
                msg = string.Format("Ember bin path set at\"{0}\"", Properties.Settings.Default.Ember_BinPath);
            }
            updateOutputStatus(msg);

        }

        /// <summary>
        /// Detects whether the metter is ON and connected to one of the COM ports
        /// If one is found, the serial port setting is changed automatically
        /// </summary>
        /// <returns>Whether a meter was detected connected to the system</returns>
        bool autoDetectMeterCOMPort()
        {
            bool detected = false;
            string[] ports = SerialPort.GetPortNames();
            foreach (string portname in ports)
            {
                MultiMeter meter = new MultiMeter(portname);
                try
                {
                    meter.WaitForDsrHolding = false;
                    meter.OpenComPort();
                    string idn = meter.IDN();
                    meter.CloseSerialPort();

                    if (idn.StartsWith("HEWLETT-PACKARD,34401A"))
                    {
                        detected = true;
                        Properties.Settings.Default.Meter_COM_Port_Name = portname;
                        string msg = string.Format("Multimetter '{0}' comunications port autodetected at {1}", idn.TrimEnd('\n'), Properties.Settings.Default.Meter_COM_Port_Name);
                        updateOutputStatus(msg);
                        break;
                    }

                }
                catch (Exception ex)
                {
                    string msgx = ex.Message;
                }
                meter.CloseSerialPort();
            }
            if (!detected)
            {
                string msg = string.Format("Unable to detect Multimetter comunications port. Using {0}.  Measurements set to manual mode", Properties.Settings.Default.Meter_COM_Port_Name);

                Properties.Settings.Default.Meter_Manual_Measurement = true;
                Properties.Settings.Default.Save();

                updateOutputStatus(msg);
            }

            return detected;

        }

        /// <summary>
        /// Inits the log file.  
        /// Creates folder loction...
        /// </summary>
        void initLogFile()
        {
            if (!Directory.Exists(_app_data_dir))
            {
                Directory.CreateDirectory(_app_data_dir);
            }
            if (!File.Exists(_log_file))
            {
                using (StreamWriter sw = File.CreateText(_log_file))
                {
                    sw.WriteLine("{0:G}: Log created", DateTime.Now);
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Inits the run status text box
        /// </summary>
        void initTextBoxRunStatus()
        {
            this.textBoxRunStatus.BackColor = Color.White;
            this.textBoxRunStatus.ForeColor = Color.Black;
            this.textBoxRunStatus.Clear();
        }

        /// <summary>
        /// Make thread safe call to update status text box
        /// </summary>
        /// <param name="txt"></param>
        private void setOutputStatus(string txt)
        {
            if (this.textBoxOutputStatus.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(setOutputStatus);
                try
                {
                    this.Invoke(d, new object[] { txt });
                }
                catch { }
            }
            else
            {
                updateOutputStatus(txt);
            }
        }

        /// <summary>
        /// Updates the output status text box and log file
        /// </summary>
        /// <param name="txt"></param>
        private void updateOutputStatus(string txt)
        {
            string line = string.Format("{0:G}: {1}\r\n", DateTime.Now, txt);

            Debug.WriteLine(line);

            this.textBoxOutputStatus.AppendText(line);
            this.textBoxOutputStatus.Update();

            using (StreamWriter sw = File.AppendText(_log_file))
            {
                sw.WriteLine("{0:G}: {1}", DateTime.Now, txt);
            }
        }

        private void debugLog(string txt)
        {
            string line = string.Format("{0:G}: {1}\r\n", DateTime.Now, txt);
            Debug.WriteLine(line);
        }

        /// <summary>
        /// Updates the run status text box
        /// </summary>
        /// <param name="txt"></param>
        private void updateRunStatus(string txt)
        {
            string line = string.Format("{0:G}: {1}\r\n", DateTime.Now, txt);

            Debug.WriteLine(line);

            this.textBoxRunStatus.Text = txt;
            this.textBoxRunStatus.Update();
        }

        /// <summary>
        /// Invokes Serial test gld
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSerialTest dlg = new FormSerialTest();
            //DialogResult result = dlg.ShowDialog();
            dlg.Show();
        }

        /// <summary>
        /// Invokes About dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 dlg = new AboutBox1();
            DialogResult result = dlg.ShowDialog();
        }

        /// <summary>
        /// Shows dialog with relays sates defined in the RelayControler when in manual mode
        /// </summary>
        /// <param name="relay_ctrl"></param>
        private void relaysSet(RelayControler relay_ctrl)
        {

            bool manual_relay = Properties.Settings.Default.Manual_Relay_Control;
            if (manual_relay)
            {
                //string msg_dlg = relay_ctrl.ToDlgText();
                string key = "AC Power";
                bool value = _relay_ctrl.ReadLine(key);
                string on_off_str = "OFF";
                if (value)
                    on_off_str = "ON";
                string msg_dlg = string.Format("{0} = {1}\r\n", key, on_off_str);

                key = "Reset";
                value = _relay_ctrl.ReadLine(key);
                on_off_str = "OFF";
                if (value)
                    on_off_str = "ON";
                msg_dlg += string.Format("{0} = {1}\r\n", key, on_off_str);

                key = "Ember";
                value = _relay_ctrl.ReadLine(key);
                on_off_str = "OFF";
                if (value)
                    on_off_str = "ON";
                msg_dlg += string.Format("{0} = {1}\r\n", key, on_off_str);

                key = "Load";
                value = _relay_ctrl.ReadLine(key);
                on_off_str = "OFF";
                if (value)
                    on_off_str = "ON";
                msg_dlg += string.Format("{0} = {1}\r\n", key, on_off_str);

                MessageBox.Show(msg_dlg);
            }
            string status = _relay_ctrl.ToStatusText();
            debugLog(status);
            updateOutputStatus(status);

        }

        private void dio_write(int value)
        {
            try
            {
                string[] channels = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DOPort, PhysicalChannelAccess.External);
                if (channels.Length > 0)
                {
                    using (Task digitalWriteTask = new Task())
                    {
                        digitalWriteTask.DOChannels.CreateChannel(channels[0], "port0", ChannelLineGrouping.OneChannelForAllLines);
                        DigitalSingleChannelWriter writer = new DigitalSingleChannelWriter(digitalWriteTask.Stream);
                        writer.WriteSingleSamplePort(true, (UInt32)value);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            //this.buttonRun.Enabled = false;
            this.textBoxOutputStatus.Clear();
            initTextBoxRunStatus();
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                bool run_using_ember = Properties.Settings.Default.Calibrate_With_Ember;
                if (run_using_ember)
                {
                    calibrate_using_ember();
                }
                else
                {
                    calibrate_using_cirrus();
                }
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                // Format and display the TimeSpan value. 
                string elapsedTime = String.Format("Elaspsed time {0:00} seconds", ts.Seconds);
                updateOutputStatus(elapsedTime);

                if (_sq != null)
                    _sq.CloseSerialPort();

                if (_meter != null)
                    _meter.CloseSerialPort();

                if (_p_ember_isachan != null)
                    _p_ember_isachan.Close();

                Process[] processes = System.Diagnostics.Process.GetProcessesByName("em3xx_load");
                foreach (Process process in processes)
                {
                    process.Kill();
                }

            }
            catch (Exception ex)
            {
                this.textBoxRunStatus.BackColor = Color.Red;
                this.textBoxRunStatus.ForeColor = Color.White;
                updateRunStatus("FAIL");
                updateOutputStatus(ex.Message);

                bool manual_relay = Properties.Settings.Default.Manual_Relay_Control;
                if (manual_relay)
                    _relay_ctrl.Disable = true;
                _relay_ctrl.Ember = false;
                _relay_ctrl.AC_Power = false;
                _relay_ctrl.Load = false;
                _relay_ctrl.Reset = false;
                relaysSet(_relay_ctrl);

                if (_sq != null)
                    _sq.CloseSerialPort();

                if (_meter != null)
                    _meter.CloseSerialPort();
            }
            this.buttonRun.Enabled = true;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.textBoxOutputStatus.Clear();
        }

        /// <summary>
        /// Sets values used for calibration for selected board
        /// </summary>
        private void set_board_calibration_values()
        {
            // Default values (USA)
            double voltage_load = 120;
            double voltage_delta = voltage_load * 0.2;
            double current_load = voltage_load/500; // 500 Ohms
            double current_delta = current_load * 0.2;

            _voltage_reference = 240;
            _current_reference = 15;
            _cmd_prefix = "cs5490"; // UART interface

            powercal.BoardTypes board_type = (powercal.BoardTypes)Enum.Parse(typeof(powercal.BoardTypes), comboBoxBoardTypes.Text);
            switch (board_type)
            {
                case BoardTypes.Humpback:
                    voltage_load = 240;
                    current_load = voltage_load/2000; // 2K Ohms
                    voltage_delta = voltage_load * 0.3;
                    current_delta = current_load * 0.4;
                    break;
                case BoardTypes.Zebrashark:
                    _cmd_prefix = "cs5480";  // SPI interface
                    break;
            }

            _voltage_high_limit = voltage_load + voltage_delta;
            _voltage_low_limit = voltage_load - voltage_delta;
            _current_high_limit = current_load + current_delta;
            _current_low_limit = current_load - current_delta;

        }

        private void calibrate_using_cirrus()
        {

            set_board_calibration_values();

            bool manual_measure = Properties.Settings.Default.Meter_Manual_Measurement;

            string msg;
            updateOutputStatus("===============================Start Calibration==============================");
            string csPortName = Properties.Settings.Default.CS_COM_Port_Name;
            powercal.BoardTypes board_type = (powercal.BoardTypes)Enum.Parse(typeof(powercal.BoardTypes), comboBoxBoardTypes.Text);
            _sq = new CSSequencer(csPortName);

            // Setup multi-meter
            string meterPortName = Properties.Settings.Default.Meter_COM_Port_Name;
            _meter = new MultiMeter(meterPortName);
            if (!manual_measure)
            {
                updateRunStatus("Setup multi-meter");
                _meter.OpenComPort();
                _meter.SetToRemote();
                _meter.ClearError();
                string idn = _meter.IDN();
            }

            // IOffsetPre_Cal
            updateRunStatus("IOffsetPre_Cal");
            bool manual_relay = Properties.Settings.Default.Manual_Relay_Control;
            if (manual_relay)
                _relay_ctrl.Disable = true;
            _relay_ctrl.Ember = false;
            _relay_ctrl.Load = false;
            _relay_ctrl.Reset = true;
            _relay_ctrl.AC_Power = true;
            relaysSet(_relay_ctrl);
            Thread.Sleep(1000);

            _sq.OpenSerialPort();

            _sq.SoftReset();
            Thread.Sleep(500);
            _sq.Init();

            // Set gain to 1
            // IGainCal
            //updateRunStatus("IGainCal Default");
            //int iRMSGainInt = 0x400000;
            //updateOutputStatus(string.Format("IrmsGain = (0x{0:X})", iRMSGainInt));
            //_sq.IGainCal(iRMSGainInt);

            //// VGainCal
            //updateRunStatus("VGainCal");
            //int vRMSGainInt = 0x400000;
            //updateOutputStatus(string.Format("VrmsGain = (0x{0:X})", vRMSGainInt));
            //_sq.VGainCal(vRMSGainInt);

            double acOffsetPre_Cal = _sq.GetIOffset();
            updateOutputStatus(string.Format("ACOffsetPre_Cal = {0:F8}", acOffsetPre_Cal));

            // IRMSNoLoad
            updateRunStatus("IRMSNoLoad");
            double iRMSNoLoad = _sq.IRMSNoLoad();
            if (iRMSNoLoad > 0.05)
            {
                msg = string.Format("Bad IrmsNoLoad value:{08:F}", iRMSNoLoad);
                throw new Exception(msg);
            }
            updateOutputStatus(string.Format("IrmsNoLoad = {0:F8}", iRMSNoLoad));


            // Connect the load
            _relay_ctrl.Load = true;
            relaysSet(_relay_ctrl);

            _sq.StartContinuousConvertion();

            Thread.Sleep(1000);

            if (!manual_measure)
            {
                _meter.SetupForVAC();
                string meter_load_voltage_str = _meter.Measure();
                meter_load_voltage_str = _meter.Measure();
                double meter_load_voltage = Double.Parse(meter_load_voltage_str);
                updateOutputStatus(string.Format("Meter Load Voltage at {0:F8} V", meter_load_voltage));
            }


            // IRMSPre_Cal
            updateRunStatus("IRMSPre_Cal");
            double iRMSPreCal = _sq.GetIRMS();
            // for hooktooth iRMSPreCal < 0.17 || iRMSPreCal > 0.4
            double lowlimit = 0.17;
            double highlimit = 0.4;
            switch (board_type)
            {
                case (powercal.BoardTypes.Humpback):
                    lowlimit = 0.08;
                    highlimit = 0.19;
                    break;
            }

            if (iRMSPreCal < lowlimit || iRMSPreCal > highlimit)
            {
                // With 500 Ohms load and 120VAC this value should be around 0.240 mA
                // With 2k Ohms load and 240VAC this value should be around 0.120 mA
                msg = string.Format("Bad IrmsPreCal value:{0:F8}", iRMSPreCal);
                throw new Exception(msg);
            }
            updateOutputStatus(string.Format("IrmsPreCal = {0:F8}", iRMSPreCal));

            // VRMSPre_Cal
            updateRunStatus("VRMSPre_Cal");
            double vRMSPreCal = _sq.GetVRMS();
            updateOutputStatus(string.Format("VrmsPreCal = {0:F8}", vRMSPreCal));

            if (vRMSPreCal < _voltage_low_limit || vRMSPreCal > _voltage_high_limit)
            {
                msg = string.Format("Bad VrmsPreCal value:{0:F}", vRMSPreCal);
                throw new Exception(msg);
            }
            updateOutputStatus(string.Format("Power before calibration = {0:F8}", vRMSPreCal * iRMSPreCal));

            // IRMSMeasure
            updateRunStatus("IRMSMeasure");
            double iRMSMeasure = 0;
            if (manual_measure)
            {
                // Enter measurement
                FormEnterMeasurement dlg = new FormEnterMeasurement();
                iRMSMeasure = dlg.GetMeasurement("Irms:");
            }
            else
            {
                _meter.SetupForIAC();
                string iac_measurement = _meter.Measure();
                iac_measurement = _meter.Measure();
                iRMSMeasure = Double.Parse(iac_measurement);
            }
            updateOutputStatus(string.Format("IrmsMeasure = {0:F8}", iRMSMeasure));

            // VRMSMeasure
            updateRunStatus("VRMSMeasure");
            double vRMSMeasure = 0;
            if (manual_measure)
            {
                // Enter measurement
                FormEnterMeasurement dlg = new FormEnterMeasurement();
                vRMSMeasure = dlg.GetMeasurement("Vrms:");
            }
            else
            {
                _meter.SetupForVAC();
                string vac_measurement = _meter.Measure();
                vac_measurement = _meter.Measure();
                vRMSMeasure = Double.Parse(vac_measurement);
            }
            updateOutputStatus(string.Format("VrmsMeasure = {0:F8}", vRMSMeasure));
            updateOutputStatus(string.Format("Power measured = {0:F8}", vRMSMeasure * iRMSMeasure));

            // IGainCal
            updateRunStatus("IGainCal");
            double iGain = iRMSMeasure / iRMSPreCal;
            int iRMSGainInt = (int)(iGain * 0x400000);
            updateOutputStatus(string.Format("IrmsGain = {0:F8} (0x{1:X})", iGain, iRMSGainInt));
            _sq.IGainCal(iRMSGainInt);

            // VGainCal
            updateRunStatus("VGainCal");
            double vGain = vRMSMeasure / vRMSPreCal;
            int vRMSGainInt = (int)(vGain * 0x400000);
            updateOutputStatus(string.Format("VrmsGain = {0:F8} (0x{1:X})", vGain, vRMSGainInt));
            _sq.VGainCal(vRMSGainInt);

            // IRMSNoLoad

            // Disconnect the load
            _relay_ctrl.Load = false;
            relaysSet(_relay_ctrl);
            Thread.Sleep(2000);

            updateRunStatus("IRMSNoLoad");

            iRMSNoLoad = _sq.IRMSNoLoad();
            updateOutputStatus(string.Format("IrmsNoLoad = {0:F8}", iRMSNoLoad));
            if (iRMSNoLoad > 0.05)
            {
                msg = string.Format("Bad IrmsNoLoad value:{0:F8}", iRMSNoLoad);
                throw new Exception(msg);
            }

            // Connect the load
            _relay_ctrl.Load = true;
            relaysSet(_relay_ctrl);
            Thread.Sleep(2000);

            // IRMSAfter_Cal
            updateRunStatus("IRMSAfter_Cal");
            double iRMSAfterCal = _sq.GetIRMS();
            updateOutputStatus(string.Format("IrmsAfterCal = {0:F8}", iRMSAfterCal));
            double delta = iRMSMeasure * 0.03;
            lowlimit = iRMSMeasure - delta;
            highlimit = iRMSMeasure + delta;
            if (iRMSAfterCal < lowlimit || iRMSAfterCal > highlimit)
            {
                msg = string.Format("IrmsAfterCal not within limits values: {0:F8} < {1:F8} < {2:F8}", lowlimit, iRMSAfterCal, highlimit);
                Debug.WriteLine(msg);
                throw new Exception(msg);
            }

            // VRMSAfter_Cal
            updateRunStatus("VRMSAfter_Cal");
            double vRMSAfterCal = _sq.GetVRMS();
            updateOutputStatus(string.Format("VrmsAfterCal = {0:F8}", vRMSAfterCal));
            delta = vRMSMeasure * 0.03;
            lowlimit = vRMSMeasure - delta;
            highlimit = vRMSMeasure + delta;
            if (vRMSAfterCal < lowlimit || vRMSAfterCal > highlimit)
            {
                msg = string.Format("VrmsAfterCal not within limits values: {0:F8} < {1:F8} < {2:F8}", lowlimit, vRMSAfterCal, highlimit);
                Debug.WriteLine(msg);
                throw new Exception(msg);
            }
            updateOutputStatus(string.Format("Power after calibration = {0:F8}", vRMSAfterCal * iRMSAfterCal));

            // IGain
            updateRunStatus("IGain");
            int iGaintInt = (int)(iGain * 0x400000);
            updateOutputStatus(string.Format("IrmsAdjust = {0:F8} (0x{1:X})", iGain, iGaintInt));

            // VGain
            updateRunStatus("VGainAdj");
            int vGainInt = (int)(vGain * 0x400000);
            updateOutputStatus(string.Format("VrmsAdjust = {0:F8} (0x{1:X})", vGain, vGainInt));

            // PatchingGain
            updateRunStatus("PatchingGain");
            _relay_ctrl.AC_Power = false;
            _relay_ctrl.Load = false;
            _relay_ctrl.Reset = false;
            _relay_ctrl.Ember = true;
            relaysSet(_relay_ctrl);
            Thread.Sleep(1000);

            Ember ember = new Ember();
            ember.EmberBinPath = Properties.Settings.Default.Ember_BinPath;
            ember.BatchFilePath = _ember_batchfile_path;
            switch (board_type)
            {
                case (powercal.BoardTypes.Humpback):
                    ember.VAdress = 0x08080980;
                    ember.IAdress = 0x08080984;
                    ember.RefereceAdress = 0x08080988;
                    ember.ACOffsetAdress = 0x080809CC;

                    ember.VRefereceValue = 0xF0; // 240 V
                    ember.IRefereceValue = 0x0F; // 15 A

                    break;
                case (powercal.BoardTypes.Zebrashark):
                case (powercal.BoardTypes.Hooktooth):
                case (powercal.BoardTypes.Milkshark):
                    ember.VAdress = 0x08040980;
                    ember.IAdress = 0x08040984;
                    ember.ACOffsetAdress = 0x080409CC;

                    ember.VRefereceValue = 0x78; // 120 V
                    ember.IRefereceValue = 0x0F; // 15 A

                    break;
            }
            ember.CreateCalibrationPatchBath(vGainInt, iGaintInt);

            bool patchit_fail = false;
            string exception_msg = "";
            string coding_output = "";
            // Retry patch loop if fail
            while (true)
            {
                patchit_fail = false;
                exception_msg = "";
                coding_output = "";
                try
                {
                    string output = ember.RunCalibrationPatchBatch();
                    if (output.Contains("ERROR:"))
                    {
                        patchit_fail = true;
                        exception_msg = "Patching error detected:\r\n";
                        exception_msg += output;
                    }
                    coding_output = output;
                }
                catch (Exception e)
                {
                    patchit_fail = true;
                    exception_msg = "Patching exception detected:\r\n";
                    exception_msg += e.Message;
                }

                if (patchit_fail)
                {
                    string retry_err_msg = exception_msg;
                    int max_len = 1000;
                    if (retry_err_msg.Length > max_len)
                        retry_err_msg = retry_err_msg.Substring(0, max_len) + "...";
                    DialogResult dlg_rc = MessageBox.Show(retry_err_msg, "Patching fail", MessageBoxButtons.RetryCancel);
                    if (dlg_rc == System.Windows.Forms.DialogResult.Cancel)
                        break;
                }
                else
                {
                    break;
                }

            }

            if (patchit_fail)
            {
                throw new Exception(exception_msg);
            }
            updateOutputStatus(coding_output);

            // Disconnect Ember
            _relay_ctrl.Ember = false;
            relaysSet(_relay_ctrl);

            updateOutputStatus("================================End Calibration===============================");

            if (_sq != null)
                _sq.CloseSerialPort();
            if (_meter != null)
                _meter.CloseSerialPort();


            if (true)
            {
                this.textBoxRunStatus.BackColor = Color.Green;
                this.textBoxRunStatus.ForeColor = Color.White;
                this.textBoxRunStatus.Text = "PASS";
            }
        }

        private void calibrate_using_ember()
        {
            // Remember to set power to external on ember

            // Set values depending on board type tested
            set_board_calibration_values();

            bool manual_measure = Properties.Settings.Default.Meter_Manual_Measurement;
            powercal.BoardTypes board_type = (powercal.BoardTypes)Enum.Parse(typeof(powercal.BoardTypes), comboBoxBoardTypes.Text);

            string msg;
            updateOutputStatus("===============================Start Calibration==============================");

            // Trun AC ON
            bool manual_relay = Properties.Settings.Default.Manual_Relay_Control;
            if (manual_relay)
                _relay_ctrl.Disable = true;
            _relay_ctrl.Ember = false;
            _relay_ctrl.Load = false;
            _relay_ctrl.Reset = false;

            _relay_ctrl.AC_Power = true;
            _relay_ctrl.Load = true;
            relaysSet(_relay_ctrl);
            Thread.Sleep(1000);

            // Setup multi-meter
            string meterPortName = Properties.Settings.Default.Meter_COM_Port_Name;
            _meter = new MultiMeter(meterPortName);
            if (!manual_measure)
            {
                updateRunStatus("Setup multi-meter");
                _meter.OpenComPort();
                _meter.SetToRemote();
                _meter.ClearError();
                _meter.SetupForVAC();

                string meter_load_voltage_str = _meter.Measure();
                meter_load_voltage_str = _meter.Measure();
                double meter_load_voltage = Double.Parse(meter_load_voltage_str);
                updateOutputStatus(string.Format("Meter Load Voltage at {0:F8} V", meter_load_voltage));

                _meter.CloseSerialPort();
                //string idn = _meter.IDN();

                if (meter_load_voltage < _voltage_low_limit || meter_load_voltage > _voltage_high_limit)
                {
                    msg = string.Format("VrmsAfterCal not within limits values: {0:F8} < {1:F8} < {2:F8}", _voltage_low_limit, meter_load_voltage, _voltage_high_limit);
                    debugLog(msg);
                    throw new Exception(msg);
                }
                
            }

            // Connect Ember
            updateRunStatus("Setup Ember usb id");
            _relay_ctrl.Ember = true;
            relaysSet(_relay_ctrl);

            // I'm not sure we need to do this all the time
            //updateRunStatus("Setup Ember usb id");
            //Process p_ember_usb = new Process()
            //{
            //    StartInfo = new ProcessStartInfo()
            //    {
            //        FileName = Path.Combine(Properties.Settings.Default.Ember_BinPath, "em3xx_load.exe"),
            //        Arguments = "--usb 0",

            //        UseShellExecute = false,
            //        CreateNoWindow = true,
            //        RedirectStandardInput = true,
            //        RedirectStandardOutput = true,
            //        RedirectStandardError = true,
            //    }
            //};
            //p_ember_usb.Start();
            //p_ember_usb.WaitForExit();
            //string output = p_ember_usb.StandardOutput.ReadToEnd();
            //int rc = p_ember_usb.ExitCode;
            //if (rc != 0)
            //{
            //    msg = string.Format("{0} [1} returned {2}\r\n", p_ember_usb.StartInfo.FileName, p_ember_usb.StartInfo.Arguments, rc);
            //    string error = p_ember_usb.StandardError.ReadToEnd();
            //    msg += error;
            //    throw new Exception(msg);
            //}
            //p_ember_usb.Close();
            //debugLog(output);

            // Opem Ember isa channels
            updateRunStatus("Start Ember isachan");
            _p_ember_isachan = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Path.Combine(Properties.Settings.Default.Ember_BinPath, "em3xx_load.exe"),
                    Arguments = "--isachan=all",

                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            _p_ember_isachan.EnableRaisingEvents = true;
            _p_ember_isachan.OutputDataReceived += p_ember_isachan_OutputDataReceived;
            _p_ember_isachan.ErrorDataReceived += p_ember_isachan_ErrorDataReceived;
            _p_ember_isachan.Start();
            _p_ember_isachan.BeginOutputReadLine();
            _p_ember_isachan.BeginErrorReadLine();


            // Create a new telnet connection
            updateRunStatus("Start telnet");
            TelnetConnection tc = new TelnetConnection("localhost", 4900);
            string datain = tc.Read();

            // Patch gain to 1
            updateRunStatus("Patch Gain to 1");
            msg = patch(board_type, 0x400000, 0x400000);
            debugLog(msg);
            Thread.Sleep(3000);
            datain = tc.Read();
            debugLog(datain);

            // Close UUT relay
            updateRunStatus("Close UUT Relay");
            tc.WriteLine("write 1 6 0 1 0x10 {01}");
            Thread.Sleep(1000);
            datain = tc.Read();
            debugLog(datain);

            // Get UUT currect/voltage values
            updateRunStatus("Get UUT values");
            CS_Current_Voltage cv = ember_parse_pinfo_registers(tc, board_type);
            msg = string.Format("Cirrus I = {0:F8}, V = {1:F8}, P = {2:F8}", cv.Current, cv.Voltage, cv.Current * cv.Voltage);
            updateOutputStatus(msg);
            //int i = 0;
            //while (true)
            //{
            //    i++;
            //    if (i > 2)
            //        break;
            //    cv = ember_parse_pinfo_registers(tc, board_type);
            //    msg = string.Format("Cirrus I = {0:F8}, V = {1:F8}, P = {2:F8}", cv.Current, cv.Voltage, cv.Current * cv.Voltage);
            //    updateOutputStatus(msg);
            //}

            if (cv.Voltage < _voltage_low_limit || cv.Voltage > _voltage_high_limit)
            {
                msg = string.Format("Bad VrmsPreCal value:{0:F}", cv.Voltage);
                throw new Exception(msg);
            }
            if (cv.Current < _current_low_limit || cv.Current > _current_high_limit)
            {
                msg = string.Format("Bad IrmsPreCal value:{0:F8}", cv.Current);
                throw new Exception(msg);
            }

            /// The meter measurements
            updateRunStatus("Meter measurements");
            _meter.OpenComPort();
            _meter.SetupForIAC();

            string current_meter_str = _meter.Measure();
            current_meter_str = _meter.Measure();
            double current_meter = Double.Parse(current_meter_str);

            _meter.SetupForVAC();
            string voltage_meter_str = _meter.Measure();
            voltage_meter_str = _meter.Measure();
            double voltage_meter = Double.Parse(voltage_meter_str);

            _meter.CloseSerialPort();

            msg = string.Format("Meter I = {0:F8}, V = {1:F8}, P = {2:F8}", current_meter, voltage_meter, current_meter * voltage_meter);
            updateOutputStatus(msg);


            // Gain calucalation
            updateRunStatus("Gain calucalation");
            double current_gain = current_meter / cv.Current;
            //double current_gain = current_meter / current_cs;
            int current_gain_int = (int)(current_gain * 0x400000);
            msg = string.Format("Current Gain = {0:F8} (0x{1:X})", current_gain, current_gain_int);
            updateOutputStatus(msg);

            double voltage_gain = voltage_meter / cv.Voltage;
            int voltage_gain_int = (int)(voltage_gain * 0x400000);
            msg = string.Format("Voltage Gain = {0:F8} (0x{1:X})", voltage_gain, voltage_gain_int);
            updateOutputStatus(msg);

            // Patch new gain
            updateRunStatus("Patch Gain");
            msg = patch(board_type, voltage_gain_int, current_gain_int);
            Thread.Sleep(3000);
            datain = tc.Read();
            debugLog(datain);

            tc.WriteLine(string.Format("cu {0}_pinfo", _cmd_prefix));
            Thread.Sleep(500);
            datain = tc.Read();
            debugLog(datain);

            // Get UUT currect/voltage values
            updateRunStatus("Get UUT calibrated values");
            cv = ember_parse_pinfo_registers(tc, board_type);
            msg = string.Format("Cirrus I = {0:F8}, V = {1:F8}, P = {2:F8}", cv.Current, cv.Voltage, cv.Current * cv.Voltage);
            updateOutputStatus(msg);

            updateRunStatus("Close telnet");
            tc.Close();

            updateRunStatus("Close Ember isachan");
            _p_ember_isachan.Close();

            _relay_ctrl.AC_Power = false;
            _relay_ctrl.Reset = false;
            _relay_ctrl.Ember = false;
            _relay_ctrl.Load = false;
            relaysSet(_relay_ctrl);

            if (cv.Voltage < _voltage_low_limit || cv.Voltage > _voltage_high_limit)
            {
                msg = string.Format("VrmsAfterCal not within limits values: {0:F8} < {1:F8} < {2:F8}", _voltage_low_limit, cv.Voltage, _voltage_high_limit);
                Debug.WriteLine(msg);
                throw new Exception(msg);
            }

            this.textBoxRunStatus.BackColor = Color.Green;
            this.textBoxRunStatus.ForeColor = Color.White;
            this.textBoxRunStatus.Text = "PASS";

            updateOutputStatus("================================End Calibration===============================");

        }

        CS_Current_Voltage ember_parse_pinfo_registers(TelnetConnection tc, BoardTypes board_type)
        {
            string rawCurrentPattern = "Raw IRMS: ([0-9,A-F]{8})";
            string rawVoltagePattern = "Raw VRMS: ([0-9,A-F]{8})";
            double current_cs = 0.0;
            double voltage_cs = 0.0;

            tc.WriteLine(string.Format("cu {0}_pload", _cmd_prefix));
            Thread.Sleep(500);
            string datain = tc.Read();
            Debug.WriteLine(datain);
            string msg;
            if (datain.Length > 0)
            {
                Match on_off_match = Regex.Match(datain, "Changing OnOff .*");
                if (on_off_match.Success)
                {
                    msg = on_off_match.Value;
                    updateOutputStatus(msg);
                }

                Match match = Regex.Match(datain, rawCurrentPattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for current.  Output was:{0}", datain);
                    throw new Exception(msg);
                }

                string current_hexstr = match.Groups[1].Value;
                int current_int = Convert.ToInt32(current_hexstr, 16);
                current_cs = RegHex_ToDouble(current_int);
                current_cs = current_cs * _current_reference / 0.6;

                voltage_cs = 0.0;
                match = Regex.Match(datain, rawVoltagePattern);
                if (match.Groups.Count != 2)
                {
                    msg = string.Format("Unable to parse pinfo for voltage.  Output was:{0}", datain);
                    throw new Exception(msg);
                }

                string voltage_hexstr = match.Groups[1].Value;
                int volatge_int = Convert.ToInt32(voltage_hexstr, 16);
                voltage_cs = RegHex_ToDouble(volatge_int);
                voltage_cs = voltage_cs * _voltage_reference / 0.6;

            }

            CS_Current_Voltage current_voltage = new CS_Current_Voltage(current_cs, voltage_cs);
            return current_voltage;
        }

        /// <summary>
        /// Converts a 24bit hex (3 bytes) CS register value to a double
        /// </summary>
        /// <example>
        /// byte[] rx_data = new byte[3];
        /// rx_data[2] = 0x5c;
        /// rx_data[1] = 0x28;
        /// rx_data[0] = 0xf6;
        /// Should return midrange =~ 0.36
        /// </example>
        /// <param name="rx_data">data byte array byte[2] <=> MSB ... byte[0] <=> LSB</param>
        /// <returns>range 0 <= value < 1.0</returns>
        private static double RegHex_ToDouble(int data)
        {
            // Maximum 1 =~ 0xFFFFFF
            // Max rms 0.6 =~ 0x999999
            // Half rms 0.36 =~ 0x5C28F6
            double value = ((double)data) / 0x1000000; // 2^24
            return value;
        }

        /// <summary>
        /// Converts a hex string (3 bytes) CS register vaue to a double
        /// </summary>
        /// <param name="hexstr"></param>
        /// <returns>range 0 <= value < 1.0</returns>
        /// <seealso cref="double RegHex_ToDouble(int data)"/>
        private static double RegHex_ToDouble(string hexstr)
        {
            int val_int = Convert.ToInt32(hexstr, 16);
            return RegHex_ToDouble(val_int); ;
        }


        void p_ember_isachan_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            string str = "Error: " + e.Data;
            debugLog(str);
            setOutputStatus(str);
        }

        void p_ember_isachan_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string str = e.Data;
            //setOutputStatus(str);
            debugLog(str);

        }

        /// <summary>
        /// Patches the flash with calibration tokens
        /// </summary>
        /// <param name="board_type"></param>
        /// <param name="voltage_gain"></param>
        /// <param name="current_gain"></param>
        /// <returns></returns>
        string patch(BoardTypes board_type, int voltage_gain, int current_gain)
        {
            Ember ember = new Ember();
            ember.EmberBinPath = Properties.Settings.Default.Ember_BinPath;
            ember.BatchFilePath = _ember_batchfile_path;
            switch (board_type)
            {
                case (powercal.BoardTypes.Humpback):
                    ember.VAdress = 0x08080980;
                    ember.IAdress = 0x08080984;
                    ember.RefereceAdress = 0x08080988;
                    ember.ACOffsetAdress = 0x080809CC;

                    ember.VRefereceValue = 0xF0; // 240 V
                    ember.IRefereceValue = 0x0F; // 15 A

                    break;
                case (powercal.BoardTypes.Zebrashark):
                case (powercal.BoardTypes.Hooktooth):
                case (powercal.BoardTypes.Milkshark):
                    ember.VAdress = 0x08040980;
                    ember.IAdress = 0x08040984;
                    ember.ACOffsetAdress = 0x080409CC;

                    ember.VRefereceValue = 0x78; // 120 V
                    ember.IRefereceValue = 0x0F; // 15 A

                    break;
            }
            ember.CreateCalibrationPatchBath(voltage_gain, current_gain);

            bool patchit_fail = false;
            string exception_msg = "";
            string coding_output = "";
            // Retry patch loop if fail
            while (true)
            {
                patchit_fail = false;
                exception_msg = "";
                coding_output = "";
                try
                {
                    string output = ember.RunCalibrationPatchBatch();
                    if (output.Contains("ERROR:"))
                    {
                        patchit_fail = true;
                        exception_msg = "Patching error detected:\r\n";
                        exception_msg += output;
                    }
                    coding_output = output;
                }
                catch (Exception e)
                {
                    patchit_fail = true;
                    exception_msg = "Patching exception detected:\r\n";
                    exception_msg += e.Message;
                }

                if (patchit_fail)
                {
                    string retry_err_msg = exception_msg;
                    int max_len = 1000;
                    if (retry_err_msg.Length > max_len)
                        retry_err_msg = retry_err_msg.Substring(0, max_len) + "...";
                    DialogResult dlg_rc = MessageBox.Show(retry_err_msg, "Patching fail", MessageBoxButtons.RetryCancel);
                    if (dlg_rc == System.Windows.Forms.DialogResult.Cancel)
                        break;
                }
                else
                {
                    break;
                }

            }

            if (patchit_fail)
            {
                throw new Exception(exception_msg);
            }

            return coding_output;
        }
        /// <summary>
        /// Invokes the DIO test dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void digitalOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormDigitalPortTest dlg = new FormDigitalPortTest();
            //dlg.ShowDialog();
            dlg.Show();
        }

        /// <summary>
        /// Invikes the settings dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings dlg = new FormSettings();

            // COM ports
            dlg.checkBoxUseEmber.Checked = Properties.Settings.Default.Calibrate_With_Ember;

            dlg.TextBoxCirrusCOM.Text = Properties.Settings.Default.CS_COM_Port_Name;

            dlg.TextBoxMeterCOM.Text = Properties.Settings.Default.Meter_COM_Port_Name;
            dlg.CheckBoxManualMultiMeter.Checked = Properties.Settings.Default.Meter_Manual_Measurement;


            // DIO Disable
            dlg.checkBoxDisableDIO.Checked = Properties.Settings.Default.Manual_Relay_Control;

            // DIO line assigment
            dlg.NumericUpDownACPower.Value = Properties.Settings.Default.DIO_ACPower_LineNum;
            dlg.NumericUpDownLoad.Value = Properties.Settings.Default.DIO_Load_LinNum;
            dlg.NumericUpDownReset.Value = Properties.Settings.Default.DIO_Reset_LineNum;
            dlg.NumericUpDownEmber.Value = Properties.Settings.Default.DIO_Ember_LineNum;

            // Ember
            dlg.TextBoxEmberBinPath.Text = Properties.Settings.Default.Ember_BinPath;

            DialogResult rc = dlg.ShowDialog();
            if (rc == DialogResult.OK)
            {
                // COM ports
                Properties.Settings.Default.Calibrate_With_Ember = dlg.checkBoxUseEmber.Checked;

                Properties.Settings.Default.CS_COM_Port_Name = dlg.TextBoxCirrusCOM.Text;

                Properties.Settings.Default.Meter_COM_Port_Name = dlg.TextBoxMeterCOM.Text;
                Properties.Settings.Default.Meter_Manual_Measurement = dlg.CheckBoxManualMultiMeter.Checked;

                // DIO Disable
                Properties.Settings.Default.Manual_Relay_Control = dlg.checkBoxDisableDIO.Checked;

                // DIO line assigment
                Properties.Settings.Default.DIO_ACPower_LineNum = (int)dlg.NumericUpDownACPower.Value;
                Properties.Settings.Default.DIO_Load_LinNum = (int)dlg.NumericUpDownLoad.Value;
                Properties.Settings.Default.DIO_Reset_LineNum = (int)dlg.NumericUpDownReset.Value;
                Properties.Settings.Default.DIO_Ember_LineNum = (int)dlg.NumericUpDownEmber.Value;

                // Ember
                Properties.Settings.Default.Ember_BinPath = dlg.TextBoxEmberBinPath.Text;

                Properties.Settings.Default.Save();
            }
        }

        private void calculatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormCalculator dlg = new FormCalculator();
            dlg.ShowDialog();
        }

        private void toolStripMenuItemPowerMeter_Click(object sender, EventArgs e)
        {
            FormPowerMeter dlg = new FormPowerMeter();
            dlg.Show();
        }

    }
}
