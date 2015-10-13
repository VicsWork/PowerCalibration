﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace powercal
{
    public partial class Form_Settings : Form
    {
        public Form_Settings()
        {
            InitializeComponent();

            CheckBoxManualMultiMeter.Checked = Properties.Settings.Default.Meter_Manual_Measurement;
            TextBoxMeterCOM.Text = Properties.Settings.Default.Meter_COM_Port_Name;

            // Pupulate DIO controller types
            comboBoxDIOCtrollerTypes.Items.Clear();
            Array relay_types = Enum.GetValues(typeof(RelayControler.Device_Types));
            foreach (RelayControler.Device_Types relay_type in relay_types)
                comboBoxDIOCtrollerTypes.Items.Add(relay_type.ToString());
            comboBoxDIOCtrollerTypes.Text = Properties.Settings.Default.Relay_Controller_Type;

            // Ember
            comboBoxEmberInterface.Text = Properties.Settings.Default.Ember_Interface;
            if (Properties.Settings.Default.Ember_Interface == "IP")
                textBoxEmberInterfaceAddress.Text = Properties.Settings.Default.Ember_Interface_IP_Address;
            else
                textBoxEmberInterfaceAddress.Text = Properties.Settings.Default.Ember_Interface_USB_Address;
            TextBoxEmberBinPath.Text = Properties.Settings.Default.Ember_BinPath;

            TextBoxCodingX.Text = string.Format("{0}", Properties.Settings.Default.Coding_StatusX);
            TextBoxCodingY.Text = string.Format("{0}", Properties.Settings.Default.Coding_StatusY);

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void setLineEnablement(bool enable)
        {
            foreach (Control ctrl in groupBoxDIO.Controls)
            {
                if (ctrl.GetType() == typeof(NumericUpDown))
                {
                    ctrl.Enabled = enable;
                }
            }
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
        }

        private void CheckBoxManualMultiMeter_CheckedChanged(object sender, EventArgs e)
        {
            if (CheckBoxManualMultiMeter.Checked)
            {
                TextBoxMeterCOM.Enabled = false;
            }
            else
            {
                TextBoxMeterCOM.Enabled = true;
            }

        }

        private void buttonEmberBinPathBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = TextBoxEmberBinPath.Text;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxEmberBinPath.Text = dlg.SelectedPath;
            }
        }

        private void comboBoxDIOCtrollerTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxDIOCtrollerTypes.Text == "Manual")
                setLineEnablement(false);
            else
                setLineEnablement(true);
        }

        private void comboBoxEmberInterface_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Ember_Interface = comboBoxEmberInterface.Text;
            Properties.Settings.Default.Save();

            if (comboBoxEmberInterface.Text == "IP")
                textBoxEmberInterfaceAddress.Text = Properties.Settings.Default.Ember_Interface_IP_Address;
            else
                textBoxEmberInterfaceAddress.Text = Properties.Settings.Default.Ember_Interface_USB_Address;
        }

        private void textBoxEmberInterfaceAddress_TextChanged(object sender, EventArgs e)
        {
            if (comboBoxEmberInterface.Text == "IP")
                Properties.Settings.Default.Ember_Interface_IP_Address = textBoxEmberInterfaceAddress.Text;
            else
                Properties.Settings.Default.Ember_Interface_USB_Address = textBoxEmberInterfaceAddress.Text;
            Properties.Settings.Default.Save();
        }

        private void buttonCodingSetXY_Click(object sender, EventArgs e)
        {
            MouseHook.Start();
            MouseHook.MouseAction += MouseHook_MouseAction;

            buttonCodingSetXY.Enabled = false;
        }

        void MouseHook_MouseAction(object sender, EventArgs e)
        {
            MouseHook.Stop();

            MouseHook.POINT p = new MouseHook.POINT();
            MouseHook.GetCursorPos(out p);

            TextBoxCodingX.Text = string.Format("{0}", p.x);
            TextBoxCodingY.Text = string.Format("{0}", p.y);

            Properties.Settings.Default.Coding_StatusX = p.x;
            Properties.Settings.Default.Coding_StatusY = p.y;
            Properties.Settings.Default.Save();

            buttonCodingSetXY.Enabled = true;

        }

        private void TextBoxCodingX_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Coding_StatusX = Convert.ToInt32(TextBoxCodingX.Text);
            Properties.Settings.Default.Save();

        }

        private void TextBoxCodingY_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Coding_StatusY = Convert.ToInt32(TextBoxCodingY.Text);
            Properties.Settings.Default.Save();
        }

    }
}
