using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Utilities;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IAGrim.Settings;

namespace IAGrim.UI {
    public partial class LanguagePackPicker : Form {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LanguagePackPicker));
        private readonly List<FirefoxRadioButton> _checkboxes = new List<FirefoxRadioButton>();
        private readonly SettingsService _settings;
        private Panel? _languageList;

        public LanguagePackPicker(SettingsService settings) {
            InitializeComponent();

            _settings = settings;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (keyData == Keys.Enter) {
                buttonSelect_Click(null, null);
                return true;
            }

            if (keyData == Keys.Escape) {
                Close();
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void buttonSelect_Click(object? sender, EventArgs? e) {
            var cb = _checkboxes.FirstOrDefault(m => m.Checked);
            if (cb != null) {
                var selectedCode = cb.Tag?.ToString() ?? string.Empty;

                if (selectedCode != _settings.GetLocal().LanguageCode) {
                    Logger.Info($"Switching language to {selectedCode}");
                    _settings.GetLocal().LanguageCode = selectedCode;

                    MessageBox.Show("IAGD is restarting to apply language change", "Restarting");
                    Application.Restart();
                    Environment.Exit(0);
                }
            }

            Close();
        }

        private void LanguagePackPicker_Load(object sender, EventArgs e) {
            LocalizationLoader.ApplyLanguage(Controls, RuntimeSettings.Language!);

            if (_languageList != null) {
                groupBox1.Controls.Remove(_languageList);
                _languageList.Dispose();
            }
            _checkboxes.Clear();

            var scaleX = pictureBox1.Width / 120F;
            var scaleY = pictureBox1.Height / 119F;
            int ScaleX(int value) => (int)Math.Round(value * scaleX);
            int ScaleY(int value) => (int)Math.Round(value * scaleY);
            var languageList = new Panel {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                Location = new Point(ScaleX(4), ScaleY(18)),
                Size = new Size(
                    pictureBox1.Left - ScaleX(8),
                    groupBox1.ClientSize.Height - ScaleY(22))
            };
            _languageList = languageList;
            groupBox1.Controls.Add(languageList);

            var n = 0;
            var currentCode = _settings.GetLocal().LanguageCode;
            var availableCodes = LanguageMapping.GetSupportedUiLanguages();

            foreach (var code in availableCodes) {
                var displayName = LanguageMapping.GetDisplayName(code);
#if DEBUG
                displayName += $" ({code})";
#endif
                var cb = new FirefoxRadioButton {
                    Location = new Point(ScaleX(6), ScaleY(6 + n * 33)),
                    Text = displayName,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                    Size = new Size(
                        languageList.ClientSize.Width - ScaleX(12) - SystemInformation.VerticalScrollBarWidth,
                        ScaleY(27)),
                    Tag = code,
                    Checked = code.Equals(currentCode, StringComparison.OrdinalIgnoreCase),
                    TabIndex = n,
                    TabStop = true
                };

                languageList.Controls.Add(cb);
                _checkboxes.Add(cb);
                n++;
            }

            languageList.AutoScrollMinSize = new Size(0, ScaleY(12 + n * 33));
        }

        private void LanguagePackPicker_FormClosing(object sender, FormClosingEventArgs e) {
            Program.MainWindow?.UpdateLanguage();
        }
    }
}
