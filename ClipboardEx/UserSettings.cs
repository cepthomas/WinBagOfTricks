using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.Text.Json;
using NBagOfTricks;
using NBagOfUis;


namespace ClipboardEx
{
    [Serializable]
    public class UserSettings
    {
        /// <summary>The global settings.</summary>
        public static UserSettings TheSettings { get; set; } = new UserSettings();

        #region Persisted editable properties
        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.LimeGreen;

        [DisplayName("Fit Image")]
        [Description("Fit to space or clip.")]
        [Browsable(true)]
        public bool FitImage { get; set; } = true;

        [DisplayName("Key Trigger")]
        [Description("Windows key + this opens clip viewer")]
        [Browsable(true)]
        public Keys KeyTrigger { get; set; } = Keys.Z;
        #endregion

        #region Persisted Non-editable Properties
        //[Browsable(false)]
        //[JsonConverter(typeof(JsonRectangleConverter))]
        //public Rectangle FormGeometry { get; set; } = new Rectangle(50, 50, 800, 800);
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "???";
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, opts);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static void Load(string appDir)
        {
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                UserSettings? set = JsonSerializer.Deserialize<UserSettings>(json);
                TheSettings = set ?? new();
                TheSettings._fn = fn;
            }
            else
            {
                // Doesn't exist, create a new one.
                TheSettings = new UserSettings { _fn = fn };
            }

            //return us;
        }
        #endregion

        #region Edit
        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        public (bool restart, bool navChange) Edit()
        {
            using Form f = new()
            {
                Text = "User Settings",
                Size = new Size(450, 450),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(200, 200),
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            PropertyGridEx pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = this
            };

            // Detect changes of interest.
            bool restart = false;
            bool navChange = false;
            string[] ch = { "ControlColor", "SelectedColor" };

            pg.PropertyValueChanged += (sdr, args) =>
            {
                restart |= ch.Contains(args.ChangedItem.PropertyDescriptor.Name);
                navChange |= args.ChangedItem.PropertyDescriptor.Name == "RootDirs";
            };

            f.Controls.Add(pg);
            f.ShowDialog();

            return (restart, navChange);
        }
        #endregion
    }
}
