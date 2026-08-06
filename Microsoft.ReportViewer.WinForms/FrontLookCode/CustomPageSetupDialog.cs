using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Microsoft.ReportViewer.WinForms.FrontLookCode
{
    /// <summary>
    /// Page setup dialog that keeps metric margin values independent from the
    /// hundredths-of-an-inch values required by System.Drawing.Printing.
    /// </summary>
    public sealed class CustomPageSetupDialog : Form
    {
        private const string MetricMarginFormat = "0.00############################";
        private readonly ComboBox paperSizeComboBox = new ComboBox();
        private readonly ComboBox paperSourceComboBox = new ComboBox();
        private readonly RadioButton portraitRadioButton = new RadioButton();
        private readonly RadioButton landscapeRadioButton = new RadioButton();
        private readonly TextBox leftMarginTextBox = new TextBox();
        private readonly TextBox rightMarginTextBox = new TextBox();
        private readonly TextBox topMarginTextBox = new TextBox();
        private readonly TextBox bottomMarginTextBox = new TextBox();
        private Button acceptButton;
        private Button cancelButton;

        public CustomPageSetupDialog(PrinterSettings printerSettings, CustomPageSetting pageSetting)
        {
            ArgumentNullException.ThrowIfNull(printerSettings);
            ArgumentNullException.ThrowIfNull(pageSetting);

            PrinterSettings = ClonePrinterSettings(printerSettings);
            PageSetting = pageSetting.Clone();

            InitializeDialog();
            LoadPaperSizes();
            LoadPaperSources();
            LoadPageSettingValues();
        }

        public PrinterSettings PrinterSettings { get; private set; }

        public CustomPageSetting PageSetting { get; }

        private void InitializeDialog()
        {
            Text = "Page Setup";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 390);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(root);

            root.Controls.Add(CreatePaperGroup(), 0, 0);
            root.Controls.Add(CreatePageOptions(), 0, 1);
            root.Controls.Add(CreateButtons(), 0, 2);

            AcceptButton = acceptButton;
            CancelButton = cancelButton;
        }

        private Control CreatePaperGroup()
        {
            var group = new GroupBox
            {
                Text = "Paper",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            layout.Controls.Add(CreateLabel("Size:"), 0, 0);
            ConfigureComboBox(paperSizeComboBox);
            layout.Controls.Add(paperSizeComboBox, 1, 0);

            layout.Controls.Add(CreateLabel("Source:"), 0, 1);
            ConfigureComboBox(paperSourceComboBox);
            layout.Controls.Add(paperSourceComboBox, 1, 1);

            group.Controls.Add(layout);
            return group;
        }

        private Control CreatePageOptions()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 8)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(CreateOrientationGroup(), 0, 0);
            layout.Controls.Add(CreateMarginsGroup(), 1, 0);
            return layout;
        }

        private Control CreateOrientationGroup()
        {
            var group = new GroupBox
            {
                Text = "Orientation",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            portraitRadioButton.Text = "Portrait";
            landscapeRadioButton.Text = "Landscape";
            layout.Controls.Add(portraitRadioButton);
            layout.Controls.Add(landscapeRadioButton);
            group.Controls.Add(layout);
            return group;
        }

        private Control CreateMarginsGroup()
        {
            var group = new GroupBox
            {
                Text = "Margins (millimetres)",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            AddMarginField(layout, "Left:", leftMarginTextBox, 0, 0);
            AddMarginField(layout, "Right:", rightMarginTextBox, 2, 0);
            AddMarginField(layout, "Top:", topMarginTextBox, 0, 1);
            AddMarginField(layout, "Bottom:", bottomMarginTextBox, 2, 1);

            group.Controls.Add(layout);
            return group;
        }

        private Control CreateButtons()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            cancelButton = CreateCancelButton();
            acceptButton = CreateAcceptButton();
            panel.Controls.Add(cancelButton);
            panel.Controls.Add(acceptButton);

            var printerButton = new Button
            {
                Text = "Printer...",
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            printerButton.Click += SelectPrinter;
            panel.Controls.Add(printerButton);
            return panel;
        }

        private Button CreateAcceptButton()
        {
            var button = new Button
            {
                Text = "OK",
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
            button.Click += AcceptChanges;
            return button;
        }

        private static Button CreateCancelButton()
        {
            return new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                UseVisualStyleBackColor = true
            };
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static void ConfigureComboBox(ComboBox comboBox)
        {
            comboBox.Dock = DockStyle.Fill;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private static void AddMarginField(TableLayoutPanel layout, string label, TextBox textBox, int column, int row)
        {
            layout.Controls.Add(CreateLabel(label), column, row);
            textBox.Dock = DockStyle.Fill;
            textBox.Margin = new Padding(4, 3, 8, 3);
            textBox.TextAlign = HorizontalAlignment.Right;
            layout.Controls.Add(textBox, column + 1, row);
        }

        private void LoadPageSettingValues()
        {
            portraitRadioButton.Checked = !PageSetting.Landscape;
            landscapeRadioButton.Checked = PageSetting.Landscape;

            var margins = PageSetting.Clone();
            var pageSettings = margins.GetPageSettings();
            SetMarginText(leftMarginTextBox, margins.LeftMarginMillimeters ?? ToMillimeters(pageSettings.Margins.Left));
            SetMarginText(rightMarginTextBox, margins.RightMarginMillimeters ?? ToMillimeters(pageSettings.Margins.Right));
            SetMarginText(topMarginTextBox, margins.TopMarginMillimeters ?? ToMillimeters(pageSettings.Margins.Top));
            SetMarginText(bottomMarginTextBox, margins.BottomMarginMillimeters ?? ToMillimeters(pageSettings.Margins.Bottom));
        }

        private static void SetMarginText(TextBox textBox, decimal value)
        {
            textBox.Text = value.ToString(MetricMarginFormat, CultureInfo.CurrentCulture);
        }

        private void LoadPaperSizes()
        {
            paperSizeComboBox.Items.Clear();
            var paperSizes = PrinterSettings.IsValid
                ? PrinterSettings.PaperSizes.Cast<PaperSize>()
                : Enumerable.Empty<PaperSize>();

            foreach (var paperSize in paperSizes)
            {
                AddPaperSizeIfMissing(paperSize);
            }

            if (PageSetting.PaperSize != null)
            {
                AddPaperSizeIfMissing(PageSetting.PaperSize);
            }

            SelectPaperSize(PageSetting.PaperSize);
        }

        private void LoadPaperSources()
        {
            paperSourceComboBox.Items.Clear();
            var paperSources = PrinterSettings.IsValid
                ? PrinterSettings.PaperSources.Cast<PaperSource>()
                : Enumerable.Empty<PaperSource>();

            foreach (var paperSource in paperSources)
            {
                paperSourceComboBox.Items.Add(new PaperSourceItem(paperSource));
            }

            if (PageSetting.PaperSource != null && !paperSourceComboBox.Items.Cast<PaperSourceItem>().Any(x => IsSamePaperSource(x.PaperSource, PageSetting.PaperSource)))
            {
                paperSourceComboBox.Items.Add(new PaperSourceItem(PageSetting.PaperSource));
            }

            var selectedSource = paperSourceComboBox.Items.Cast<PaperSourceItem>().FirstOrDefault(x => IsSamePaperSource(x.PaperSource, PageSetting.PaperSource));
            if (selectedSource != null)
            {
                paperSourceComboBox.SelectedItem = selectedSource;
            }
            else if (paperSourceComboBox.Items.Count > 0)
            {
                paperSourceComboBox.SelectedIndex = 0;
            }
        }

        private void AddPaperSizeIfMissing(PaperSize paperSize)
        {
            if (paperSize == null || paperSizeComboBox.Items.Cast<PaperSizeItem>().Any(x => IsSamePaperSize(x.PaperSize, paperSize)))
            {
                return;
            }

            paperSizeComboBox.Items.Add(new PaperSizeItem(paperSize));
        }

        private void SelectPaperSize(PaperSize paperSize)
        {
            var selectedSize = paperSizeComboBox.Items.Cast<PaperSizeItem>().FirstOrDefault(x => IsSamePaperSize(x.PaperSize, paperSize));
            if (selectedSize != null)
            {
                paperSizeComboBox.SelectedItem = selectedSize;
            }
            else if (paperSizeComboBox.Items.Count > 0)
            {
                paperSizeComboBox.SelectedIndex = 0;
            }
        }

        private void SelectPrinter(object sender, EventArgs e)
        {
            using var printDialog = new PrintDialog
            {
                PrinterSettings = ClonePrinterSettings(PrinterSettings),
                AllowSomePages = false,
                AllowSelection = false,
                AllowPrintToFile = false,
                UseEXDialog = false
            };

            if (printDialog.ShowDialog(this) == DialogResult.OK)
            {
                PrinterSettings = printDialog.PrinterSettings;
                LoadPaperSizes();
                LoadPaperSources();
            }
        }

        private void AcceptChanges(object sender, EventArgs e)
        {
            try
            {
                var left = ParseMargin(leftMarginTextBox, "Left");
                var right = ParseMargin(rightMarginTextBox, "Right");
                var top = ParseMargin(topMarginTextBox, "Top");
                var bottom = ParseMargin(bottomMarginTextBox, "Bottom");

                PageSetting.PaperSize = (paperSizeComboBox.SelectedItem as PaperSizeItem)?.PaperSize ?? PageSetting.PaperSize;
                PageSetting.PaperSource = (paperSourceComboBox.SelectedItem as PaperSourceItem)?.PaperSource ?? PageSetting.PaperSource;
                PageSetting.Landscape = landscapeRadioButton.Checked;
                PageSetting.SetMetricMargins(left, right, top, bottom);
                DialogResult = DialogResult.OK;
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static decimal ParseMargin(TextBox textBox, string name)
        {
            var text = textBox.Text.Trim();
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) &&
                !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                throw new ArgumentException($"{name} margin must be a valid number.");
            }

            if (value < 0m)
            {
                throw new ArgumentException($"{name} margin cannot be negative.");
            }

            return value;
        }

        private static decimal ToMillimeters(int hundredthsOfAnInch)
        {
            return hundredthsOfAnInch * 25.4m / 100m;
        }

        private static PrinterSettings ClonePrinterSettings(PrinterSettings source)
        {
            var clone = new PrinterSettings();
            try
            {
                clone.PrinterName = source.PrinterName;
            }
            catch
            {
                // Keep the default printer when the stored printer is unavailable.
            }

            clone.Copies = source.Copies;
            clone.Collate = source.Collate;
            clone.PrintRange = source.PrintRange;
            clone.FromPage = source.FromPage;
            clone.ToPage = source.ToPage;
            clone.MinimumPage = source.MinimumPage;
            clone.MaximumPage = source.MaximumPage;
            return clone;
        }

        private static bool IsSamePaperSize(PaperSize first, PaperSize second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }

            return first.RawKind == second.RawKind ||
                   (first.Width == second.Width && first.Height == second.Height && string.Equals(first.PaperName, second.PaperName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSamePaperSource(PaperSource first, PaperSource second)
        {
            if (first == null || second == null)
            {
                return first == second;
            }

            return first.RawKind == second.RawKind || string.Equals(first.SourceName, second.SourceName, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class PaperSizeItem
        {
            public PaperSizeItem(PaperSize paperSize)
            {
                PaperSize = paperSize;
            }

            public PaperSize PaperSize { get; }

            public override string ToString() => PaperSize.PaperName;
        }

        private sealed class PaperSourceItem
        {
            public PaperSourceItem(PaperSource paperSource)
            {
                PaperSource = paperSource;
            }

            public PaperSource PaperSource { get; }

            public override string ToString() => PaperSource.SourceName;
        }
    }
}
