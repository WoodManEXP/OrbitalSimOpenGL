﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OrbitalSimOpenGL.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.6.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Not finding initial JPL bodies CSV file")]
        public string NoInitJPL_BodiesCSVFile {
            get {
                return ((string)(this["NoInitJPL_BodiesCSVFile"]));
            }
            set {
                this["NoInitJPL_BodiesCSVFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Unable to save bodies CSV file")]
        public string NoSaveBodiesCSV {
            get {
                return ((string)(this["NoSaveBodiesCSV"]));
            }
            set {
                this["NoSaveBodiesCSV"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Gathering ephemerides from JPL Horizons")]
        public string GatheringEphemerides {
            get {
                return ((string)(this["GatheringEphemerides"]));
            }
            set {
                this["GatheringEphemerides"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://ssd.jpl.nasa.gov/horizons.cgi")]
        public string HorizonsCheckURL {
            get {
                return ((string)(this["HorizonsCheckURL"]));
            }
            set {
                this["HorizonsCheckURL"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://ssd.jpl.nasa.gov/horizons_batch.cgi?batch=1&COMMAND=\'{Command}\'&CENTER=\'@" +
            "0\'&MAKE_EPHEM=\'YES\'&EPHEM_TYPE=\'VECTOR\'&VEC_TABLE=\'3\'&START_TIME=\'{StartTime}\'&S" +
            "TOP_TIME=\'{StopTime}\'&STEP_SIZE=\'1h\'&OUT_UNITS=\'KM-S\'&CSV_FORMAT=\'YES\'")]
        public string HorizonsEphemerisURL {
            get {
                return ((string)(this["HorizonsEphemerisURL"]));
            }
            set {
                this["HorizonsEphemerisURL"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("No connection to JPL Horizons")]
        public string NoHorizonsConnection {
            get {
                return ((string)(this["NoHorizonsConnection"]));
            }
            set {
                this["NoHorizonsConnection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("24000000000")]
        public double AxisLength {
            get {
                return ((double)(this["AxisLength"]));
            }
            set {
                this["AxisLength"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Origin")]
        public string Origin {
            get {
                return ((string)(this["Origin"]));
            }
            set {
                this["Origin"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("System barycenter")]
        public string SystemBarycenter {
            get {
                return ((string)(this["SystemBarycenter"]));
            }
            set {
                this["SystemBarycenter"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Properties\\Images")]
        public string ImagesDir {
            get {
                return ((string)(this["ImagesDir"]));
            }
            set {
                this["ImagesDir"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Continue")]
        public string ContinueStr {
            get {
                return ((string)(this["ContinueStr"]));
            }
            set {
                this["ContinueStr"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("InitJPL_Bodies.csv")]
        public string InitJPL_BodiesFile {
            get {
                return ((string)(this["InitJPL_BodiesFile"]));
            }
            set {
                this["InitJPL_BodiesFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SavedJPLBodies.csv")]
        public string SavedJPLBodiesFile {
            get {
                return ((string)(this["SavedJPLBodiesFile"]));
            }
            set {
                this["SavedJPLBodiesFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SavedEphemerisBodies.json")]
        public string SavedEphemerisBodiesFile {
            get {
                return ((string)(this["SavedEphemerisBodiesFile"]));
            }
            set {
                this["SavedEphemerisBodiesFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("21")]
        public int MaxCamMoveScale {
            get {
                return ((int)(this["MaxCamMoveScale"]));
            }
            set {
                this["MaxCamMoveScale"] = value;
            }
        }
    }
}
