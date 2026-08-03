using IAGrim.Database.Interfaces;
using IAGrim.Parsers.Arz;
using IAGrim.Parsers.GameDataParsing.Model;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Utilities;
using log4net;
using StatTranslator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IAGrim.Settings;
using IAGrim.Settings.Dto;

namespace IAGrim.UI {
    public partial class LanguagePackPicker : Form {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LanguagePackPicker));
        private IEnumerable<string>? _paths;
        private readonly List<FirefoxRadioButton> _checkboxes = new List<FirefoxRadioButton>();
        private readonly IItemTagDao _itemTagDao;
        private readonly IPlayerItemDao _playerItemDao;
        private readonly ParsingService _parsingService;
        private readonly SettingsService _settings;

        public LanguagePackPicker(
            IItemTagDao itemTagDao,
            IPlayerItemDao playerItemDao,
            ParsingService parsingService,
            SettingsService settings
        ) {
            InitializeComponent();

            _parsingService = parsingService;
            _settings = settings;
            _itemTagDao = itemTagDao;
            _playerItemDao = playerItemDao;
        }

        public DialogResult Show(IEnumerable<string> paths) {
            this._paths = paths;
            return ShowDialog();
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
            groupBox1.Controls.Add(languageList);

            var n = 0;
            var currentCode = _settings.GetLocal().LanguageCode;
            var availableCodes = LanguageMapping.GetAvailableLanguages(_paths ?? Enumerable.Empty<string>()).ToList();

            // Always show English first
            if (!availableCodes.Contains("EN")) {
                availableCodes.Insert(0, "EN");
            }

            foreach (var code in availableCodes) {
                var displayName = LanguageMapping.GetDisplayName(code);
#if DEBUG
                displayName += $" ({code})";
#endif
                var isFullySupported = code.Equals("EN", StringComparison.OrdinalIgnoreCase) || LanguageMapping.IsFullySupported(code);
                var prefix = isFullySupported ? "" : "[Partial] ";

                var cb = new FirefoxRadioButton {
                    Location = new Point(ScaleX(6), ScaleY(6 + n * 33)),
                    Text = prefix + displayName,
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
