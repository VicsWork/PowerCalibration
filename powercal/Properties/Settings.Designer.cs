﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PowerCalibration.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("COM1")]
        public string Meter_COM_Port_Name {
            get {
                return ((string)(this["Meter_COM_Port_Name"]));
            }
            set {
                this["Meter_COM_Port_Name"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Program Files (x86)\\Ember\\ISA3 Utilities\\bin")]
        public string Ember_BinPath {
            get {
                return ((string)(this["Ember_BinPath"]));
            }
            set {
                this["Ember_BinPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("FT232H")]
        public string Relay_Controller_Type {
            get {
                return ((string)(this["Relay_Controller_Type"]));
            }
            set {
                this["Relay_Controller_Type"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string Last_Used_Board {
            get {
                return ((string)(this["Last_Used_Board"]));
            }
            set {
                this["Last_Used_Board"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Meter_Manual_Measurement {
            get {
                return ((bool)(this["Meter_Manual_Measurement"]));
            }
            set {
                this["Meter_Manual_Measurement"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("IP")]
        public string Ember_Interface {
            get {
                return ((string)(this["Ember_Interface"]));
            }
            set {
                this["Ember_Interface"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("172.19.14.93")]
        public string Ember_Interface_IP_Address {
            get {
                return ((string)(this["Ember_Interface_IP_Address"]));
            }
            set {
                this["Ember_Interface_IP_Address"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public string Ember_Interface_USB_Address {
            get {
                return ((string)(this["Ember_Interface_USB_Address"]));
            }
            set {
                this["Ember_Interface_USB_Address"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("All")]
        public string Shortcut_Spacebar_Action {
            get {
                return ((string)(this["Shortcut_Spacebar_Action"]));
            }
            set {
                this["Shortcut_Spacebar_Action"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool PrePost_Test_Enabled {
            get {
                return ((bool)(this["PrePost_Test_Enabled"]));
            }
            set {
                this["PrePost_Test_Enabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DB_Loging_Enabled {
            get {
                return ((bool)(this["DB_Loging_Enabled"]));
            }
            set {
                this["DB_Loging_Enabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Centralite")]
        public string ProductionSiteName {
            get {
                return ((string)(this["ProductionSiteName"]));
            }
            set {
                this["ProductionSiteName"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=zeus.centralite.com;Initial Catalog=ManufacturingStore_v2;Integrated " +
            "Security=FALSE;User Id=mfgTester;Password=mfgTester")]
        public string DBConnectionString {
            get {
                return ((string)(this["DBConnectionString"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=a1040.centralite.com;Initial Catalog=PowerCalibration;Integrated Secu" +
            "rity=True")]
        public string DBConnectionString_Debug {
            get {
                return ((string)(this["DBConnectionString_Debug"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("120")]
        public double CalibrationLoadVoltageValue {
            get {
                return ((double)(this["CalibrationLoadVoltageValue"]));
            }
            set {
                this["CalibrationLoadVoltageValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public double CalibrationLoadVoltageTolarance {
            get {
                return ((double)(this["CalibrationLoadVoltageTolarance"]));
            }
            set {
                this["CalibrationLoadVoltageTolarance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("500")]
        public double CalibrationLoadResistorValue {
            get {
                return ((double)(this["CalibrationLoadResistorValue"]));
            }
            set {
                this["CalibrationLoadResistorValue"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public double CalibrationLoadResistorTolarance {
            get {
                return ((double)(this["CalibrationLoadResistorTolarance"]));
            }
            set {
                this["CalibrationLoadResistorTolarance"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CodeMinimizedOnPASS {
            get {
                return ((bool)(this["CodeMinimizedOnPASS"]));
            }
            set {
                this["CodeMinimizedOnPASS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=172.24.16.100;Initial Catalog=PowerCalibration;Integrated Security=Tr" +
            "ue")]
        public string DBConnectionString_Keytronics {
            get {
                return ((string)(this["DBConnectionString_Keytronics"]));
            }
            set {
                this["DBConnectionString_Keytronics"] = value;
            }
        }
    }
}
