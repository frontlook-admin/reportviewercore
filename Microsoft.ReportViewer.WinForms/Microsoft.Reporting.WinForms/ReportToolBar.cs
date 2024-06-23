using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    internal class ReportToolBar : UserControl
    {
        public class ToolStripButtonOverride : ToolStripButton
        {
            public override bool CanSelect => Enabled;
        }

        private bool m_ignoreZoomEvents;

        private ReportViewer m_currentViewerControl;
        private ToolStripButtonOverride firstPage;
        private ToolStripButtonOverride previousPage;
        private ToolStripTextBox currentPage;

        private ToolStripLabel labelOf;

        private ToolStripLabel totalPages;
        private ToolStripButtonOverride nextPage;
        private ToolStripButtonOverride lastPage;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButtonOverride back;
        private ToolStripButtonOverride stop;
        private ToolStripButtonOverride refresh;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButtonOverride PrintDialog;
        private ToolStripButtonOverride printPreview;
        private ToolStripSeparator separator4;

        private ToolStrip toolStrip1;

        private ToolStripComboBox zoom;

        private ToolStripTextBox textToFind;
        private ToolStripButtonOverride find;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButtonOverride findNext;
        private ToolStripButtonOverride pageSetup;
        private ToolStripButton DirectPrint;
        private ToolStripButton zoomIn;
        private ToolStripButton zoomOut;
        private ToolStripDropDownButton export;

        public override Size MinimumSize
        {
            get
            {
                return GetIdealSize();
            }
            set
            {
            }
        }

        public override Size MaximumSize
        {
            get
            {
                return GetIdealSize();
            }
            set
            {
            }
        }

        internal ReportViewer ViewerControl
        {
            get
            {
                return m_currentViewerControl;
            }
            set
            {
                if (m_currentViewerControl != null)
                {
                    m_currentViewerControl.StatusChanged -= OnReportViewerStateChanged;
                }
                m_currentViewerControl = value;
                if (m_currentViewerControl != null)
                {
                    m_currentViewerControl.StatusChanged += OnReportViewerStateChanged;
                }
            }
        }

        public event ZoomChangedEventHandler ZoomChange;

        public event PageNavigationEventHandler PageNavigation;

        public event ExportEventHandler Export;

        public event SearchEventHandler Search;

        public event EventHandler ReportRefresh;

        public event EventHandler Print;

        public event EventHandler DPrint;

        public event EventHandler Back;

        public event EventHandler PageSetup;

        public ReportToolBar()
        {
            InitializeComponent();
            using (Bitmap image = new Bitmap(1, 1))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    currentPage.Width = graphics.MeasureString("12345", currentPage.Font).ToSize().Width;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                currentPage.TextBox.Visible = false;
                textToFind.TextBox.Visible = false;
            }
            base.Dispose(disposing);
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!base.DesignMode)
            {
                ApplyLocalizedResources();
            }
            base.OnLoad(e);
        }

        internal void ApplyCustomResources()
        {
            firstPage.ToolTipText = LocalizationHelper.Current.FirstPageButtonToolTip;
            previousPage.ToolTipText = LocalizationHelper.Current.PreviousPageButtonToolTip;
            currentPage.ToolTipText = LocalizationHelper.Current.CurrentPageTextBoxToolTip;
            labelOf.Text = LocalizationHelper.Current.PageOf;
            totalPages.ToolTipText = LocalizationHelper.Current.TotalPagesToolTip;
            nextPage.ToolTipText = LocalizationHelper.Current.NextPageButtonToolTip;
            lastPage.ToolTipText = LocalizationHelper.Current.LastPageButtonToolTip;
            back.ToolTipText = LocalizationHelper.Current.BackButtonToolTip;
            stop.ToolTipText = LocalizationHelper.Current.StopButtonToolTip;
            refresh.ToolTipText = LocalizationHelper.Current.RefreshButtonToolTip;
            PrintDialog.ToolTipText = LocalizationHelper.Current.PrintButtonToolTip;
            printPreview.ToolTipText = LocalizationHelper.Current.PrintLayoutButtonToolTip;
            pageSetup.ToolTipText = LocalizationHelper.Current.PageSetupButtonToolTip;
            export.ToolTipText = LocalizationHelper.Current.ExportButtonToolTip;
            zoom.ToolTipText = LocalizationHelper.Current.ZoomControlToolTip;
            textToFind.ToolTipText = LocalizationHelper.Current.SearchTextBoxToolTip;
            find.Text = LocalizationHelper.Current.FindButtonText;
            find.ToolTipText = LocalizationHelper.Current.FindButtonToolTip;
            findNext.Text = LocalizationHelper.Current.FindNextButtonText;
            findNext.ToolTipText = LocalizationHelper.Current.FindNextButtonToolTip;
        }

        private void ApplyLocalizedResources()
        {
            firstPage.AccessibleName = ReportPreviewStrings.FirstPageAccessibleName;
            previousPage.AccessibleName = ReportPreviewStrings.PreviousPageAccessibleName;
            currentPage.AccessibleName = ReportPreviewStrings.CurrentPageAccessibleName;
            nextPage.AccessibleName = ReportPreviewStrings.NextPageAccessibleName;
            lastPage.AccessibleName = ReportPreviewStrings.LastPageAccessibleName;
            back.AccessibleName = ReportPreviewStrings.BackAccessibleName;
            stop.AccessibleName = ReportPreviewStrings.StopAccessibleName;
            refresh.AccessibleName = ReportPreviewStrings.RefreshAccessibleName;
            PrintDialog.AccessibleName = ReportPreviewStrings.PrintAccessibleName;
            printPreview.AccessibleName = ReportPreviewStrings.PrintPreviewAccessibleName;
            pageSetup.AccessibleName = ReportPreviewStrings.PageSetupAccessibleName;
            export.AccessibleDescription = ReportPreviewStrings.ExportAccessibleDescription;
            export.AccessibleName = ReportPreviewStrings.ExportAccessibleName;
            zoom.AccessibleName = ReportPreviewStrings.ZoomAccessibleName;
            textToFind.AccessibleName = ReportPreviewStrings.SearchTextBoxAccessibleName;
            find.AccessibleName = ReportPreviewStrings.FindAccessibleName;
            findNext.AccessibleName = ReportPreviewStrings.FindNextAccessibleName;
            base.AccessibleName = ReportPreviewStrings.ReportToolBarAccessibleName;
            ApplyCustomResources();
        }

        internal void SetToolStripRenderer(ToolStripRenderer renderer)
        {
            toolStrip1.Renderer = renderer;
        }

        private Size GetIdealSize()
        {
            Size result = base.Size;
            if (toolStrip1 != null && base.Parent != null)
            {
                result = new Size(base.Parent.Width, toolStrip1.PreferredSize.Height);
            }
            return result;
        }

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(ReportToolBar));
            firstPage = new ToolStripButtonOverride();
            previousPage = new ToolStripButtonOverride();
            currentPage = new ToolStripTextBox();
            labelOf = new ToolStripLabel();
            totalPages = new ToolStripLabel();
            nextPage = new ToolStripButtonOverride();
            lastPage = new ToolStripButtonOverride();
            toolStripSeparator2 = new ToolStripSeparator();
            back = new ToolStripButtonOverride();
            stop = new ToolStripButtonOverride();
            refresh = new ToolStripButtonOverride();
            toolStrip1 = new ToolStrip();
            toolStripSeparator3 = new ToolStripSeparator();
            DirectPrint = new ToolStripButton();
            PrintDialog = new ToolStripButtonOverride();
            printPreview = new ToolStripButtonOverride();
            pageSetup = new ToolStripButtonOverride();
            export = new ToolStripDropDownButton();
            separator4 = new ToolStripSeparator();
            zoomIn = new ToolStripButton();
            zoom = new ToolStripComboBox();
            zoomOut = new ToolStripButton();
            textToFind = new ToolStripTextBox();
            find = new ToolStripButtonOverride();
            toolStripSeparator4 = new ToolStripSeparator();
            findNext = new ToolStripButtonOverride();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // firstPage
            // 
            firstPage.DisplayStyle = ToolStripItemDisplayStyle.Image;
            firstPage.Image = (Image)resources.GetObject("firstPage.Image");
            firstPage.ImageTransparentColor = Color.Fuchsia;
            firstPage.Name = "firstPage";
            firstPage.RightToLeftAutoMirrorImage = true;
            firstPage.Size = new Size(23, 22);
            firstPage.Click += OnPageNavButtonClick;
            // 
            // previousPage
            // 
            previousPage.DisplayStyle = ToolStripItemDisplayStyle.Image;
            previousPage.Image = (Image)resources.GetObject("previousPage.Image");
            previousPage.ImageTransparentColor = Color.Fuchsia;
            previousPage.Name = "previousPage";
            previousPage.RightToLeftAutoMirrorImage = true;
            previousPage.Size = new Size(23, 22);
            previousPage.Click += OnPageNavButtonClick;
            // 
            // currentPage
            // 
            currentPage.AcceptsReturn = true;
            currentPage.AcceptsTab = true;
            currentPage.MaxLength = 10;
            currentPage.Name = "currentPage";
            currentPage.Size = new Size(40, 25);
            currentPage.WordWrap = false;
            currentPage.KeyPress += CurrentPage_KeyPress;
            // 
            // labelOf
            // 
            labelOf.DisplayStyle = ToolStripItemDisplayStyle.Text;
            labelOf.ImageTransparentColor = Color.Fuchsia;
            labelOf.Name = "labelOf";
            labelOf.Size = new Size(0, 22);
            // 
            // totalPages
            // 
            totalPages.DisplayStyle = ToolStripItemDisplayStyle.Text;
            totalPages.ImageTransparentColor = Color.Fuchsia;
            totalPages.Name = "totalPages";
            totalPages.Size = new Size(0, 22);
            totalPages.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nextPage
            // 
            nextPage.DisplayStyle = ToolStripItemDisplayStyle.Image;
            nextPage.Image = (Image)resources.GetObject("nextPage.Image");
            nextPage.ImageTransparentColor = Color.Fuchsia;
            nextPage.Name = "nextPage";
            nextPage.RightToLeftAutoMirrorImage = true;
            nextPage.Size = new Size(23, 22);
            nextPage.Click += OnPageNavButtonClick;
            // 
            // lastPage
            // 
            lastPage.DisplayStyle = ToolStripItemDisplayStyle.Image;
            lastPage.Image = (Image)resources.GetObject("lastPage.Image");
            lastPage.ImageTransparentColor = Color.Fuchsia;
            lastPage.Name = "lastPage";
            lastPage.RightToLeftAutoMirrorImage = true;
            lastPage.Size = new Size(23, 22);
            lastPage.Click += OnPageNavButtonClick;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // back
            // 
            back.Image = (Image)resources.GetObject("back.Image");
            back.ImageTransparentColor = Color.Fuchsia;
            back.Name = "back";
            back.RightToLeftAutoMirrorImage = true;
            back.Size = new Size(23, 22);
            back.Click += OnBack;
            // 
            // stop
            // 
            stop.DisplayStyle = ToolStripItemDisplayStyle.Image;
            stop.Image = (Image)resources.GetObject("stop.Image");
            stop.ImageTransparentColor = Color.Fuchsia;
            stop.Name = "stop";
            stop.Size = new Size(23, 22);
            stop.Click += OnStopClick;
            // 
            // refresh
            // 
            refresh.DisplayStyle = ToolStripItemDisplayStyle.Image;
            refresh.Image = (Image)resources.GetObject("refresh.Image");
            refresh.ImageTransparentColor = Color.Fuchsia;
            refresh.Name = "refresh";
            refresh.Size = new Size(23, 22);
            refresh.Click += OnRefresh;
            // 
            // toolStrip1
            // 
            toolStrip1.AccessibleName = "Toolstrip";
            toolStrip1.Dock = DockStyle.Fill;
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { firstPage, previousPage, currentPage, labelOf, totalPages, nextPage, lastPage, toolStripSeparator2, back, stop, refresh, toolStripSeparator3, DirectPrint, PrintDialog, printPreview, pageSetup, export, separator4, zoomIn, zoom, zoomOut, textToFind, find, toolStripSeparator4, findNext });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.RenderMode = ToolStripRenderMode.Professional;
            toolStrip1.Size = new Size(714, 25);
            toolStrip1.TabIndex = 3;
            toolStrip1.TabStop = true;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 25);
            // 
            // DirectPrint
            // 
            DirectPrint.DisplayStyle = ToolStripItemDisplayStyle.Image;
            DirectPrint.Image = (Image)resources.GetObject("DirectPrint.Image");
            DirectPrint.ImageTransparentColor = Color.Magenta;
            DirectPrint.Name = "DirectPrint";
            DirectPrint.Size = new Size(23, 22);
            DirectPrint.ToolTipText = "Direct Print";
            DirectPrint.Click += OnDPrint;
            // 
            // PrintDialog
            // 
            PrintDialog.Image = (Image)resources.GetObject("PrintDialog.Image");
            PrintDialog.ImageTransparentColor = Color.Fuchsia;
            PrintDialog.Name = "PrintDialog";
            PrintDialog.Size = new Size(23, 22);
            PrintDialog.ToolTipText = "Print Dialog";
            PrintDialog.Click += OnPrint;
            // 
            // printPreview
            // 
            printPreview.DisplayStyle = ToolStripItemDisplayStyle.Image;
            printPreview.Image = (Image)resources.GetObject("printPreview.Image");
            printPreview.ImageTransparentColor = Color.Fuchsia;
            printPreview.Name = "printPreview";
            printPreview.Size = new Size(23, 22);
            printPreview.Click += OnPrintPreviewClick;
            // 
            // pageSetup
            // 
            pageSetup.DisplayStyle = ToolStripItemDisplayStyle.Image;
            pageSetup.Image = (Image)resources.GetObject("pageSetup.Image");
            pageSetup.ImageTransparentColor = Color.Magenta;
            pageSetup.Name = "pageSetup";
            pageSetup.Size = new Size(23, 22);
            pageSetup.Click += OnPageSetupClick;
            // 
            // export
            // 
            export.DisplayStyle = ToolStripItemDisplayStyle.Image;
            export.Image = (Image)resources.GetObject("export.Image");
            export.ImageTransparentColor = Color.Fuchsia;
            export.Name = "export";
            export.Size = new Size(29, 22);
            export.DropDownItemClicked += OnExport;
            // 
            // separator4
            // 
            separator4.Margin = new Padding(2, 0, 0, 0);
            separator4.Name = "separator4";
            separator4.Size = new Size(6, 25);
            // 
            // zoomIn
            // 
            zoomIn.DisplayStyle = ToolStripItemDisplayStyle.Image;
            zoomIn.Image = (Image)resources.GetObject("zoomIn.Image");
            zoomIn.ImageTransparentColor = Color.Magenta;
            zoomIn.Name = "zoomIn";
            zoomIn.Size = new Size(23, 22);
            zoomIn.Text = "toolStripButton2";
            zoomIn.Click += zoomIn_Click;
            // 
            // zoom
            // 
            zoom.DropDownStyle = ComboBoxStyle.DropDownList;
            zoom.Margin = new Padding(7, 0, 1, 0);
            zoom.MaxDropDownItems = 9;
            zoom.Name = "zoom";
            zoom.Size = new Size(110, 25);
            zoom.SelectedIndexChanged += OnZoomChanged;
            // 
            // zoomOut
            // 
            zoomOut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            zoomOut.Image = (Image)resources.GetObject("zoomOut.Image");
            zoomOut.ImageTransparentColor = Color.Magenta;
            zoomOut.Name = "zoomOut";
            zoomOut.Size = new Size(23, 22);
            zoomOut.Text = "toolStripButton1";
            zoomOut.Click += zoomOut_Click;
            // 
            // textToFind
            // 
            textToFind.AcceptsReturn = true;
            textToFind.Margin = new Padding(10, 0, 1, 0);
            textToFind.Name = "textToFind";
            textToFind.Size = new Size(75, 25);
            textToFind.KeyPress += textToFind_KeyPress;
            textToFind.TextChanged += textToFind_TextChanged;
            // 
            // find
            // 
            find.Enabled = false;
            find.ForeColor = Color.Blue;
            find.Margin = new Padding(3, 1, 1, 2);
            find.Name = "find";
            find.Size = new Size(23, 22);
            find.Click += find_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.AutoSize = false;
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 20);
            // 
            // findNext
            // 
            findNext.Enabled = false;
            findNext.ForeColor = Color.Blue;
            findNext.Margin = new Padding(2, 1, 0, 2);
            findNext.Name = "findNext";
            findNext.Size = new Size(23, 22);
            findNext.Click += findNext_Click;
            // 
            // ReportToolBar
            // 
            BackColor = SystemColors.Control;
            Controls.Add(toolStrip1);
            Name = "ReportToolBar";
            Size = new Size(714, 25);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void OnZoomChanged(object sender, EventArgs e)
        {
            if (!m_ignoreZoomEvents && this.ZoomChange != null)
            {
                ZoomItem zoomItem = (ZoomItem)zoom.SelectedItem;
                ZoomChangeEventArgs e2 = new ZoomChangeEventArgs(zoomItem.ZoomMode, zoomItem.ZoomPercent);
                this.ZoomChange(this, e2);
            }
        }

        private void zoomIn_Click(object sender, EventArgs e)
        {
            if (!m_ignoreZoomEvents && this.ZoomChange != null)
            {
                var currentZoomPercent = ViewerControl.ZoomPercent;
                var zoomPercent = currentZoomPercent + 10;
                if (zoomPercent > 400)
                {
                    zoomPercent = 400;
                }
                ZoomItem zoomItem = new ZoomItem(ZoomMode.Percent, zoomPercent);
                //check if the zoom item already exists in the list
                if (zoom.Items.Contains(zoomItem))
                {
                    zoom.SelectedItem = zoomItem;
                }
                else
                {
                    zoom.Items.Add(zoomItem);
                    zoom.SelectedItem = zoomItem;
                }
                ZoomChangeEventArgs e2 = new ZoomChangeEventArgs(zoomItem.ZoomMode, zoomItem.ZoomPercent);
                this.ZoomChange(this, e2);
            }
        }

        private void zoomOut_Click(object sender, EventArgs e)
        {
            if (!m_ignoreZoomEvents && this.ZoomChange != null)
            {
                var currentZoomPercent = ViewerControl.ZoomPercent;
                var zoomPercent = currentZoomPercent - 10;
                if (zoomPercent < 10)
                {
                    zoomPercent = 10;
                }
                ZoomItem zoomItem = new ZoomItem(ZoomMode.Percent, zoomPercent);
                //check if the zoom item already exists in the list
                if (zoom.Items.Contains(zoomItem))
                {
                    zoom.SelectedItem = zoomItem;
                }
                else
                {
                    zoom.Items.Add(zoomItem);
                    zoom.SelectedItem = zoomItem;
                }
                ZoomChangeEventArgs e2 = new ZoomChangeEventArgs(zoomItem.ZoomMode, zoomItem.ZoomPercent);
                this.ZoomChange(this, e2);
            }
        }

        private void OnPageNavigation(int newPage)
        {
            if (this.PageNavigation != null)
            {
                PageNavigationEventArgs e = new PageNavigationEventArgs(newPage);
                this.PageNavigation(this, e);
            }
        }

        private void OnExport(object sender, ToolStripItemClickedEventArgs e)
        {
            if (this.Export != null)
            {
                ReportExportEventArgs e2 = new ReportExportEventArgs((RenderingExtension)e.ClickedItem.Tag);
                this.Export(this, e2);
            }
        }

        private void OnSearch(object sender, SearchEventArgs se)
        {
            if (this.Search != null)
            {
                this.Search(this, se);
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            if (this.ReportRefresh != null)
            {
                this.ReportRefresh(this, EventArgs.Empty);
            }
        }

        private void OnPrint(object sender, EventArgs e)
        {
            if (this.Print != null)
            {
                toolStrip1.Capture = false;
                this.Print(this, EventArgs.Empty);
            }
        }

        private void OnDPrint(object sender, EventArgs e)
        {
            if (this.Print != null)
            {
                toolStrip1.Capture = false;
                this.DPrint(this, EventArgs.Empty);
            }
        }

        private void OnBack(object sender, EventArgs e)
        {
            if (this.Back != null)
            {
                this.Back(this, e);
            }
        }

        private void OnPageSetupClick(object sender, EventArgs e)
        {
            if (this.PageSetup != null)
            {
                this.PageSetup(this, EventArgs.Empty);
            }
        }

        public void SetZoom()
        {
            try
            {
                m_ignoreZoomEvents = true;
                ZoomMenuHelper.Populate(zoom, ViewerControl.ZoomMode, ViewerControl.ZoomPercent);
            }
            finally
            {
                m_ignoreZoomEvents = false;
            }
        }

        private void PopulateExportList()
        {
            RenderingExtension[] extensions = ViewerControl.Report.ListRenderingExtensions();
            RenderingExtensionsHelper.Populate(export, null, extensions);
        }

        private void OnPageNavButtonClick(object sender, EventArgs e)
        {
            if (sender == firstPage)
            {
                OnPageNavigation(1);
            }
            else if (sender == previousPage)
            {
                OnPageNavigation(ViewerControl.CurrentPage - 1);
            }
            else if (sender == nextPage)
            {
                OnPageNavigation(ViewerControl.CurrentPage + 1);
            }
            else if (sender == lastPage)
            {
                PageCountMode pageCountMode;
                int newPage = ViewerControl.GetTotalPages(out pageCountMode);
                if (pageCountMode != 0)
                {
                    OnPageNavigation(int.MaxValue);
                }
                else
                {
                    OnPageNavigation(newPage);
                }
            }
        }

        private void find_Click(object sender, EventArgs e)
        {
            OnSearch(sender, new SearchEventArgs(textToFind.Text, ViewerControl.CurrentPage, isFindNext: false));
        }

        private void findNext_Click(object sender, EventArgs e)
        {
            OnSearch(sender, new SearchEventArgs(textToFind.Text, ViewerControl.CurrentPage, isFindNext: true));
        }

        private void textToFind_TextChanged(object sender, EventArgs e)
        {
            find.Enabled = !string.IsNullOrEmpty(textToFind.Text);
            findNext.Enabled = false;
        }

        private void textToFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && textToFind.Text.Length > 0)
            {
                if (!findNext.Enabled)
                {
                    find_Click(sender, null);
                }
                else
                {
                    findNext_Click(sender, null);
                }
            }
        }

        private void OnPrintPreviewClick(object sender, EventArgs e)
        {
            try
            {
                ViewerControl.SetDisplayMode((!printPreview.Checked) ? DisplayMode.PrintLayout : DisplayMode.Normal);
            }
            catch (Exception e2)
            {
                ViewerControl.UpdateUIState(e2);
            }
        }

        private void OnStopClick(object sender, EventArgs e)
        {
            try
            {
                ViewerControl.CancelRendering(0);
            }
            catch (Exception e2)
            {
                ViewerControl.UpdateUIState(e2);
            }
        }

        private void OnReportViewerStateChanged(object sender, EventArgs e)
        {
            ReportViewer reportViewer = (ReportViewer)sender;
            PageCountMode pageCountMode = PageCountMode.Actual;
            int num = 0;
            try
            {
                num = ViewerControl.GetTotalPages(out pageCountMode);
            }
            catch (Exception e2)
            {
                if (!ViewerControl.CurrentStatus.IsInFailedState)
                {
                    ViewerControl.UpdateUIState(e2);
                }
            }
            if (num < 1)
            {
                totalPages.Text = "";
            }
            else
            {
                totalPages.Text = LocalizationHelper.Current.TotalPages(num, pageCountMode);
            }
            ReportViewerStatus currentStatus = reportViewer.CurrentStatus;
            if (currentStatus.CanNavigatePages)
            {
                currentPage.Text = ViewerControl.CurrentPage.ToString(CultureInfo.CurrentCulture);
            }
            bool flag = ViewerControl.CurrentPage <= 1;
            bool flag2 = ViewerControl.CurrentPage >= num && pageCountMode != PageCountMode.Estimate;
            firstPage.Enabled = (currentStatus.CanNavigatePages && !flag);
            previousPage.Enabled = (currentStatus.CanNavigatePages && !flag);
            currentPage.Enabled = currentStatus.CanNavigatePages;
            nextPage.Enabled = (currentStatus.CanNavigatePages && !flag2);
            lastPage.Enabled = (currentStatus.CanNavigatePages && !flag2);
            back.Enabled = currentStatus.CanNavigateBack;
            stop.Enabled = currentStatus.InCancelableOperation;
            refresh.Enabled = currentStatus.CanRefreshData;
            PrintDialog.Enabled = currentStatus.CanPrint;
            printPreview.Enabled = currentStatus.CanChangeDisplayModes;
            printPreview.Checked = (reportViewer.DisplayMode == DisplayMode.PrintLayout);
            pageSetup.Enabled = PrintDialog.Enabled;
            export.Enabled = currentStatus.CanExport;
            zoom.Enabled = currentStatus.CanChangeZoom;
            textToFind.Enabled = currentStatus.CanSearch;
            find.Enabled = (textToFind.Enabled && !string.IsNullOrEmpty(textToFind.Text));
            findNext.Enabled = currentStatus.CanContinueSearch;
            if (currentStatus.CanSearch && ViewerControl.SearchState != null)
            {
                textToFind.Text = ViewerControl.SearchState.Text;
            }
            bool showPageNavigationControls = reportViewer.ShowPageNavigationControls;
            firstPage.Visible = showPageNavigationControls;
            previousPage.Visible = showPageNavigationControls;
            currentPage.Visible = showPageNavigationControls;
            labelOf.Visible = showPageNavigationControls;
            totalPages.Visible = showPageNavigationControls;
            nextPage.Visible = showPageNavigationControls;
            lastPage.Visible = showPageNavigationControls;
            toolStripSeparator2.Visible = showPageNavigationControls;
            back.Visible = reportViewer.ShowBackButton;
            stop.Visible = reportViewer.ShowStopButton;
            refresh.Visible = reportViewer.ShowRefreshButton;
            toolStripSeparator3.Visible = (back.Visible || stop.Visible || refresh.Visible);
            PrintDialog.Visible = reportViewer.ShowPrintButton;
            printPreview.Visible = reportViewer.ShowPrintButton;
            pageSetup.Visible = (PrintDialog.Visible || printPreview.Visible);
            export.Visible = reportViewer.ShowExportButton;
            separator4.Visible = (PrintDialog.Visible || printPreview.Visible || export.Visible);
            zoom.Visible = reportViewer.ShowZoomControl;
            bool showFindControls = reportViewer.ShowFindControls;
            toolStripSeparator4.Visible = showFindControls;
            find.Visible = showFindControls;
            findNext.Visible = showFindControls;
            textToFind.Visible = showFindControls;
            if (export.Visible && export.Enabled)
            {
                PopulateExportList();
            }
        }

        private void CurrentPage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && currentPage.Text.Length > 0)
            {
                bool flag = false;
                if (int.TryParse(currentPage.Text, out int result) && ViewerControl.CanMoveToPage(result))
                {
                    flag = true;
                    OnPageNavigation(result);
                }
                if (!flag)
                {
                    currentPage.Text = ViewerControl.CurrentPage.ToString(CultureInfo.CurrentCulture);
                }
                e.Handled = true;
            }
            else if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

    }
}
