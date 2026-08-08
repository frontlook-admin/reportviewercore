using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.ReportingServices.Common;
using Microsoft.ReportingServices.Interfaces;
using Microsoft.ReportingServices.Rendering.SPBProcessing;
using Microsoft.ReportViewer.Common.FrontLookCode;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using NPOI.OpenXmlFormats.Dml.Chart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;
using Interlocked = System.Threading.Interlocked;
using Volatile = System.Threading.Volatile;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    [Designer("Microsoft.Reporting.WinForms.ReportViewerDesigner, Microsoft.ReportViewer.Design, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91", typeof(IDesigner))]
    [Docking(DockingBehavior.Ask)]
    [SRDescription("ReportViewerDescription")]
    public class ReportViewer : UserControl, IRenderable
    {
        private class AsyncReportOperationWrapper : AsyncReportOperation
        {
            private AsyncReportOperation m_operation;

            public AsyncReportOperationWrapper(AsyncReportOperation operation)
                : base(operation.Report)
            {
                m_operation = operation;
            }

            public void EndWrappedOperationExecution(Exception e)
            {
                m_operation.EndAsyncExecution(e);
            }

            protected override void PerformOperation()
            {
                m_operation.BeginAsyncExecution();
            }
        }

        public const int MaximumPageCount = int.MaxValue;

        private bool m_showToolbar = true;

        private bool m_showParameterPrompts = true;

        private bool m_showCredentialPrompts = true;

        private bool m_promptAreaCollapsed;

        private bool m_docMapCollapsed;

        private bool m_showProgress = true;

        private bool m_showStatusBar = true;

        private int m_toolbarVisibility = -1;

        public UIState m_lastUIState;

        private SearchState m_searchState;

        private bool m_userChangedSplitter;

        private PageCountMode m_pageCountMode = PageCountMode.Estimate;

        private ZoomMode m_zoomMode = ZoomMode.Percent;

        private int m_zoomPercent = 100;

        private ProcessingMode m_processingMode;

        private PrinterSettings m_printerSettings = new PrinterSettings();

        private DisplayMode m_viewMode;

        private Timer m_autoRefreshTimer = new Timer();

        private readonly ReportViewerLiveReloadOptions m_liveReload = new ReportViewerLiveReloadOptions();

        private Timer m_liveReloadPollTimer;

        private Timer m_liveReloadDebounceTimer;

        private readonly List<FileSystemWatcher> m_liveReloadWatchers = new List<FileSystemWatcher>();

        private CancellationTokenSource m_liveReloadCancellation;

        private string m_liveReloadFingerprint;

        private bool m_liveReloadCheckInProgress;

        private bool m_liveReloadCheckQueued;

        private ProcessingThread m_processingThread = new ProcessingThread();

        private RVSplitContainer paramsSplitContainer;

        private AsyncWaitControl m_asyncWaitControl;

        private Timer m_asyncWaitControlTimer;

        private int m_waitControlDisplayAfter;

        private bool m_canRenderForWaitControl;

        private RSParams rsParams;

        private ReportToolBar reportToolBar;

        private StatusStrip reportStatusStrip;

        private ToolStripStatusLabel statusMessage;

        private ToolStripProgressBar statusProgress;

        private ToolStripStatusLabel statusZoom;

        private RVSplitContainer dmSplitContainer;

        private RSDocMap rsDocMap;

        private WinRSviewer winRSviewer;

        private bool m_disposing;

        private ReportHierarchy m_reportHierarchy = new ReportHierarchy();

        private ReportPageSettings m_reportPageSettings;

        private string m_printSettingFilePath;

        private string m_preferencesFilePath;

        private Exception m_lastError;

        private TimeSpan? m_lastRenderDuration;

        private PrinterSettings m_pendingPrintPrinterSettings;

        private PageSettings m_pendingPrintPageSettings;

        private const int RenderingCancellationTimeoutMilliseconds = 5000;

        private long m_renderGeneration;

        private IReportViewerMessages m_reportViewerMessages;

        private Queue<MethodInvoker> m_pendingAsyncInvokes = new Queue<MethodInvoker>();

        private ReportViewerTheme m_theme = ReportViewerTheme.Light;

        private string m_loadingMessage;

        private bool m_loadingMessageCustomized;

        private string m_loadingBrandText = "Powered By Incredible Informatics";

        private System.Drawing.Image m_loadingBrandLogo;

        private bool m_showLoadingBrand = true;

        private bool m_preferencesLoaded;

        private bool m_preferencePersistenceSuppressed;

        private ToolStripRenderer m_toolStripRenderer = new ModernReportToolStripRenderer();

        private ReportViewerStatus m_status;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [SRDescription("ServerReportDesc")]
        public ServerReport ServerReport => CurrentReport.ServerReport;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [SRDescription("LocalReportDesc")]
        public LocalReport LocalReport => CurrentReport.LocalReport;

        internal Report Report
        {
            get
            {
                if (ProcessingMode == ProcessingMode.Remote)
                {
                    return ServerReport;
                }
                return LocalReport;
            }
        }

        [DefaultValue(BorderStyle.FixedSingle)]
        public new BorderStyle BorderStyle
        {
            get
            {
                return base.BorderStyle;
            }
            set
            {
                base.BorderStyle = value;
            }
        }

        [DefaultValue(typeof(Color), "243, 246, 250")]
        public override Color BackColor
        {
            get
            {
                return base.BackColor;
            }
            set
            {
                base.BackColor = value;
                winRSviewer.BackColor = value;
            }
        }

        [Category("Appearance")]
        [Description("Colors used by the report viewer toolbar, canvas, and report page.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ReportViewerTheme Theme
        {
            get
            {
                return m_theme;
            }
            set
            {
                m_theme = value ?? ReportViewerTheme.Light;
                ApplyTheme();
                PersistThemePreference();
            }
        }

        [Category("Appearance")]
        [Description("Text shown while the report is loading.")]
        [DefaultValue(null)]
        public string LoadingMessage
        {
            get
            {
                return m_loadingMessage;
            }
            set
            {
                m_loadingMessage = value;
                m_loadingMessageCustomized = !string.IsNullOrWhiteSpace(value);
                if (m_asyncWaitControl != null)
                {
                    m_asyncWaitControl.SetLoadingMessage(value, m_loadingMessageCustomized);
                }
            }
        }

        [Category("Appearance")]
        [Description("Brand text shown in the report loading card.")]
        [DefaultValue("Powered By Incredible Informatics")]
        public string LoadingBrandText
        {
            get
            {
                return m_loadingBrandText;
            }
            set
            {
                m_loadingBrandText = value ?? string.Empty;
                ApplyLoadingBranding();
            }
        }

        [Category("Appearance")]
        [Description("Logo shown in the report loading card. The caller owns the image and remains responsible for disposing it.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public System.Drawing.Image LoadingBrandLogo
        {
            get
            {
                return m_loadingBrandLogo;
            }
            set
            {
                m_loadingBrandLogo = value;
                ApplyLoadingBranding();
            }
        }

        [Category("Appearance")]
        [Description("Shows or hides the loading card branding.")]
        [DefaultValue(true)]
        public bool ShowLoadingBrand
        {
            get
            {
                return m_showLoadingBrand;
            }
            set
            {
                m_showLoadingBrand = value;
                ApplyLoadingBranding();
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PreferencesFilePath
        {
            get => string.IsNullOrWhiteSpace(m_preferencesFilePath)
                ? GetDefaultPreferencesFilePath()
                : m_preferencesFilePath;
            set => m_preferencesFilePath = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override System.Drawing.Image BackgroundImage
        {
            get
            {
                return base.BackgroundImage;
            }
            set
            {
                base.BackgroundImage = value;
                winRSviewer.BackgroundImage = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override ImageLayout BackgroundImageLayout
        {
            get
            {
                return base.BackgroundImageLayout;
            }
            set
            {
                base.BackgroundImageLayout = value;
                winRSviewer.BackgroundImageLayout = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(100)]
        [SRDescription("DocMapWidthDesc")]
        public int DocumentMapWidth
        {
            get
            {
                return dmSplitContainer.SplitterDistance;
            }
            set
            {
                dmSplitContainer.SplitterDistance = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        [SRDescription("DocMapWidthFixedDesc")]
        public bool IsDocumentMapWidthFixed
        {
            get
            {
                return dmSplitContainer.FixedSize;
            }
            set
            {
                dmSplitContainer.FixedSize = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        [SRDescription("DocMapCollapsedDesc")]
        public bool DocumentMapCollapsed
        {
            get
            {
                return m_docMapCollapsed;
            }
            set
            {
                if (m_docMapCollapsed != value)
                {
                    m_docMapCollapsed = value;
                    UpdateUIState(m_lastUIState);
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        [SRDescription("PromptAreaCollapsedDesc")]
        public bool PromptAreaCollapsed
        {
            get
            {
                return m_promptAreaCollapsed;
            }
            set
            {
                m_promptAreaCollapsed = value;
                UpdateUIState(m_lastUIState);
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [SRDescription("ShowParameterPromptsDesc")]
        public bool ShowParameterPrompts
        {
            get
            {
                return m_showParameterPrompts;
            }
            set
            {
                m_showParameterPrompts = value;
                UpdateUIState(m_lastUIState);
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [SRDescription("ShowCredentialPromptsDesc")]
        public bool ShowCredentialPrompts
        {
            get
            {
                return m_showCredentialPrompts;
            }
            set
            {
                m_showCredentialPrompts = value;
                UpdateUIState(m_lastUIState);
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [SRDescription("ShowToolBarDesc")]
        public bool ShowToolBar
        {
            get
            {
                return m_showToolbar;
            }
            set
            {
                m_showToolbar = value;
                reportToolBar.Visible = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [SRDescription("ShowProgressDesc")]
        public bool ShowProgress
        {
            get
            {
                return m_showProgress;
            }
            set
            {
                m_showProgress = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("Shows the report viewer status bar.")]
        public bool ShowStatusBar
        {
            get
            {
                return m_showStatusBar;
            }
            set
            {
                m_showStatusBar = value;
                if (reportStatusStrip != null)
                {
                    reportStatusStrip.Visible = value;
                }
            }
        }

        [SRDescription("WaitControlDisplayAfterDesc")]
        [DefaultValue(0)]
        public int WaitControlDisplayAfter
        {
            get
            {
                return m_waitControlDisplayAfter;
            }
            set
            {
                m_waitControlDisplayAfter = value;
            }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [SRDescription("ShowContextMenuDesc")]
        public bool ShowContextMenu
        {
            get
            {
                return winRSviewer.ShowContextMenu;
            }
            set
            {
                winRSviewer.ShowContextMenu = value;
            }
        }

        [DefaultValue(true)]
        [SRDescription("ShowDocumentMapButtonDesc")]
        public bool ShowDocumentMapButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.DocMap);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.DocMap, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [DefaultValue(true)]
        [SRDescription("ShowPromptAreaButtonDesc")]
        public bool ShowPromptAreaButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Params);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Params, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowPageNavigationDesc")]
        public bool ShowPageNavigationControls
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.PageNav);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.PageNav, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowBackButtonDesc")]
        public bool ShowBackButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Back);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Back, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowStopButtonDesc")]
        public bool ShowStopButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Stop);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Stop, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowRefreshButtonDesc")]
        public bool ShowRefreshButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Refresh);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Refresh, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowPrintButtonDesc")]
        public bool ShowPrintButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Print);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Print, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowExportButtonDesc")]
        public bool ShowExportButton
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Export);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Export, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowZoomButtonDesc")]
        public bool ShowZoomControl
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Zoom);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Zoom, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [SRCategory("ToolBarCategoryDesc")]
        [DefaultValue(true)]
        [SRDescription("ShowFindButtonDesc")]
        public bool ShowFindControls
        {
            get
            {
                return IsVisibilityFlagSet(ToolbarFlags.Find);
            }
            set
            {
                SetVisibilityFlag(ToolbarFlags.Find, value);
                UpdateUIState(m_lastUIState);
            }
        }

        [DefaultValue(ProcessingMode.Local)]
        [SRDescription("ProcessingModeDesc")]
        public ProcessingMode ProcessingMode
        {
            get
            {
                return m_processingMode;
            }
            set
            {
                if (m_processingMode != value)
                {
                    if (Report.IsDrillthroughReport)
                    {
                        throw new InvalidOperationException();
                    }
                    Clear();
                    m_processingMode = value;
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReportViewerMessages Messages
        {
            get
            {
                return m_reportViewerMessages;
            }
            set
            {
                m_reportViewerMessages = value;
                (LocalizationHelper.Current as LocalizationHelper).SetWinformsViewerMessages(m_reportViewerMessages);
                reportToolBar.SuspendLayout();
                reportToolBar.ApplyCustomResources();
                reportToolBar.SetZoom();
                reportToolBar.ResumeLayout(performLayout: true);
                rsParams.SuspendLayout();
                rsParams.ApplyCustomResources();
                rsParams.ResumeLayout(performLayout: true);
                m_asyncWaitControl.SuspendLayout();
                m_asyncWaitControl.ApplyCustomResources();
                m_asyncWaitControl.ResumeLayout(performLayout: true);
                winRSviewer.SuspendLayout();
                winRSviewer.ApplyCustomResources();
                winRSviewer.SetZoom();
                winRSviewer.ResumeLayout(performLayout: true);
                ApplySplitterResources(allResources: false);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CurrentPage
        {
            get
            {
                return CurrentReport.CurrentPage;
            }
            set
            {
                try
                {
                    SetCurrentPage(value, ActionType.None, null);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
            }
        }

        [Browsable(false)]
        public DisplayMode DisplayMode => m_viewMode;

        [DefaultValue(PageCountMode.Estimate)]
        [SRDescription("PageCountModeDesc")]
        public PageCountMode PageCountMode
        {
            get
            {
                return m_pageCountMode;
            }
            set
            {
                if (value != m_pageCountMode)
                {
                    m_pageCountMode = value;
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(ZoomMode.Percent)]
        [SRDescription("ZoomModeDesc")]
        public ZoomMode ZoomMode
        {
            get
            {
                return m_zoomMode;
            }
            set
            {
                if (value != ZoomMode)
                {
                    m_zoomMode = value;
                    SetZoom();
                }
            }
        }

        [Category("Appearance")]
        [DefaultValue(100)]
        [SRDescription("ZoomPercentDesc")]
        public int ZoomPercent
        {
            get
            {
                return m_zoomPercent;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (value != ZoomPercent)
                {
                    m_zoomPercent = value;
                    if (ZoomMode == ZoomMode.Percent)
                    {
                        SetZoom();
                    }
                }
            }
        }

        [Browsable(false)]
        public int ZoomCalculated => Math.Max((int)Math.Round(winRSviewer.ZoomCalculated * 100f), 1);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ToolStripRenderer ToolStripRenderer
        {
            get
            {
                return m_toolStripRenderer;
            }
            set
            {
                m_toolStripRenderer = value;
                reportToolBar.SetToolStripRenderer(value);
                winRSviewer.SetToolStripRenderer(value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ToolStrip Toolbar => reportToolBar.ToolStrip;

        /// <summary>
        /// Adds an application-owned button immediately before the built-in theme menu.
        /// The caller owns the supplied image and is responsible for disposing it.
        /// </summary>
        public ToolStripButton AddToolbarButton(string name, string text, EventHandler click, System.Drawing.Image image = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A toolbar button name is required.", nameof(name));
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Toolbar button text is required.", nameof(text));
            }
            if (click == null)
            {
                throw new ArgumentNullException(nameof(click));
            }
            if (Toolbar.Items.ContainsKey(name))
            {
                throw new ArgumentException($"A toolbar item named '{name}' already exists.", nameof(name));
            }

            var button = new ToolStripButton(text)
            {
                Name = name,
                Image = image,
                DisplayStyle = image == null
                    ? ToolStripItemDisplayStyle.Text
                    : ToolStripItemDisplayStyle.ImageAndText,
                ToolTipText = text,
                AccessibleName = text
            };
            button.Click += click;
            reportToolBar.AddCustomItem(button);
            return button;
        }

        private void ApplyTheme()
        {
            if (reportToolBar == null || winRSviewer == null)
            {
                return;
            }

            if (m_toolStripRenderer is ModernReportToolStripRenderer)
            {
                m_toolStripRenderer = new ModernReportToolStripRenderer(m_theme);
            }

            base.BackColor = m_theme.CanvasBackground;
            paramsSplitContainer.BackColor = m_theme.ToolbarBorder;
            dmSplitContainer.BackColor = m_theme.ToolbarBorder;
            rsParams.BackColor = m_theme.CanvasBackground;
            rsDocMap.BackColor = m_theme.InputBackground;
            reportStatusStrip.BackColor = m_theme.ToolbarBackground;
            reportStatusStrip.ForeColor = m_theme.Foreground;
            statusMessage.ForeColor = m_theme.Foreground;
            statusProgress.ForeColor = m_theme.Accent;
            statusZoom.ForeColor = m_theme.Foreground;
            reportToolBar.ApplyTheme(m_theme, m_toolStripRenderer);
            m_asyncWaitControl.ApplyTheme(m_theme);
            ApplyLoadingBranding();
            winRSviewer.ApplyTheme(m_theme, m_toolStripRenderer);
        }

        private void ApplyLoadingBranding()
        {
            if (m_asyncWaitControl == null)
            {
                return;
            }

            if (m_loadingMessageCustomized)
            {
                m_asyncWaitControl.SetLoadingMessage(m_loadingMessage, custom: true);
            }

            m_asyncWaitControl.SetBranding(m_loadingBrandText, m_showLoadingBrand, m_loadingBrandLogo);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ReportViewerStatus CurrentStatus => m_status;

        [Category("Behavior")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ReportViewerLiveReloadOptions LiveReload => m_liveReload;

        [SRDescription("KeepSessionAliveDesc")]
        [DefaultValue(true)]
        public bool KeepSessionAlive
        {
            get
            {
                return m_reportHierarchy.KeepSessionAlive;
            }
            set
            {
                m_reportHierarchy.KeepSessionAlive = value;
            }
        }

        internal ProcessingThread BackgroundThread => m_processingThread;

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PageSettings CurrentReportPageSetting
        {
            get => CurrentReport.PageSettings;
            set
            {
                m_reportPageSettings = null;
                CurrentReport.PageSettings = value;
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool MetricEnabled { get; set; } = true;

        internal ReportInfo CurrentReport
        {
            get
            {
                if (m_reportHierarchy.Count == 0)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return m_reportHierarchy.Peek();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SearchState SearchState => m_searchState;

        [Browsable(false)]
        public int SearchMatchCount
        {
            get
            {
                if (m_searchState == null || m_reportHierarchy.Count == 0 || CurrentReport.GdiRenderer?.Context == null)
                {
                    return 0;
                }

                return CurrentReport.GdiRenderer.Context.SearchMatches?.Count ?? 0;
            }
        }

        [Browsable(false)]
        public int SearchMatchIndex
        {
            get
            {
                if (SearchMatchCount == 0 || CurrentReport.GdiRenderer?.Context == null)
                {
                    return 0;
                }

                return CurrentReport.GdiRenderer.Context.SearchMatchIndex + 1;
            }
        }

        [Browsable(false)]
        public string SearchText => m_searchState?.Text;

        [Browsable(false)]
        public Exception LastError => m_lastError;

        [Browsable(false)]
        public TimeSpan? LastRenderDuration => m_lastRenderDuration;

        public void ClearSearch()
        {
            if (m_reportHierarchy.Count == 0)
            {
                return;
            }

            CurrentReport.GdiRenderer?.ClearSearchResults();
            m_searchState = null;
            reportToolBar.ClearSearchText();
            UpdateUIState(m_lastUIState);
        }

        private PageSettings PageSettings
        {
            get
            {
                PageSettings pageSettings = CurrentReport.PageSettings;
                if (pageSettings == null)
                {
                    return ResetAndGetPageSettings();
                }
                return pageSettings;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public PrinterSettings PrinterSettings
        {
            get
            {
                if (!m_printerSettings.IsValid)
                {
                    m_printerSettings = new PrinterSettings();
                }
                return m_printerSettings;
            }
            set
            {
                m_printerSettings = value;
                ReportPageSettings.UpdatePageSettingsForPrinter(PageSettings, m_printerSettings);
            }
        }

        private CreateAndRegisterStream PrintCreateAndRegisterStream => CreateStreamEMF;

        private bool ParametersAreaSupported => ProcessingMode == ProcessingMode.Remote;

        bool IRenderable.CanRender => m_canRenderForWaitControl;

        [SRDescription("ZoomEventDesc")]
        public event ZoomChangedEventHandler ZoomChange;

        [SRDescription("PageNavigationEventDesc")]
        public event PageNavigationEventHandler PageNavigation;

        [SRDescription("ExportEventDesc")]
        public event ExportEventHandler ReportExport;

        [SRDescription("RefreshEventDesc")]
        public event CancelEventHandler ReportRefresh;

        [SRDescription("PrintEventDesc")]
        public event ReportPrintEventHandler Print;

        [SRDescription("PrintingBegingEventDesc")]
        public event ReportPrintEventHandler PrintingBegin;

        public event EventHandler<ReportPrintCompletedEventArgs> PrintCompleted;

        [SRDescription("BackEventDesc")]
        public event BackEventHandler Back;

        [SRDescription("BookmarkEventDesc")]
        public event BookmarkNavigationEventHandler BookmarkNavigation;

        [SRDescription("ToggleEventDesc")]
        public event CancelEventHandler Toggle;

        [SRDescription("DrillthroughEventDesc")]
        public event DrillthroughEventHandler Drillthrough;

        [SRDescription("ViewReportEventDesc")]
        public event CancelEventHandler ViewButtonClick;

        [SRDescription("SortEventDesc")]
        public event SortEventHandler Sort;

        [SRDescription("HyperlinkEventDesc")]
        public event HyperlinkEventHandler Hyperlink;

        [SRDescription("DocMapEventDesc")]
        public event DocumentMapNavigationEventHandler DocumentMapNavigation;

        [SRDescription("RenderCompleteEventDesc")]
        public event RenderingCompleteEventHandler RenderingComplete;

        [SRDescription("RenderBeginEventDesc")]
        public event CancelEventHandler RenderingBegin;

        public event EventHandler<ReportRenderProgress> RenderingProgress;

        public event EventHandler LiveReloaded;

        public event EventHandler<ReportViewerLiveReloadErrorEventArgs> LiveReloadError;

        [SRDescription("SearchEventDesc")]
        public event SearchEventHandler Search;

        [SRDescription("ErrorEventDesc")]
        public event ReportErrorEventHandler ReportError;

        [SRDescription("StateChangedEventDesc")]
        public event EventHandler<EventArgs> StatusChanged;

        [SRDescription("SubmittingDataSourceCredentialsEventDesc")]
        public event ReportCredentialsEventHandler SubmittingDataSourceCredentials;

        [SRDescription("SubmittingParameterValuesEventDesc")]
        public event ReportParametersEventHandler SubmittingParameterValues;

        [SRDescription("PageSettingsChangedEventDesc")]
        public event EventHandler PageSettingsChanged;

        public ReportViewer()
        {
            if (SystemInformation.HighContrast)
            {
                m_theme = ReportViewerTheme.HighContrast;
            }
            InitializeComponent();
            reportToolBar.SetToolStripRenderer(m_toolStripRenderer);
            winRSviewer.SetToolStripRenderer(m_toolStripRenderer);
            reportToolBar.ThemeChange += OnThemeChange;
            ApplyTheme();
            reportToolBar.ViewerControl = this;
            rsParams.ViewerControl = this;
            winRSviewer.ViewerControl = this;
            m_autoRefreshTimer.Tick += OnRefresh;
            m_liveReload.Changed += OnLiveReloadOptionsChanged;
            RenderingProgress += OnRenderingProgress;
            Reset();
            SetZoom();
        }

        private void OnThemeChange(object sender, EventArgs e)
        {
            Theme = reportToolBar.SelectedTheme;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            Keys modifiers = keyData & Keys.Modifiers;

            if ((modifiers & Keys.Control) == Keys.Control && key == Keys.P)
            {
                if ((modifiers & Keys.Shift) == Keys.Shift)
                {
                    DPrint();
                }
                else
                {
                    PrintDialog();
                }

                return true;
            }

            if (modifiers == Keys.Control)
            {
                if (key == Keys.Add || key == Keys.Oemplus)
                {
                    ChangeZoomByKeyboard(10);
                    return true;
                }

                if (key == Keys.Subtract || key == Keys.OemMinus)
                {
                    ChangeZoomByKeyboard(-10);
                    return true;
                }

                if (key == Keys.D0 || key == Keys.NumPad0)
                {
                    ZoomMode = ZoomMode.Percent;
                    ZoomPercent = 100;
                    return true;
                }

                if (key == Keys.F)
                {
                    reportToolBar.FocusSearch();
                    return true;
                }
            }

            if (modifiers == Keys.None)
            {
                if (key == Keys.PageUp)
                {
                    return NavigateWithKeyboard(CurrentPage - 1);
                }

                if (key == Keys.PageDown)
                {
                    return NavigateWithKeyboard(CurrentPage + 1);
                }

                if (key == Keys.Home)
                {
                    return NavigateWithKeyboard(1);
                }

                if (key == Keys.End)
                {
                    var totalPages = GetTotalPages(out var pageCountMode);
                    return NavigateWithKeyboard(pageCountMode == PageCountMode.Estimate ? int.MaxValue : totalPages);
                }

                if (key == Keys.F5)
                {
                    RefreshReport();
                    return true;
                }

                if (key == Keys.Escape && SearchState != null)
                {
                    ClearSearch();
                    return true;
                }

                if (key == Keys.Escape && CurrentStatus != null && CurrentStatus.InCancelableOperation)
                {
                    CancelRendering(0);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ChangeZoomByKeyboard(int amount)
        {
            int zoomPercent = Math.Max(10, Math.Min(400, ZoomPercent + amount));
            ZoomMode = ZoomMode.Percent;
            ZoomPercent = zoomPercent;
        }

        private bool NavigateWithKeyboard(int targetPage)
        {
            if (CurrentStatus == null || !CurrentStatus.CanNavigatePages || !CanMoveToPage(targetPage))
            {
                return false;
            }

            OnPageNavigation(this, new PageNavigationEventArgs(targetPage));
            return true;
        }

        private void OnZoomChanged(object sender, ZoomChangeEventArgs e)
        {
            try
            {
                if (this.ZoomChange != null)
                {
                    this.ZoomChange(this, e);
                }
                if (!e.Cancel)
                {
                    ZoomPercent = e.ZoomPercent;
                    ZoomMode = e.ZoomMode;
                }
                else if (sender == reportToolBar)
                {
                    reportToolBar.SetZoom();
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnPageNavigation(object sender, PageNavigationEventArgs e)
        {
            OnPageNavigation(sender, e, ActionType.None);
        }

        private void OnPageNavigation(object sender, PageNavigationEventArgs e, ActionType postRenderAction)
        {
            try
            {
                if (this.PageNavigation != null)
                {
                    this.PageNavigation(this, e);
                }
                if (!e.Cancel)
                {
                    SetCurrentPage(e.NewPage, postRenderAction, null);
                }
                else if (sender == reportToolBar)
                {
                    UpdateUIState(m_lastUIState);
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private bool OnPageNavigation(int targetPage)
        {
            if (this.PageNavigation == null)
            {
                return true;
            }
            PageNavigationEventArgs pageNavigationEventArgs = new PageNavigationEventArgs(targetPage);
            this.PageNavigation(this, pageNavigationEventArgs);
            return !pageNavigationEventArgs.Cancel;
        }

        private void OnExport(object sender, ReportExportEventArgs e)
        {
            try
            {
                if (this.ReportExport != null)
                {
                    this.ReportExport(this, e);
                }
                if (!e.Cancel)
                {
                    ExportDialog(e.Extension, e.DeviceInfo);
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            try
            {
                CancelEventArgs cancelEventArgs = new CancelEventArgs();
                if (this.ReportRefresh != null)
                {
                    this.ReportRefresh(this, cancelEventArgs);
                }
                if (!cancelEventArgs.Cancel)
                {
                    int targetPage = 1;
                    PostRenderArgs postRenderArgs = null;
                    if (sender == m_autoRefreshTimer)
                    {
                        targetPage = CurrentPage;
                        postRenderArgs = new PostRenderArgs(isDifferentReport: true, isPartialRendering: false, winRSviewer.ReportPanelAutoScrollPosition);
                    }
                    RefreshReport(targetPage, postRenderArgs);
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnPrint(object sender, EventArgs e)
        {
            try
            {
                ReportPrintEventArgs reportPrintEventArgs = new ReportPrintEventArgs(CreateDefaultPrintSettings());
                if (this.Print != null)
                {
                    this.Print(this, reportPrintEventArgs);
                }
                if (!reportPrintEventArgs.Cancel)
                {
                    PrintDialog(reportPrintEventArgs.PrinterSettings);
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnPrinterPageSettings(object sender, EventArgs e)
        {
            try
            {
                ReportPrintEventArgs reportPrintEventArgs = new ReportPrintEventArgs(CreateDefaultPrintSettings());
                if (this.Print != null)
                {
                    this.Print(this, reportPrintEventArgs);
                }
                if (!reportPrintEventArgs.Cancel)
                {
                    SetPrinterAndPageSettings();
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnDPrint(object sender, EventArgs e)
        {
            try
            {
                ReportPrintEventArgs reportPrintEventArgs = new ReportPrintEventArgs(CreateDefaultPrintSettings());
                if (this.Print != null)
                {
                    this.Print(this, reportPrintEventArgs);
                }
                if (!reportPrintEventArgs.Cancel)
                {
                    DPrint();
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private bool OnPrintingBegin(object sender, PrinterSettings printerSettings)
        {
            try
            {
                ReportPrintEventArgs reportPrintEventArgs = new ReportPrintEventArgs(printerSettings);
                if (this.PrintingBegin != null)
                {
                    this.PrintingBegin(this, reportPrintEventArgs);
                    return !reportPrintEventArgs.Cancel;
                }
                return true;
            }
            catch (Exception e)
            {
                UpdateUIState(e);
                return false;
            }
        }

        private void OnBack(object sender, EventArgs e)
        {
            try
            {
                if (m_reportHierarchy.Count < 2)
                {
                    throw new InvalidOperationException("Back call without drillthrough report");
                }
                bool flag = false;
                if (this.Back != null)
                {
                    ReportInfo reportInfo = m_reportHierarchy.ToArray()[1];
                    Report parentReport = (Report)((ProcessingMode != 0) ? ((object)reportInfo.ServerReport) : ((object)reportInfo.LocalReport));
                    BackEventArgs backEventArgs = new BackEventArgs(parentReport);
                    this.Back(this, backEventArgs);
                    flag = backEventArgs.Cancel;
                }
                if (!flag)
                {
                    PerformBack();
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnDocumentMapNavigation(object sender, DocumentMapNavigationEventArgs e)
        {
            try
            {
                if (this.DocumentMapNavigation != null)
                {
                    this.DocumentMapNavigation(this, e);
                }
                if (!e.Cancel)
                {
                    int num = Report.PerformDocumentMapNavigation(e.DocumentMapId);
                    if (num != 0 && (num == CurrentPage || OnPageNavigation(num)))
                    {
                        SetCurrentPage(num, ActionType.DocumentMap, e.DocumentMapId);
                    }
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        private void OnSearch(object sender, SearchEventArgs se)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(se.SearchString))
                {
                    ClearSearch();
                    return;
                }

                if (this.Search != null)
                {
                    this.Search(this, se);
                }
                if (se.Cancel)
                {
                    return;
                }
                if (!se.IsFindNext)
                {
                    if (Find(se.SearchString, se.StartPage) == 0)
                    {
                        MessageBoxWrappers.ShowMessageBox(this, LocalizationHelper.Current.TextNotFound, LocalizationHelper.Current.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
                else if (FindNext() == 0)
                {
                    MessageBoxWrappers.ShowMessageBox(this, LocalizationHelper.Current.NoMoreMatches, LocalizationHelper.Current.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        private bool OnError(Exception e)
        {
            m_lastError = e;
            if (this.ReportError != null)
            {
                ReportErrorEventArgs reportErrorEventArgs = new ReportErrorEventArgs(e);
                this.ReportError(this, reportErrorEventArgs);
                return reportErrorEventArgs.Handled;
            }
            return false;
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            TriggerWaitControl();
            if (this.StatusChanged != null)
            {
                this.StatusChanged(sender, e);
            }
        }

        private void OnRenderingProgress(object sender, ReportRenderProgress progress)
        {
            if (progress == null)
            {
                return;
            }

            if (progress.Stage == ReportRenderStage.Started)
            {
                m_lastError = null;
                m_lastRenderDuration = null;
            }
            else if (progress.Stage == ReportRenderStage.Completed
                || progress.Stage == ReportRenderStage.Cancelled
                || progress.Stage == ReportRenderStage.Failed)
            {
                m_lastRenderDuration = progress.Elapsed;
                if (progress.Error != null)
                {
                    m_lastError = progress.Error;
                }
            }

            if (reportStatusStrip == null || IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new MethodInvoker(() => OnRenderingProgress(sender, progress)));
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            statusMessage.Text = progress.Message ?? string.Empty;
            statusProgress.Visible = progress.Stage == ReportRenderStage.Started || progress.Stage == ReportRenderStage.Rendering;
            if (progress.Stage == ReportRenderStage.Completed)
            {
                statusMessage.Text = "Ready";
            }
            else if (progress.Stage == ReportRenderStage.Cancelled)
            {
                statusMessage.Text = "Cancelled";
            }
            else if (progress.Stage == ReportRenderStage.Failed && progress.Error != null)
            {
                statusMessage.Text = progress.Error.Message;
            }
        }

        private void UpdateStatusBar(UIState state)
        {
            if (reportStatusStrip == null)
            {
                return;
            }

            statusZoom.Text = ZoomMode == ZoomMode.Percent
                ? $"{ZoomPercent}%"
                : ZoomMode == ZoomMode.PageWidth ? "Page width" : "Full page";
            statusMessage.Text = SearchMatchCount > 0
                ? $"Match {SearchMatchIndex} of {SearchMatchCount}"
                : state switch
            {
                UIState.NoReport => "No report loaded",
                UIState.LongRunningAction => "Working...",
                UIState.ProcessingFailure => "Report rendering failed",
                UIState.ProcessingPartial => $"Page {CurrentPage} (partial)",
                _ => $"Page {CurrentPage}"
            };
        }

        private void TriggerWaitControl()
        {
            if (CurrentStatus.CanInteractWithReportPage == ReportViewerStatus.DoesStateAllowInteractWithReportPage(m_lastUIState))
            {
                return;
            }
            if (!CurrentStatus.CanInteractWithReportPage)
            {
                if (m_showProgress)
                {
                    if (m_waitControlDisplayAfter > 0)
                    {
                        m_asyncWaitControlTimer.Interval = m_waitControlDisplayAfter;
                        m_asyncWaitControlTimer.Start();
                    }
                    else
                    {
                        SetWaitControlVisibility(visible: true);
                    }
                }
            }
            else
            {
                m_asyncWaitControlTimer.Stop();
                SetWaitControlVisibility(visible: false);
            }
        }

        private void OnWaitPanelTimerTick(object sender, EventArgs e)
        {
            m_asyncWaitControlTimer.Stop();
            SetWaitControlVisibility(m_showProgress);
        }

        private void SetWaitControlVisibility(bool visible)
        {
            dmSplitContainer.SuspendLayout();
            rsDocMap.Enabled = !visible;
            rsDocMap.BackColor = visible ? m_theme.CanvasBackground : m_theme.InputBackground;
            if (visible)
            {
                m_asyncWaitControl.BringToFront();
            }
            m_asyncWaitControl.Visible = visible;
            dmSplitContainer.ResumeLayout(performLayout: true);
        }

        private void OnSubmittingDataSourceCredentials(object sender, ReportCredentialsEventArgs credentialArgs)
        {
            if (this.SubmittingDataSourceCredentials != null)
            {
                this.SubmittingDataSourceCredentials(this, credentialArgs);
            }
        }

        private void OnSubmittingParameterValues(object sender, ReportParametersEventArgs parameterArgs)
        {
            if (this.SubmittingParameterValues != null)
            {
                this.SubmittingParameterValues(this, parameterArgs);
            }
        }

        private bool IsVisibilityFlagSet(ToolbarFlags flag)
        {
            return (m_toolbarVisibility & (int)flag) != 0;
        }

        private void SetVisibilityFlag(ToolbarFlags flag, bool shouldSet)
        {
            if (shouldSet)
            {
                m_toolbarVisibility |= (int)flag;
            }
            else
            {
                m_toolbarVisibility &= (int)(~flag);
            }
        }

        private void InitializeComponent()
        {
            paramsSplitContainer = new Microsoft.Reporting.WinForms.RVSplitContainer();
            rsParams = new Microsoft.Reporting.WinForms.RSParams();
            dmSplitContainer = new Microsoft.Reporting.WinForms.RVSplitContainer();
            rsDocMap = new Microsoft.Reporting.WinForms.RSDocMap();
            winRSviewer = new Microsoft.Reporting.WinForms.WinRSviewer();
            reportToolBar = new Microsoft.Reporting.WinForms.ReportToolBar();
            reportStatusStrip = new StatusStrip();
            statusMessage = new ToolStripStatusLabel();
            statusProgress = new ToolStripProgressBar();
            statusZoom = new ToolStripStatusLabel();
            paramsSplitContainer.Panel1.SuspendLayout();
            paramsSplitContainer.Panel2.SuspendLayout();
            paramsSplitContainer.SuspendLayout();
            dmSplitContainer.Panel1.SuspendLayout();
            dmSplitContainer.Panel2.SuspendLayout();
            dmSplitContainer.SuspendLayout();
            SuspendLayout();
            AutoScaleMode = AutoScaleMode.Dpi;
            paramsSplitContainer.Dock = DockStyle.Fill;
            paramsSplitContainer.Orientation = Orientation.Horizontal;
            paramsSplitContainer.Size = new Size(396, 246);
            paramsSplitContainer.SplitterDistance = 100;
            paramsSplitContainer.TabIndex = 6;
            paramsSplitContainer.BackColor = System.Drawing.SystemColors.ScrollBar;
            paramsSplitContainer.Name = "paramsSplitContainer";
            paramsSplitContainer.Panel1.Controls.Add(rsParams);
            paramsSplitContainer.Panel1Visible = false;
            paramsSplitContainer.Panel2.Controls.Add(dmSplitContainer);
            paramsSplitContainer.Panel2.Controls.Add(reportToolBar);
            paramsSplitContainer.Panel2.Controls.Add(reportStatusStrip);
            paramsSplitContainer.SplitterMoving += new System.EventHandler(OnParamsSplitterMoving);
            paramsSplitContainer.CollapsedChanged += new System.EventHandler(OnPromptAreaCollapse);
            rsParams.AutoScroll = true;
            rsParams.Dock = DockStyle.Fill;
            rsParams.Size = new Size(398, 100);
            rsParams.Visible = false;
            rsParams.BackColor = System.Drawing.SystemColors.Control;
            rsParams.Name = "rsParams";
            rsParams.ViewButtonClick += new System.EventHandler(OnViewButtonClick);
            rsParams.SubmitDataSourceCredentials += new Microsoft.Reporting.WinForms.ReportCredentialsEventHandler(OnSubmittingDataSourceCredentials);
            rsParams.SubmitParameters += new Microsoft.Reporting.WinForms.ReportParametersEventHandler(OnSubmittingParameterValues);
            rsParams.PreferredHeightChanged += new System.EventHandler(OnPreferredPromptAreaHeightChanged);
            dmSplitContainer.Dock = DockStyle.Fill;
            dmSplitContainer.Location = new Point(0, 25);
            dmSplitContainer.Size = new Size(396, 221);
            dmSplitContainer.SplitterDistance = 100;
            dmSplitContainer.TabIndex = 8;
            dmSplitContainer.BackColor = System.Drawing.Color.LightGray;
            dmSplitContainer.Name = "dmSplitContainer";
            dmSplitContainer.Panel1.Controls.Add(rsDocMap);
            dmSplitContainer.Panel1Visible = false;
            dmSplitContainer.CollapsedChanged += new System.EventHandler(OnDocumentMapCollapse);
            dmSplitContainer.Panel2.Controls.Add(winRSviewer);
            rsDocMap.Dock = DockStyle.Fill;
            rsDocMap.Size = new Size(100, 223);
            rsDocMap.BackColor = System.Drawing.Color.White;
            rsDocMap.BorderStyle = System.Windows.Forms.BorderStyle.None;
            rsDocMap.HotTracking = true;
            rsDocMap.Name = "rsDocMap";
            rsDocMap.RightToLeftLayout = true;
            rsDocMap.DocumentMapNavigation += new Microsoft.Reporting.WinForms.DocumentMapNavigationEventHandler(OnDocumentMapNavigation);
            winRSviewer.AutoScroll = true;
            winRSviewer.Dock = DockStyle.Fill;
            winRSviewer.Size = new Size(398, 221);
            winRSviewer.BackColor = System.Drawing.Color.FromArgb(243, 246, 250);
            winRSviewer.CausesValidation = false;
            winRSviewer.Name = "winRSviewer";
            winRSviewer.ShowContextMenu = true;
            winRSviewer.PageNavigation += new Microsoft.Reporting.WinForms.InternalPageNavigationEventHandler(OnPageNavigation);
            winRSviewer.ZoomChange += new Microsoft.Reporting.WinForms.ZoomChangedEventHandler(OnZoomChanged);
            winRSviewer.ReportRefresh += new System.EventHandler(OnRefresh);
            winRSviewer.PageSettings += new System.EventHandler(OnPageSetup);
            winRSviewer.Print += new System.EventHandler(OnPrint);
            winRSviewer.PrinterPageSettings += new System.EventHandler(OnPrinterPageSettings);
            winRSviewer.DPrint += new System.EventHandler(OnDPrint);
            winRSviewer.Export += new Microsoft.Reporting.WinForms.ExportEventHandler(OnExport);
            winRSviewer.Back += new System.EventHandler(OnBack);
            reportToolBar.Dock = DockStyle.Top;
            reportToolBar.Size = new Size(396, 40);
            reportToolBar.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            reportToolBar.Name = "reportToolBar";
            reportToolBar.ZoomChange += new Microsoft.Reporting.WinForms.ZoomChangedEventHandler(OnZoomChanged);
            reportToolBar.ReportRefresh += new System.EventHandler(OnRefresh);
            reportToolBar.PageSetup += new System.EventHandler(OnPageSetup);
            reportToolBar.Print += new System.EventHandler(OnPrint);
            reportToolBar.DPrint += new System.EventHandler(OnDPrint);
            reportToolBar.PrinterPageSettings += new System.EventHandler(OnPrinterPageSettings);
            reportToolBar.Export += new Microsoft.Reporting.WinForms.ExportEventHandler(OnExport);
            reportToolBar.Search += new Microsoft.Reporting.WinForms.SearchEventHandler(OnSearch);
            reportToolBar.Back += new System.EventHandler(OnBack);
            reportToolBar.PageNavigation += new Microsoft.Reporting.WinForms.PageNavigationEventHandler(OnPageNavigation);
            reportStatusStrip.Dock = DockStyle.Bottom;
            reportStatusStrip.Name = "reportStatusStrip";
            reportStatusStrip.SizingGrip = false;
            reportStatusStrip.TabStop = false;
            reportStatusStrip.Items.AddRange(new ToolStripItem[] { statusMessage, statusProgress, statusZoom });
            statusMessage.Name = "statusMessage";
            statusMessage.Spring = true;
            statusMessage.TextAlign = ContentAlignment.MiddleLeft;
            statusProgress.Name = "statusProgress";
            statusProgress.MarqueeAnimationSpeed = 30;
            statusProgress.Size = new Size(120, 16);
            statusProgress.Visible = false;
            statusZoom.Name = "statusZoom";
            statusZoom.AutoSize = false;
            statusZoom.Size = new Size(64, 16);
            statusZoom.TextAlign = ContentAlignment.MiddleRight;
            BackColor = System.Drawing.Color.FromArgb(243, 246, 250);
            BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            base.Controls.Add(paramsSplitContainer);
            base.Name = "ReportViewer";
            Size = new Size(396, 246);
            m_asyncWaitControl = new Microsoft.Reporting.WinForms.AsyncWaitControl(this);
            m_asyncWaitControl.Dock = System.Windows.Forms.DockStyle.Fill;
            m_asyncWaitControl.Visible = false;
            m_asyncWaitControlTimer = new System.Windows.Forms.Timer();
            m_asyncWaitControlTimer.Tick += new System.EventHandler(OnWaitPanelTimerTick);
            dmSplitContainer.Panel2.Controls.Add(m_asyncWaitControl);
            paramsSplitContainer.Panel1.ResumeLayout(false);
            paramsSplitContainer.Panel2.ResumeLayout(false);
            paramsSplitContainer.ResumeLayout(false);
            dmSplitContainer.Panel1.ResumeLayout(false);
            dmSplitContainer.Panel2.ResumeLayout(false);
            dmSplitContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        public void OnPrinterPageSettingsClick()
        {
            reportToolBar.OnPrinterPageSettingsClick();

        }

        public int GetTotalPages()
        {
            PageCountMode pageCountMode;
            return GetTotalPages(out pageCountMode);
        }

        public int GetTotalPages(out PageCountMode pageCountMode)
        {
            if (m_viewMode == DisplayMode.PrintLayout)
            {
                pageCountMode = PageCountMode.Actual;
                if (CurrentReport.FileManager.Status == FileManagerStatus.InProgress)
                {
                    return Math.Max(CurrentReport.FileManager.Count - 1, 0);
                }
                return CurrentReport.FileManager.Count;
            }
            return Report.GetTotalPages(out pageCountMode);
        }

        private void SetCurrentPage(int page, ActionType postActionType, string actionID)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException("page");
            }
            CurrentReport.CurrentPage = page;
            PostRenderArgs postRenderArgs = new PostRenderArgs(postActionType, actionID);
            if (m_viewMode == DisplayMode.PrintLayout)
            {
                SetViewForCurrentPage(m_lastUIState, postRenderArgs);
            }
            else
            {
                RenderForPreview(postRenderArgs, invalidateCache: false);
            }
        }

        private void SetViewForCurrentPage(UIState state, PostRenderArgs args)
        {
            if (m_viewMode == DisplayMode.PrintLayout)
            {
                winRSviewer.SetNewPage(new MetaFilePage(CurrentReport.FileManager.Get(CurrentPage), PageSettings));
            }
            else
            {
                if (args.PostActionType == ActionType.Sort || args.PostActionType == ActionType.Toggle)
                {
                    args.PreActionScrollPosition = winRSviewer.ReportPanelAutoScrollPosition;
                }
                winRSviewer.SetNewPage(new GdiPage(CurrentReport.GdiRenderer));
                if (args.IsDifferentReport)
                {
                    rsDocMap.PopulateTree(Report.GetDocumentMap());
                }
                rsDocMap.UpdateTreeForPage(CurrentReport.GdiRenderer.Report.Labels);
            }
            PerformPostRenderAction(args);
            UpdateUIState(state);
        }

        internal bool CanMoveToPage(int page)
        {
            PageCountMode pageCountMode;
            int totalPages = GetTotalPages(out pageCountMode);
            if (pageCountMode != 0)
            {
                return page >= 1;
            }
            if (page <= totalPages)
            {
                return page >= 1;
            }
            return false;
        }

        internal bool CancelAllRenderingRequests()
        {
            Interlocked.Increment(ref m_renderGeneration);
            bool completed = CancelRendering(RenderingCancellationTimeoutMilliseconds);
            if (completed && m_reportHierarchy.Count > 0 && CurrentReport.FileManager.Status == FileManagerStatus.InProgress)
            {
                CurrentReport.FileManager.Status = FileManagerStatus.Aborted;
            }
            CancelAutoRefreshTimer();
            ProcessAsyncInvokes();
            return completed;
        }

        public bool CancelRendering(int millisecondsTimeout)
        {
            return BackgroundThread.Cancel(millisecondsTimeout);
        }

        private void RenderDrillthrough(DrillthroughAction action)
        {
            if (!string.IsNullOrEmpty(action.ReportId))
            {
                string reportPath = null;
                Report report = Report.PerformDrillthrough(action.ReportId, out reportPath);
                LocalReport localReport = null;
                ServerReport serverReport = null;
                if (ProcessingMode == ProcessingMode.Local)
                {
                    serverReport = new ServerReport(ServerReport);
                    localReport = (LocalReport)report;
                }
                else
                {
                    localReport = CreateLocalReport();
                    serverReport = (ServerReport)report;
                }
                DrillthroughEventArgs drillthroughEventArgs = new DrillthroughEventArgs(reportPath, report);
                if (this.Drillthrough != null)
                {
                    this.Drillthrough(this, drillthroughEventArgs);
                }
                if (!drillthroughEventArgs.Cancel)
                {
                    PushReport(localReport, serverReport);
                    RenderReportWithNewParameters(1, null);
                }
            }
        }

        public void PerformBack()
        {
            try
            {
                if (!Report.IsDrillthroughReport)
                {
                    throw new InvalidOperationException(CommonStrings.NotInDrillthrough);
                }
                m_reportHierarchy.Pop();
                rsParams.EnsureParamsLoaded();
                if (m_viewMode == DisplayMode.Normal || CurrentReport.FileManager.Status == FileManagerStatus.Complete)
                {
                    SetViewForCurrentPage(UIState.ProcessingSuccess, new PostRenderArgs(isDifferentReport: true, isPartialRendering: false));
                }
                else
                {
                    RenderForPreview(new PostRenderArgs(isDifferentReport: true, isPartialRendering: false), invalidateCache: false);
                }
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        private void PushReport(LocalReport localReport, ServerReport serverReport)
        {
            m_reportHierarchy.Push(new ReportInfo(localReport, serverReport));
        }

        public void Clear()
        {
            if (!CancelAllRenderingRequests())
            {
                UpdateUIState(new InvalidOperationException("The report rendering operation did not finish after cancellation."));
                return;
            }
            CurrentReport.ClearGdiPage();
            CurrentReport.CurrentPage = 0;
            m_searchState = null;
            rsDocMap.Clear();
            CurrentReport.FileManager.Clean();
            UpdateUIState(UIState.NoReport);
        }

        public void Reset()
        {
            if (!CancelAllRenderingRequests())
            {
                UpdateUIState(new InvalidOperationException("The report rendering operation did not finish after cancellation."));
                return;
            }
            m_reportHierarchy.Clear();
            PushReport(CreateLocalReport(), new ServerReport());
            rsParams.Clear();
            Clear();
        }

        private LocalReport CreateLocalReport()
        {
            return new LocalReport();
        }

        private void OnPrintPreviewPageAvailableUI(int pageNumber, bool isLastPage)
        {
            try
            {
                UIState uIState = (!isLastPage) ? UIState.ProcessingPartial : UIState.ProcessingSuccess;
                if (pageNumber == CurrentPage)
                {
                    SetViewForCurrentPage(uIState, new PostRenderArgs(isDifferentReport: true, isPartialRendering: false));
                }
                else
                {
                    UpdateUIState(uIState);
                }
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        private void OnRenderingComplete(object sender, AsyncCompletedEventArgs args)
        {
            if (m_disposing)
            {
                return;
            }
            AsyncRenderingOperation asyncRenderingOperation = (AsyncRenderingOperation)sender;
            if (asyncRenderingOperation.Generation != Volatile.Read(ref m_renderGeneration))
            {
                return;
            }
            if (m_viewMode == DisplayMode.PrintLayout)
            {
                if (args.Error != null)
                {
                    CurrentReport.FileManager.Status = FileManagerStatus.Aborted;
                }
                else
                {
                    CurrentReport.FileManager.Status = FileManagerStatus.Complete;
                }
            }
            if (args.Error != null)
            {
                if (args.Cancelled)
                {
                    winRSviewer.ShowMessage(LocalizationHelper.Current.ProcessingStopped, enabled: false);
                    UpdateUIState(UIState.ProcessingFailure);
                }
                else
                {
                    UpdateUIState(args.Error);
                }
                CurrentReport.ClearGdiPage();
            }
            else
            {
                try
                {
                    if (m_viewMode == DisplayMode.Normal)
                    {
                        AsyncMainStreamRenderingOperation asyncMainStreamRenderingOperation = (AsyncMainStreamRenderingOperation)sender;
                        int totalPages = Report.GetTotalPages();
                        if (CurrentPage > totalPages)
                        {
                            CurrentReport.CurrentPage = totalPages;
                        }
                        if (asyncMainStreamRenderingOperation.ReportBytes == null || asyncMainStreamRenderingOperation.ReportBytes.LongLength == 0L)
                        {
                            PostRenderArgs postRenderArgs = asyncRenderingOperation.PostRenderArgs;
                            postRenderArgs.PreActionScrollPosition = Point.Empty;
                            RenderRPLPage(CurrentReport.CurrentPage, postRenderArgs);
                            return;
                        }
                        CurrentReport.SetNewGdiPage(asyncMainStreamRenderingOperation.ReportBytes);
                        SetViewForCurrentPage(UIState.ProcessingSuccess, asyncRenderingOperation.PostRenderArgs);
                        StartAutoRefreshTimer(Report.AutoRefreshInterval);
                    }
                    else
                    {
                        int count = CurrentReport.FileManager.Count;
                        if (CurrentPage > count)
                        {
                            CurrentReport.CurrentPage = count;
                        }
                        OnPrintPreviewPageAvailableUI(CurrentReport.FileManager.Count, isLastPage: true);
                    }
                }
                catch (Exception e)
                {
                    UpdateUIState(e);
                }
            }
            NotifyRenderingProgress(asyncRenderingOperation, args);
            if (this.RenderingComplete != null)
            {
                RenderingCompleteEventArgs e2 = new RenderingCompleteEventArgs(asyncRenderingOperation.Warnings, args.Error);
                this.RenderingComplete(this, e2);
            }
        }

        private void PerformPostRenderAction(PostRenderArgs args)
        {
            if (args.PreActionScrollPosition != Point.Empty)
            {
                winRSviewer.ReportPanelAutoScrollPosition = args.PreActionScrollPosition;
            }
            if (args.ActionID != null)
            {
                if (args.PostActionType == ActionType.DocumentMap)
                {
                    winRSviewer.SetFocusPoint(rsDocMap.GetFocusPoint(args.ActionID), WinRSviewer.FocusMode.AlignToTop);
                }
                else if (args.PostActionType == ActionType.BookmarkLink)
                {
                    winRSviewer.SetBookmarkFocusPoint(args.ActionID);
                }
                else if (args.PostActionType == ActionType.Search)
                {
                    winRSviewer.RenderToGraphics(winRSviewer.CreateGraphics(), testMode: false);
                    CurrentReport.GdiRenderer.Search(args.ActionID);
                    List<SearchMatch> searchMatches = CurrentReport.GdiRenderer.Context.SearchMatches;
                    winRSviewer.SetFocusPointMm((searchMatches != null && searchMatches.Count > 0) ? searchMatches[0].Point : new PointF(0f, 0f), WinRSviewer.FocusMode.AvoidScrolling);
                }
                else
                {
                    winRSviewer.SetActionFocusPoint(args.PostActionType, args.ActionID);
                }
            }
            else if (args.PostActionType == ActionType.ScrollToBottomOfPage)
            {
                winRSviewer.SetActionFocusPoint(args.PostActionType, args.ActionID);
            }
        }

        private void ExportDialogClosed(object sender, EventArgs e)
        {
            UpdateUIState(UIState.ProcessingSuccess);
        }

        internal void DisplayErrorMsgBox(Exception ex, string title)
        {
            string text = ex.Message;
            for (ex = ex.InnerException; ex != null; ex = ex.InnerException)
            {
                text = text + "\r\n" + ex.Message;
            }
            MessageBoxWrappers.ShowMessageBox(this, text, title, MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }

        internal void NotifyRenderingProgress(ReportRenderProgress progress)
        {
            if (progress == null || m_disposing)
            {
                return;
            }

            try
            {
                RenderingProgress?.Invoke(this, progress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReportViewer.RenderingProgress: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void NotifyRenderingProgress(AsyncRenderingOperation operation, AsyncCompletedEventArgs args)
        {
            ReportRenderStage stage = args.Cancelled
                ? ReportRenderStage.Cancelled
                : args.Error == null ? ReportRenderStage.Completed : ReportRenderStage.Failed;
            long? bytesRendered = null;
            if (operation is AsyncMainStreamRenderingOperation mainStreamOperation && mainStreamOperation.ReportBytes != null)
            {
                bytesRendered = mainStreamOperation.ReportBytes.LongLength;
            }

            TimeSpan elapsed = DateTime.UtcNow - operation.StartedAtUtc;
            NotifyRenderingProgress(new ReportRenderProgress(
                stage,
                operation.OperationFormat,
                elapsed,
                bytesRendered,
                args.Error,
                args.Error?.Message ?? $"Report rendering {stage.ToString().ToLowerInvariant()}."));
        }

        private AsyncReportOperationWrapper WrapAsyncOperationForUIThreadNotification(AsyncReportOperation operation)
        {
            AsyncReportOperationWrapper asyncReportOperationWrapper = new AsyncReportOperationWrapper(operation);
            asyncReportOperationWrapper.Completed += OnBackgroundThreadCompleted;
            return asyncReportOperationWrapper;
        }

        private void OnBackgroundThreadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            AsyncReportOperationWrapper operationWrapper = (AsyncReportOperationWrapper)sender;
            RegisterAsyncInvoke(delegate
            {
                operationWrapper.EndWrappedOperationExecution(e.Error);
            });
        }

        private void ProcessAsyncInvokes()
        {
            List<MethodInvoker> pendingInvokes;
            lock (m_pendingAsyncInvokes)
            {
                pendingInvokes = new List<MethodInvoker>(m_pendingAsyncInvokes);
                m_pendingAsyncInvokes.Clear();
            }

            foreach (var pendingInvoke in pendingInvokes)
            {
                if (m_disposing || IsDisposed || Disposing)
                {
                    return;
                }

                pendingInvoke();
            }
        }

        private void RegisterAsyncInvoke(MethodInvoker method)
        {
            if (method == null || m_disposing || IsDisposed || Disposing)
            {
                return;
            }

            lock (m_pendingAsyncInvokes)
            {
                if (m_disposing || IsDisposed || Disposing)
                {
                    return;
                }

                m_pendingAsyncInvokes.Enqueue(method);
                if (base.IsHandleCreated)
                {
                    try
                    {
                        BeginInvoke(new MethodInvoker(ProcessAsyncInvokes));
                    }
                    catch (InvalidOperationException)
                    {
                        // The handle can be destroyed between the check and BeginInvoke.
                    }
                }
            }
        }

        private void OnRenderingCompletePrintOnly(object sender, AsyncCompletedEventArgs args)
        {
            if (m_disposing || ((AsyncRenderingOperation)sender).Generation != Volatile.Read(ref m_renderGeneration))
            {
                ClearPendingPrint();
                return;
            }

            if (((AsyncRenderingOperation)sender).PostRenderArgs.IsPartialRendering || args.Error != null)
            {
                CurrentReport.FileManager.Status = FileManagerStatus.Aborted;
            }
            else
            {
                CurrentReport.FileManager.Status = FileManagerStatus.Complete;
            }

			NotifyRenderingProgress((AsyncRenderingOperation)sender, args);

            PrinterSettings printerSettings = m_pendingPrintPrinterSettings;
            PageSettings pageSettings = m_pendingPrintPageSettings;
            ClearPendingPrint();
            if (args.Error != null || printerSettings == null || pageSettings == null)
            {
                return;
            }

            try
            {
                PrintPreparedPages(printerSettings, pageSettings);
                PrintCompleted?.Invoke(this, new ReportPrintCompletedEventArgs(printerSettings, pageSettings));
            }
            catch (Exception exception)
            {
                UpdateUIState(exception);
            }
        }

        private void OnAsyncLoadCompleted(object sender, AsyncCompletedEventArgs args)
        {
            if (m_disposing || ((AsyncLoadOperation)sender).Generation != Volatile.Read(ref m_renderGeneration))
            {
                return;
            }

            if (args.Error != null)
            {
                UpdateUIState(args.Error);
                return;
            }
            try
            {
                RefreshReport();
            }
            catch (Exception e)
            {
                OnError(e);
            }
        }

        internal void LoadAndRefreshReportAsync(Stream reportDefinition)
        {
            Clear();
            AsyncLoadOperation asyncLoadOperation = new AsyncLoadOperation(Report, reportDefinition);
            asyncLoadOperation.Generation = Interlocked.Increment(ref m_renderGeneration);
            asyncLoadOperation.Completed += OnAsyncLoadCompleted;
            AsyncReportOperationWrapper operation = WrapAsyncOperationForUIThreadNotification(asyncLoadOperation);
            UpdateUIState(UIState.LongRunningAction);
            BackgroundThread.BeginBackgroundOperation(operation);
        }

        public void RefreshReport()
        {
            RefreshReport(1, null);
        }

        private void RefreshReport(int targetPage, PostRenderArgs postRenderArgs)
        {
            try
            {
                Clear();
                Report.Refresh();
                RenderReportWithNewParameters(targetPage, postRenderArgs);
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        public int Find(string searchString, int startPage)
        {
            int defaultEndPageForStartPage = GetDefaultEndPageForStartPage(startPage);
            m_searchState = null;
            return Find(searchString, startPage, defaultEndPageForStartPage);
        }

        private int Find(string searchString, int startPage, int endPage)
        {
            if (!ReportViewerStatus.DoesStateAllowSearch(this, m_lastUIState))
            {
                throw new InvalidOperationException();
            }
            if (CurrentReport.GdiRenderer != null)
            {
                CurrentReport.GdiRenderer.ClearSearchResults();
            }
            int num = (m_processingMode != 0) ? CurrentReport.ServerReport.PerformSearch(searchString, startPage, endPage) : CurrentReport.LocalReport.PerformSearch(searchString, startPage, endPage);
            if (num != 0)
            {
                if (m_searchState == null)
                {
                    m_searchState = new SearchState(searchString, startPage);
                }
                if (num != CurrentPage || CurrentReport.GdiRenderer == null)
                {
                    SearchState searchState = m_searchState;
                    SetCurrentPage(num, ActionType.Search, searchString);
                    m_searchState = searchState;
                }
                else
                {
                    SetViewForCurrentPage(UIState.ProcessingSuccess, new PostRenderArgs(ActionType.Search, searchString, winRSviewer.ReportPanelAutoScrollPosition));
                }
            }
            return num;
        }

        private int GetDefaultEndPageForStartPage(int startPage)
        {
            if (startPage == 1)
            {
                PageCountMode pageCountMode;
                int totalPages = GetTotalPages(out pageCountMode);
                if (pageCountMode != 0)
                {
                    return int.MaxValue;
                }
                return totalPages;
            }
            return startPage - 1;
        }

        public int FindNext()
        {
            if (CurrentReport.GdiRenderer == null || m_searchState == null)
            {
                throw new InvalidOperationException(ReportPreviewStrings.NotSearching);
            }
            if (CurrentReport.GdiRenderer.FindNext())
            {
                SetViewForCurrentPage(UIState.ProcessingSuccess, new PostRenderArgs(isDifferentReport: false, isPartialRendering: false, winRSviewer.ReportPanelAutoScrollPosition));
                winRSviewer.SetFocusPointMm(CurrentReport.GdiRenderer.Context.SearchMatches[CurrentReport.GdiRenderer.Context.SearchMatchIndex].Point, WinRSviewer.FocusMode.AvoidScrolling);
                return CurrentPage;
            }
            int defaultEndPageForStartPage = GetDefaultEndPageForStartPage(m_searchState.StartPage);
            int num = 0;
            if (CurrentPage != defaultEndPageForStartPage)
            {
                PageCountMode pageCountMode;
                int totalPages = GetTotalPages(out pageCountMode);
                num = Find(startPage: (CurrentPage == totalPages && pageCountMode == PageCountMode.Actual) ? 1 : (CurrentPage + 1), searchString: CurrentReport.GdiRenderer.Context.SearchText, endPage: defaultEndPageForStartPage);
            }
            if (num == 0)
            {
                m_searchState = null;
                UpdateUIState(m_lastUIState);
            }
            return num;
        }

        public void JumpToBookmark(string bookmarkId)
        {
            string uniqueName;
            int num = Report.PerformBookmarkNavigation(bookmarkId, out uniqueName);
            if (num > 0)
            {
                SetCurrentPage(num, ActionType.BookmarkLink, uniqueName);
            }
        }

        public void JumpToDocumentMapId(string documentMapId)
        {
            int num = Report.PerformDocumentMapNavigation(documentMapId);
            if (num > 0)
            {
                SetCurrentPage(num, ActionType.DocumentMap, documentMapId);
            }
        }

        private void RenderReportWithNewParameters(int pageNumber, PostRenderArgs postRenderArgs)
        {
            try
            {
                CurrentReport.CurrentPage = pageNumber;
                rsParams.EnsureParamsLoaded();
                if (Report.PrepareForRender())
                {
                    if (postRenderArgs == null)
                    {
                        postRenderArgs = new PostRenderArgs(isDifferentReport: true, isPartialRendering: false);
                    }
                    RenderForPreview(postRenderArgs, invalidateCache: true);
                    return;
                }
                if (!Report.IsReadyForConnection)
                {
                    throw new MissingReportSourceException();
                }
                if (!ParametersAreaSupported || (!ShowPromptAreaButton && PromptAreaCollapsed))
                {
                    rsParams.ValidateReportInputsSatisfied();
                    if (ProcessingMode == ProcessingMode.Local)
                    {
                        foreach (string dataSourceName in LocalReport.GetDataSourceNames())
                        {
                            if (LocalReport.DataSources[dataSourceName] == null)
                            {
                                throw new MissingDataSourceException(dataSourceName);
                            }
                        }
                    }
                    throw new Exception(CommonStrings.ReportNotReadyException);
                }
                UpdateUIState(UIState.NoReport);
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        public PrinterSettings GetPrintDialog(PrintDialog pd)
        {
            if (pd.ShowDialog() == DialogResult.OK)
            {
                return pd.PrinterSettings;
            }
            return PrinterSettings;

        }
        public DialogResult PrintDialog()
        {
            return PrintDialog(CreateDefaultPrintSettings());
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PrintSettingFilePath
        {
            get => string.IsNullOrWhiteSpace(m_printSettingFilePath)
                ? GetDefaultPrintSettingFilePathForCurrentReport()
                : m_printSettingFilePath;
            set => m_printSettingFilePath = value;
        }

        public static string GetDefaultPrintSettingFilePath(string reportName)
        {
            var safeReportName = GetSafePrintSettingReportName(reportName);
            return Path.Combine(AppContext.BaseDirectory, "RdlcPrintSetting", $"{safeReportName}.json");
        }

        public static string GetDefaultPreferencesFilePath()
        {
            string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData))
            {
                localApplicationData = AppContext.BaseDirectory;
            }

            return Path.Combine(localApplicationData, "FrontLook", "RdlcViewer", "ViewerPreferences.json");
        }

        public ReportViewerPreferences CapturePreferences()
        {
            return new ReportViewerPreferences
            {
                Theme = GetThemeKind(),
                ZoomMode = ZoomMode,
                ZoomPercent = ZoomPercent,
                ShowToolBar = ShowToolBar,
                ShowStatusBar = ShowStatusBar,
                ShowProgress = ShowProgress,
                ShowContextMenu = ShowContextMenu,
                ShowParameterPrompts = ShowParameterPrompts,
                ShowCredentialPrompts = ShowCredentialPrompts,
                PromptAreaCollapsed = PromptAreaCollapsed,
                DocumentMapCollapsed = DocumentMapCollapsed,
                DocumentMapWidth = DocumentMapWidth,
                IsDocumentMapWidthFixed = IsDocumentMapWidthFixed,
                ShowDocumentMapButton = ShowDocumentMapButton,
                ShowPromptAreaButton = ShowPromptAreaButton,
                ShowPageNavigationControls = ShowPageNavigationControls,
                ShowBackButton = ShowBackButton,
                ShowStopButton = ShowStopButton,
                ShowRefreshButton = ShowRefreshButton,
                ShowPrintButton = ShowPrintButton,
                ShowExportButton = ShowExportButton,
                ShowZoomControl = ShowZoomControl,
                ShowFindControls = ShowFindControls
            };
        }

        public void ApplyPreferences(ReportViewerPreferences preferences)
        {
            if (preferences == null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            bool previousSuppression = m_preferencePersistenceSuppressed;
            m_preferencePersistenceSuppressed = true;
            try
            {
                Theme = preferences.Theme switch
                {
                    ReportViewerThemeKind.Dark => ReportViewerTheme.Dark,
                    ReportViewerThemeKind.HighContrast => ReportViewerTheme.HighContrast,
                    _ => ReportViewerTheme.Light
                };
                ZoomPercent = Math.Max(10, Math.Min(400, preferences.ZoomPercent));
                ZoomMode = preferences.ZoomMode;
                ShowToolBar = preferences.ShowToolBar;
                ShowStatusBar = preferences.ShowStatusBar;
                ShowProgress = preferences.ShowProgress;
                ShowContextMenu = preferences.ShowContextMenu;
                ShowParameterPrompts = preferences.ShowParameterPrompts;
                ShowCredentialPrompts = preferences.ShowCredentialPrompts;
                PromptAreaCollapsed = preferences.PromptAreaCollapsed;
                DocumentMapCollapsed = preferences.DocumentMapCollapsed;
                if (preferences.DocumentMapWidth > 0)
                {
                    DocumentMapWidth = preferences.DocumentMapWidth;
                }
                IsDocumentMapWidthFixed = preferences.IsDocumentMapWidthFixed;
                ShowDocumentMapButton = preferences.ShowDocumentMapButton;
                ShowPromptAreaButton = preferences.ShowPromptAreaButton;
                ShowPageNavigationControls = preferences.ShowPageNavigationControls;
                ShowBackButton = preferences.ShowBackButton;
                ShowStopButton = preferences.ShowStopButton;
                ShowRefreshButton = preferences.ShowRefreshButton;
                ShowPrintButton = preferences.ShowPrintButton;
                ShowExportButton = preferences.ShowExportButton;
                ShowZoomControl = preferences.ShowZoomControl;
                ShowFindControls = preferences.ShowFindControls;
            }
            finally
            {
                m_preferencePersistenceSuppressed = previousSuppression;
            }
        }

        public void SavePreferences()
        {
            WriteTextFileAtomically(PreferencesFilePath, CapturePreferences().ToJson());
        }

        public bool LoadPreferences()
        {
            if (!File.Exists(PreferencesFilePath))
            {
                return false;
            }

            ApplyPreferences(ReportViewerPreferences.Read(PreferencesFilePath));
            return true;
        }

        private ReportViewerThemeKind GetThemeKind()
        {
            if (ReferenceEquals(Theme, ReportViewerTheme.Dark))
            {
                return ReportViewerThemeKind.Dark;
            }
            if (ReferenceEquals(Theme, ReportViewerTheme.HighContrast))
            {
                return ReportViewerThemeKind.HighContrast;
            }

            return ReportViewerThemeKind.Light;
        }

        private string GetDefaultPrintSettingFilePathForCurrentReport()
        {
            string reportName = null;
            try
            {
                reportName = Report.DisplayNameForUse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ReportViewer.GetDefaultPrintSettingFilePath: {ex.GetType().Name} - {ex.Message}");
            }

            return GetDefaultPrintSettingFilePath(reportName);
        }

        private static string GetSafePrintSettingReportName(string reportName)
        {
            var name = Path.GetFileNameWithoutExtension(reportName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Report";
            }

            var invalidCharacters = Path.GetInvalidFileNameChars();
            var safeName = new StringBuilder(name.Length);
            foreach (var character in name)
            {
                safeName.Append(Array.IndexOf(invalidCharacters, character) >= 0 ? '_' : character);
            }

            return string.IsNullOrWhiteSpace(safeName.ToString()) ? "Report" : safeName.ToString();
        }
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CustomPrintDialog CustomPrintDialog { get; set; }

        // save print settings for next time
        public void SavePrintSetting()
        {
            if (string.IsNullOrWhiteSpace(PrintSettingFilePath) || CustomPrintDialog == null)
            {
                return;
            }

            SavePrintSettingsToFile(CustomPrintDialog);
        }

        /// <summary>
        /// Loads printer settings from file or creates default settings.
        /// </summary>
        private CustomPrintDialog LoadPrinterSettingsFromFile()
        {
            if (File.Exists(PrintSettingFilePath))
            {
                try
                {
                    var printSettingsTxt = File.ReadAllText(PrintSettingFilePath);
                    var customDialog = printSettingsTxt.FL_CastToClass<CustomPrintDialog>();
                    if (customDialog != null)
                    {
                        return customDialog;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"LoadPrinterSettingsFromFile: Failed to load from file: {ex.GetType().Name} - {ex.Message}");
                }
            }

            if (CustomPrintDialog != null)
            {
                return CustomPrintDialog;
            }

            // Fallback to default settings
            var pageSetting = GetPageSettings();
            return new CustomPrintDialog(PrinterSettings, pageSetting);
        }

        /// <summary>
        /// Applies page settings to a print dialog's default page settings.
        /// </summary>
        private void ApplyPageSettingsToPrintDialog(PrintDialog printDialog, PageSettings pageSettings)
        {
            if (printDialog == null)
                throw new ArgumentNullException(nameof(printDialog));
            if (pageSettings == null)
                throw new ArgumentNullException(nameof(pageSettings));

            var defaultSettings = printDialog.PrinterSettings.DefaultPageSettings;
            defaultSettings.PaperSize = pageSettings.PaperSize;
            defaultSettings.Landscape = pageSettings.Landscape;
            defaultSettings.Margins = pageSettings.Margins;
            defaultSettings.Color = pageSettings.Color;
            defaultSettings.PaperSource = pageSettings.PaperSource;
            defaultSettings.PrinterResolution = pageSettings.PrinterResolution;
        }

        /// <summary>
        /// Saves the current print settings to the configured file path.
        /// </summary>
        private void SavePrintSettingsToFile(CustomPrintDialog printDialog)
        {
            if (printDialog == null)
                throw new ArgumentNullException(nameof(printDialog));

            if (!string.IsNullOrEmpty(PrintSettingFilePath))
            {
                try
                {
                    var json = printDialog.FL_CastToJson();
                    WriteTextFileAtomically(PrintSettingFilePath, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SavePrintSettingsToFile: Failed to save: {ex.GetType().Name} - {ex.Message}");
                    throw; // Re-throw to let caller handle
                }
            }
        }

        private static void WriteTextFileAtomically(string filePath, string content)
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("The settings path must contain a file name.", nameof(filePath));
            }

            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                File.WriteAllText(temporaryPath, content, Encoding.UTF8);
                File.Move(temporaryPath, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        /// <summary>
        /// Handles the successful path of printer and page settings configuration.
        /// </summary>
        private void HandleSuccessfulSettingsConfiguration(PrinterSettings printerSettings, CustomPrintDialog printDialog)
        {
            PrinterSettings = printerSettings;
            // Keep the in-memory settings in sync with the settings written to
            // disk. The PrintLayout renderer prefers CustomPrintDialog, so
            // leaving the previous instance here makes it render with stale
            // margins after Printer & Page Settings is accepted.
            CustomPrintDialog = printDialog;

            try
            {
                SavePrintSettingsToFile(printDialog);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HandleSuccessfulSettingsConfiguration: Could not save settings: {ex.Message}");
                // Continue even if save fails
            }

            RefreshReport();
        }

        /// <summary>
        /// Handles the error path when initial printer settings configuration fails.
        /// </summary>
        private void HandleSettingsConfigurationError()
        {
            CustomPrintDialog loadedDialog = LoadPrinterSettingsFromFile();

            using (var pd = loadedDialog?.GetPrintDialogSettings().CreatePrintDialog())
            {
                if (pd != null && loadedDialog != null)
                {
                    var setUpPgSetting = loadedDialog.CPageSettings?.GetPageSettings();
                    if (setUpPgSetting != null)
                    {
                        ApplyPageSettingsToPrintDialog(pd, setUpPgSetting);
                    }

                    if (pd.ShowDialog() == DialogResult.OK)
                    {
                        PrinterSettings = pd.PrinterSettings;
                        PageSetupDialog();

                        CustomPrintDialog = new CustomPrintDialog(pd, CurrentReportPageSetting);
                        SavePrintSetting();
                        RefreshReport();
                    }
                }
            }
        }

        /// <summary>
        /// Sets up printer and page settings using configuration file.
        /// This method orchestrates the printer configuration workflow.
        /// </summary>
        public void SetPrinterAndPageSettings()
        {
            try
            {
                var pageSetting = GetPageSettings();
                var printSettings = new CustomPrintDialog(PrinterSettings, pageSetting);

                PrinterSettings configuredSettings;

                using (var pd = printSettings.GetPrintDialogSettings().CreatePrintDialog())
                {
                    if (pd == null)
                    {
                        throw new InvalidOperationException("Failed to create print dialog");
                    }

                    ApplyPageSettingsToPrintDialog(pd, pageSetting);
                    configuredSettings = GetPrintDialog(pd);
                }

                var updatedDialog = new PrintDialog { PrinterSettings = configuredSettings };
                var dialogResult = PageSetupDialog();

                if (dialogResult == DialogResult.OK)
                {
                    var finalPrintSettings = new CustomPrintDialog(updatedDialog, CurrentReportPageSetting);
                    // PageSetupDialog keeps the exact metric values in the
                    // viewer's CustomPrintDialog. Preserve those values while
                    // replacing the printer-selection fields from the outer
                    // printer dialog.
                    if (CustomPrintDialog?.CPageSettings != null)
                    {
                        finalPrintSettings.CPageSettings = CustomPrintDialog.CPageSettings.Clone();
                        finalPrintSettings.PaperSize = finalPrintSettings.CPageSettings.PaperSize ?? finalPrintSettings.PaperSize;
                        finalPrintSettings.Landscape = finalPrintSettings.CPageSettings.Landscape;
                    }
                    HandleSuccessfulSettingsConfiguration(configuredSettings, finalPrintSettings);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetPrinterAndPageSettings: Error in main flow: {ex.GetType().Name} - {ex.Message}\\n{ex.StackTrace}");
                HandleSettingsConfigurationError();
            }
        }

        //Check For Error in DirectPrint by FrontLook
        public void DPrint()
        {
            if (string.IsNullOrWhiteSpace(PrintSettingFilePath) || !File.Exists(PrintSettingFilePath))
            {
                DefaultPrintMode();
                return;
            }

            var printSettingsTxt = File.ReadAllText(PrintSettingFilePath);
            var printSettings = printSettingsTxt.FL_CastToClass<CustomPrintDialog>();
            if (printSettings == null)
            {
                DefaultPrintMode();
                return;
            }

            var printerSettings = printSettings.GetPrinterSettings();

            if (OnPrintingBegin(this, printerSettings))
            {
                var pageSettings = printSettings.CPageSettings?.GetPageSettings() ?? GetPageSettings();
                if (printSettings.CPageSettings != null)
                {
                    SetPageSettings(pageSettings);
                }

                QueuePrintAfterRender(printerSettings, pageSettings);
                if (!RenderPrintPages(printSettings.CPageSettings, printerSettings))
                {
                    ClearPendingPrint();
                }

                // Clear the cached print-only pages.
                winRSviewer.SetNewPage(null);
            }
        }

        private bool RenderPrintPages(CustomPageSetting customPageSettings, PrinterSettings printerSettings)
        {
            if (CurrentReport.FileManager.Status == FileManagerStatus.InProgress && !CancelAllRenderingRequests())
            {
                UpdateUIState(new InvalidOperationException("The report rendering operation did not finish before printing."));
                return false;
            }

            // Print rendering must not reuse preview pages. The page cache also
            // carries the previous report device information, including margins.
            CurrentReport.FileManager.Clean();

            bool allPages = printerSettings.PrintRange == PrintRange.AllPages;
            int startPage = allPages ? 0 : 1;
            int endPage = allPages ? 0 : printerSettings.ToPage;
            string deviceInfo = customPageSettings != null
                ? CreateEMFDeviceInfo(customPageSettings, startPage, endPage)
                : CreateEMFDeviceInfo(startPage, endPage);

            try
            {
                ProcessAsyncInvokes();
                BeginAsyncRender(
                    "IMAGE",
                    allowInternalRenderers: true,
                    deviceInfo,
                    PageCountMode.Estimate,
                    CreateStreamEMFPrintOnly,
                    OnRenderingCompletePrintOnly,
                    new PostRenderArgs(isDifferentReport: false, isPartialRendering: !allPages),
                    requireCompletionOnUIThread: true);
                return true;
            }
            catch
            {
                ClearPendingPrint();
                throw;
            }
        }

        private void QueuePrintAfterRender(PrinterSettings printerSettings, PageSettings pageSettings)
        {
            m_pendingPrintPrinterSettings = printerSettings;
            m_pendingPrintPageSettings = (PageSettings)pageSettings.Clone();
        }

        private void ClearPendingPrint()
        {
            m_pendingPrintPrinterSettings = null;
            m_pendingPrintPageSettings = null;
        }

        private void PrintPreparedPages(PrinterSettings printerSettings, PageSettings pageSettings)
        {
            var printPageSettings = (PageSettings)pageSettings.Clone();
            printPageSettings.PrinterSettings = printerSettings;
            var reportPrintDocument = new ReportPrintDocument(CurrentReport.FileManager, printPageSettings)
            {
                DocumentName = Report.DisplayNameForUse,
                PrinterSettings = printerSettings
            };
            reportPrintDocument.Print();
        }

        private void DefaultPrintMode()
        {
            try
            {
                ReportPrintEventArgs reportPrintEventArgs = new ReportPrintEventArgs(CreateDefaultPrintSettings());
                if (this.Print != null)
                {
                    this.Print(this, reportPrintEventArgs);
                }
                if (!reportPrintEventArgs.Cancel)
                {
                    PrintDialog(reportPrintEventArgs.PrinterSettings);
                }
            }
            catch (Exception e2)
            {
                UpdateUIState(e2);
            }
        }

        public DialogResult PrintDialog(PrinterSettings printerSettings)
        {
            if (!ReportViewerStatus.DoesStateAllowPrinting(m_lastUIState))
            {
                throw new InvalidOperationException();
            }
            DialogResult dialogResult = DialogResult.Cancel;
            using (PrintDialog printDialog = new PrintDialog())
            {
                printDialog.PrinterSettings = printerSettings;
                printDialog.AllowSelection = false;
                printDialog.AllowSomePages = true;
                printDialog.UseEXDialog = true;
                dialogResult = printDialog.ShowDialog(this);
                if (dialogResult == DialogResult.OK)
                {
                    var selectedPrinterSettings = printDialog.PrinterSettings;
                    if (OnPrintingBegin(this, selectedPrinterSettings))
                    {
                        var pageSettings = GetPageSettings();
                        QueuePrintAfterRender(selectedPrinterSettings, pageSettings);
                        if (!RenderPrintPages(null, selectedPrinterSettings))
                        {
                            ClearPendingPrint();
                        }
                        return dialogResult;
                    }
                    return dialogResult;
                }
                return dialogResult;
            }
        }

        public PrinterSettings CreateDefaultPrintSettings()
        {
            PrinterSettings printerSettings = PrinterSettings;
            printerSettings.PrintRange = PrintRange.AllPages;
            printerSettings.MinimumPage = 1;
            printerSettings.FromPage = 1;
            FileManager fileManager = CurrentReport.FileManager;
            if (fileManager.Status == FileManagerStatus.Complete)
            {
                printerSettings.MaximumPage = fileManager.Count;
                printerSettings.ToPage = fileManager.Count;
            }
            else
            {
                printerSettings.ToPage = 1;
            }
            return printerSettings;
        }

        public PageSettings GetPageSettings()
        {
            return ReportViewerUtils.DeepClonePageSettings(PageSettings);
        }

        public void ResetPageSettings()
        {
            ResetAndGetPageSettings();
        }

        private PageSettings ResetAndGetPageSettings()
        {
            PageSettings pageSettings = null;
            try
            {
                m_reportPageSettings = Report.GetDefaultPageSettings();
                pageSettings = m_reportPageSettings.ToPageSettings(PrinterSettings);
            }
            catch (MissingReportSourceException)
            {
                m_reportPageSettings = null;
                pageSettings = null;
            }
            m_reportHierarchy.Peek().PageSettings = pageSettings;
            return pageSettings;
        }

        public void SetPageSettings(PageSettings pageSettings)
        {
            if (pageSettings == null)
            {
                throw new ArgumentNullException("pageSettings");
            }
            pageSettings = ReportViewerUtils.DeepClonePageSettings(pageSettings);
            if (DisplayMode == DisplayMode.PrintLayout)
            {
                try
                {
                    if (!CancelAllRenderingRequests())
                    {
                        UpdateUIState(new InvalidOperationException("The report rendering operation did not finish after cancellation."));
                        return;
                    }
                    CurrentReportPageSetting = pageSettings;
                    RenderForPreview(new PostRenderArgs(isDifferentReport: true, isPartialRendering: false), invalidateCache: true);
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    UpdateUIState(e);
                }
            }
            else
            {
                // Clear the report-default settings source so subsequent print
                // device information comes from the user-selected settings.
                CurrentReportPageSetting = pageSettings;
            }
        }

        private void RenderForPreview(PostRenderArgs postRenderArgs, bool invalidateCache)
        {
            if (this.RenderingBegin != null)
            {
                CancelEventArgs cancelEventArgs = new CancelEventArgs();
                this.RenderingBegin(this, cancelEventArgs);
                if (cancelEventArgs.Cancel)
                {
                    return;
                }
            }
            m_searchState = null;
            if (!CancelAllRenderingRequests())
            {
                UpdateUIState(new InvalidOperationException("The report rendering operation did not finish after cancellation."));
                return;
            }
            if (invalidateCache)
            {
                CurrentReport.FileManager.Clean();
            }
            if (m_viewMode == DisplayMode.PrintLayout && CurrentReport.FileManager.Status == FileManagerStatus.Complete)
            {
                SetViewForCurrentPage(UIState.ProcessingSuccess, postRenderArgs);
                if (this.RenderingComplete != null)
                {
                    this.RenderingComplete(this, new RenderingCompleteEventArgs(null, null));
                }
                return;
            }
            UpdateUIState(UIState.LongRunningAction);
            if (!base.IsHandleCreated)
            {
                CreateHandle();
            }
            if (m_viewMode == DisplayMode.PrintLayout)
            {
                string deviceInfo = CreateEMFDeviceInfo(0, 0);
                BeginAsyncRender("IMAGE", allowInternalRenderers: true, deviceInfo, PageCountMode.Actual, PrintCreateAndRegisterStream, OnRenderingComplete, postRenderArgs, requireCompletionOnUIThread: true);
            }
            else
            {
                RenderRPLPage(CurrentPage, postRenderArgs);
            }
        }

        private void BeginAsyncRender(string format, bool allowInternalRenderers, string deviceInfo, PageCountMode pageCountMode, CreateAndRegisterStream createStreamCallback, AsyncCompletedEventHandler onCompleteCallback, PostRenderArgs postRenderArgs, bool requireCompletionOnUIThread)
        {
            AsyncReportOperation asyncReportOperation = (createStreamCallback != null) ? ((AsyncRenderingOperation)new AsyncAllStreamsRenderingOperation(Report, pageCountMode, format, deviceInfo, allowInternalRenderers, postRenderArgs, createStreamCallback)) : ((AsyncRenderingOperation)new AsyncMainStreamRenderingOperation(Report, pageCountMode, format, deviceInfo, allowInternalRenderers, postRenderArgs));
            asyncReportOperation.Generation = Interlocked.Increment(ref m_renderGeneration);
			asyncReportOperation.StartedAtUtc = DateTime.UtcNow;
			NotifyRenderingProgress(new ReportRenderProgress(ReportRenderStage.Started, format, TimeSpan.Zero, null, null, "Report rendering started."));
			NotifyRenderingProgress(new ReportRenderProgress(ReportRenderStage.Rendering, format, TimeSpan.Zero, null, null, "Report rendering is in progress."));
            asyncReportOperation.Completed += onCompleteCallback;
            if (requireCompletionOnUIThread)
            {
                asyncReportOperation = WrapAsyncOperationForUIThreadNotification(asyncReportOperation);
            }
            BackgroundThread.BeginBackgroundOperation(asyncReportOperation);
        }

        private void RenderRPLPage(int page, PostRenderArgs postRenderArgs)
        {
            string format = "RPL";
            string text = "10.6";
            string format2 = "<DeviceInfo><StartPage>{0}</StartPage><EndPage>{1}</EndPage><MeasureItems>{2}</MeasureItems><SecondaryStreams>{3}</SecondaryStreams><StreamNames>{4}</StreamNames><RPLVersion>{5}</RPLVersion></DeviceInfo>";
            string deviceInfo = string.Format(CultureInfo.InvariantCulture, format2, page, page, true, SecondaryStreams.Embedded, false, text);
            BeginAsyncRender(format, allowInternalRenderers: true, deviceInfo, PageCountMode, null, OnRenderingComplete, postRenderArgs, requireCompletionOnUIThread: true);
        }


        public string CreateEMFDeviceInfo(CustomPageSetting PageSettings, int startPage, int endPage)
        {
            string text = "";
            PageSettings pageSettings = PageSettings.GetPageSettings();
            PageSettings.GetMetricMargins(out var leftMarginMillimeters, out var rightMarginMillimeters, out var topMarginMillimeters, out var bottomMarginMillimeters);
            int hundrethsOfInch = pageSettings.Landscape ? pageSettings.PaperSize.Height : pageSettings.PaperSize.Width;
            int hundrethsOfInch2 = pageSettings.Landscape ? pageSettings.PaperSize.Width : pageSettings.PaperSize.Height;
            return string.Format(CultureInfo.InvariantCulture,
                $@"
                        <DeviceInfo>
                            <OutputFormat>emf</OutputFormat>
                            <StartPage>{startPage}</StartPage>
                            <EndPage>{endPage}</EndPage>
                            <MarginTop>{ToInches(topMarginMillimeters)}</MarginTop>
                            <MarginLeft>{ToInches(leftMarginMillimeters)}</MarginLeft>
                            <MarginRight>{ToInches(rightMarginMillimeters)}</MarginRight>
                            <MarginBottom>{ToInches(bottomMarginMillimeters)}</MarginBottom>
                            <PageHeight>{ToInches(hundrethsOfInch2)}</PageHeight>
                            <PageWidth>{ToInches(hundrethsOfInch)}</PageWidth>
                       </DeviceInfo>"
                       );
        }

        /// <summary>
        /// Creates renderer device information for exports using the configured
        /// paper size and exact metric margins.
        /// </summary>
        public static string CreateExportDeviceInfo(CustomPageSetting pageSetting)
        {
            if (pageSetting == null)
            {
                throw new ArgumentNullException(nameof(pageSetting));
            }

            var pageSettings = pageSetting.GetPageSettings();
            if (pageSettings.PaperSize == null)
            {
                throw new InvalidOperationException("A paper size is required for export.");
            }

            pageSetting.GetMetricMargins(
                out var leftMarginMillimeters,
                out var rightMarginMillimeters,
                out var topMarginMillimeters,
                out var bottomMarginMillimeters);

            int pageWidth = pageSettings.Landscape ? pageSettings.PaperSize.Height : pageSettings.PaperSize.Width;
            int pageHeight = pageSettings.Landscape ? pageSettings.PaperSize.Width : pageSettings.PaperSize.Height;

            return string.Format(
                CultureInfo.InvariantCulture,
                @"<DeviceInfo>
                    <StartPage>0</StartPage>
                    <EndPage>0</EndPage>
                    <MarginTop>{0}</MarginTop>
                    <MarginLeft>{1}</MarginLeft>
                    <MarginRight>{2}</MarginRight>
                    <MarginBottom>{3}</MarginBottom>
                    <PageHeight>{4}</PageHeight>
                    <PageWidth>{5}</PageWidth>
                </DeviceInfo>",
                ToInches(topMarginMillimeters),
                ToInches(leftMarginMillimeters),
                ToInches(rightMarginMillimeters),
                ToInches(bottomMarginMillimeters),
                ToInches(pageHeight),
                ToInches(pageWidth));
        }

        public static string CreateExportDeviceInfo(CustomPrintDialog printSettings)
        {
            if (printSettings == null)
            {
                throw new ArgumentNullException(nameof(printSettings));
            }

            var pageSetting = printSettings.CPageSettings?.Clone() ?? new CustomPageSetting
            {
                Landscape = printSettings.Landscape,
                Margins = new Margins()
            };
            pageSetting.PaperSize ??= printSettings.PaperSize;
            return CreateExportDeviceInfo(pageSetting);
        }

        private string CreateExportDeviceInfo()
        {
            if (CustomPrintDialog != null)
            {
                return CreateExportDeviceInfo(CustomPrintDialog);
            }

            if (m_reportPageSettings != null)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    @"<DeviceInfo>
                        <StartPage>0</StartPage>
                        <EndPage>0</EndPage>
                        <MarginTop>{0}</MarginTop>
                        <MarginLeft>{1}</MarginLeft>
                        <MarginRight>{2}</MarginRight>
                        <MarginBottom>{3}</MarginBottom>
                        <PageHeight>{4}</PageHeight>
                        <PageWidth>{5}</PageWidth>
                    </DeviceInfo>",
                    ToInches(m_reportPageSettings.TopMarginMillimeters),
                    ToInches(m_reportPageSettings.LeftMarginMillimeters),
                    ToInches(m_reportPageSettings.RightMarginMillimeters),
                    ToInches(m_reportPageSettings.BottomMarginMillimeters),
                    ToInches(m_reportPageSettings.PageHeightMillimeters),
                    ToInches(m_reportPageSettings.PageWidthMillimeters));
            }

            var pageSettings = PageSettings;
            if (pageSettings?.PaperSize == null)
            {
                return string.Empty;
            }

            int pageWidth = pageSettings.Landscape ? pageSettings.PaperSize.Height : pageSettings.PaperSize.Width;
            int pageHeight = pageSettings.Landscape ? pageSettings.PaperSize.Width : pageSettings.PaperSize.Height;
            return string.Format(
                CultureInfo.InvariantCulture,
                @"<DeviceInfo>
                    <StartPage>0</StartPage>
                    <EndPage>0</EndPage>
                    <MarginTop>{0}</MarginTop>
                    <MarginLeft>{1}</MarginLeft>
                    <MarginRight>{2}</MarginRight>
                    <MarginBottom>{3}</MarginBottom>
                    <PageHeight>{4}</PageHeight>
                    <PageWidth>{5}</PageWidth>
                </DeviceInfo>",
                ToInches(pageSettings.Margins.Top),
                ToInches(pageSettings.Margins.Left),
                ToInches(pageSettings.Margins.Right),
                ToInches(pageSettings.Margins.Bottom),
                ToInches(pageHeight),
                ToInches(pageWidth));
        }

        private string CreateEMFDeviceInfo(int startPage, int endPage)
        {
            if (CustomPrintDialog?.CPageSettings != null)
            {
                return CreateEMFDeviceInfo(CustomPrintDialog.CPageSettings, startPage, endPage);
            }

            if (m_reportPageSettings != null)
            {
                return CreateEMFDeviceInfo(m_reportPageSettings, startPage, endPage);
            }

            string text = "";
            PageSettings pageSettings = PageSettings;
            int hundrethsOfInch = pageSettings.Landscape ? pageSettings.PaperSize.Height : pageSettings.PaperSize.Width;
            int hundrethsOfInch2 = pageSettings.Landscape ? pageSettings.PaperSize.Width : pageSettings.PaperSize.Height;
            text = string.Format(CultureInfo.InvariantCulture, "<MarginTop>{0}</MarginTop><MarginLeft>{1}</MarginLeft><MarginRight>{2}</MarginRight><MarginBottom>{3}</MarginBottom><PageHeight>{4}</PageHeight><PageWidth>{5}</PageWidth>", ToInches(pageSettings.Margins.Top), ToInches(pageSettings.Margins.Left), ToInches(pageSettings.Margins.Right), ToInches(pageSettings.Margins.Bottom), ToInches(hundrethsOfInch2), ToInches(hundrethsOfInch));
            return string.Format(CultureInfo.InvariantCulture, "<DeviceInfo><OutputFormat>emf</OutputFormat><StartPage>{0}</StartPage><EndPage>{1}</EndPage>{2}</DeviceInfo>", startPage, endPage, text);
        }

        private string CreateEMFDeviceInfo(ReportPageSettings reportPageSettings, int startPage, int endPage)
        {
            return string.Format(CultureInfo.InvariantCulture,
                $@"
                        <DeviceInfo>
                            <OutputFormat>emf</OutputFormat>
                            <StartPage>{startPage}</StartPage>
                            <EndPage>{endPage}</EndPage>
                            <MarginTop>{ToInches(reportPageSettings.TopMarginMillimeters)}</MarginTop>
                            <MarginLeft>{ToInches(reportPageSettings.LeftMarginMillimeters)}</MarginLeft>
                            <MarginRight>{ToInches(reportPageSettings.RightMarginMillimeters)}</MarginRight>
                            <MarginBottom>{ToInches(reportPageSettings.BottomMarginMillimeters)}</MarginBottom>
                            <PageHeight>{ToInches(reportPageSettings.PageHeightMillimeters)}</PageHeight>
                            <PageWidth>{ToInches(reportPageSettings.PageWidthMillimeters)}</PageWidth>
                       </DeviceInfo>");
        }

        private static string ToInches(int hundrethsOfInch)
        {
            return ((double)hundrethsOfInch / 100.0).ToString(CultureInfo.InvariantCulture) + "in";
        }

        private static string ToInches(decimal millimeters)
        {
            if (millimeters == 0m)
            {
                return "0in";
            }

            return (millimeters / 25.4m).ToString(CultureInfo.InvariantCulture) + "in";
        }

        private static string ToInches(double millimeters)
        {
            if (millimeters == 0d)
            {
                return "0in";
            }

            return (millimeters / 25.4d).ToString("R", CultureInfo.InvariantCulture) + "in";
        }

        private Stream CreateStreamEMF(string name, string extension, Encoding encoding, string mimeType, bool useChunking, StreamOper operation)
        {
            if (BackgroundThread.IsCancelInProgress) throw (Exception)Activator.CreateInstance(typeof(System.Threading.ThreadAbortException), true);
            Stream result = CreateStreamEMFPrintOnly(name, extension, encoding, mimeType, useChunking, operation);
            PageCountMode pageCountMode;
            int totalPages = GetTotalPages(out pageCountMode);
            if (totalPages > 0 && base.IsHandleCreated)
            {
                RegisterAsyncInvoke(delegate
                {
                    OnPrintPreviewPageAvailableUI(totalPages, isLastPage: false);
                });
            }
            return result;
        }

        private Stream CreateStreamEMFPrintOnly(string name, string extension, Encoding encoding, string mimeType, bool useChunking, StreamOper operation)
        {
            return CurrentReport.FileManager.CreatePage(operation == StreamOper.CreateAndRegister || operation == StreamOper.CreateForPersistedStreams);
        }

        private void OnLiveReloadOptionsChanged(object sender, EventArgs e)
        {
            if (m_disposing || !IsHandleCreated || IsDisposed)
            {
                return;
            }

            try
            {
                BeginInvoke(new MethodInvoker(ConfigureLiveReload));
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ConfigureLiveReload()
        {
            StopLiveReload();
            if (m_disposing || !m_liveReload.Enabled)
            {
                return;
            }

            try
            {
                if (ProcessingMode != ProcessingMode.Local)
                {
                    throw new InvalidOperationException("Live reload is supported only for LocalReport.");
                }

                if (m_liveReload.ReloadAsync == null)
                {
                    throw new InvalidOperationException("LiveReload.ReloadAsync must be configured.");
                }

                if (GetLiveReloadPaths().Length == 0)
                {
                    throw new InvalidOperationException(
                        "Configure LiveReload.ReportDefinitionPath or LiveReload.DataFilePaths.");
                }

                if (m_liveReload.PollInterval <= TimeSpan.Zero ||
                    m_liveReload.DebounceDelay < TimeSpan.Zero ||
                    m_liveReload.StabilityDelay < TimeSpan.Zero ||
                    m_liveReload.MaxStableReadAttempts <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(LiveReload), "Live reload timing settings are invalid.");
                }

                m_liveReloadCancellation = new CancellationTokenSource();
                m_liveReloadPollTimer = new Timer
                {
                    Interval = ToTimerInterval(m_liveReload.PollInterval)
                };
                m_liveReloadPollTimer.Tick += OnLiveReloadPollTick;
                m_liveReloadPollTimer.Start();

                m_liveReloadDebounceTimer = new Timer
                {
                    Interval = Math.Max(1, ToTimerInterval(m_liveReload.DebounceDelay))
                };
                m_liveReloadDebounceTimer.Tick += OnLiveReloadDebounceTick;

                ConfigureLiveReloadWatchers();
                QueueLiveReloadCheck();
            }
            catch (Exception exception)
            {
                StopLiveReload();
                RaiseLiveReloadError(exception);
            }
        }

        private void ConfigureLiveReloadWatchers()
        {
            foreach (var path in GetLiveReloadPaths())
            {
                var fullPath = Path.GetFullPath(path);
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                {
                    continue;
                }

                var watcher = new FileSystemWatcher(directory, Path.GetFileName(fullPath))
                {
                    NotifyFilter = NotifyFilters.FileName |
                        NotifyFilters.LastWrite |
                        NotifyFilters.Size |
                        NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };
                watcher.Changed += OnLiveReloadFileEvent;
                watcher.Created += OnLiveReloadFileEvent;
                watcher.Deleted += OnLiveReloadFileEvent;
                watcher.Renamed += OnLiveReloadFileRenamed;
                m_liveReloadWatchers.Add(watcher);
            }
        }

        private string[] GetLiveReloadPaths()
        {
            var paths = new List<string>();
            var reportPath = m_liveReload.ReportDefinitionPath;
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                reportPath = LocalReport.ReportPath;
            }

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                paths.Add(reportPath);
            }

            paths.AddRange(m_liveReload.DataFilePaths ?? Array.Empty<string>());
            return paths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private void OnLiveReloadFileEvent(object sender, FileSystemEventArgs e)
        {
            QueueLiveReloadDebounce();
        }

        private void OnLiveReloadFileRenamed(object sender, RenamedEventArgs e)
        {
            QueueLiveReloadDebounce();
        }

        private void QueueLiveReloadDebounce()
        {
            if (m_disposing || m_liveReloadDebounceTimer == null || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    if (m_liveReloadDebounceTimer == null)
                    {
                        return;
                    }

                    m_liveReloadDebounceTimer.Stop();
                    m_liveReloadDebounceTimer.Start();
                }));
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnLiveReloadDebounceTick(object sender, EventArgs e)
        {
            m_liveReloadDebounceTimer?.Stop();
            QueueLiveReloadCheck();
        }

        private void OnLiveReloadPollTick(object sender, EventArgs e)
        {
            QueueLiveReloadCheck();
        }

        private void QueueLiveReloadCheck()
        {
            if (m_disposing || !m_liveReload.Enabled || m_liveReloadCancellation == null)
            {
                return;
            }

            if (m_liveReloadCheckInProgress)
            {
                m_liveReloadCheckQueued = true;
                return;
            }

            m_liveReloadCheckInProgress = true;
            _ = CheckLiveReloadAsync(m_liveReloadCancellation.Token);
        }

        private async Task CheckLiveReloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var fingerprint = await WaitForStableLiveReloadFingerprintAsync(cancellationToken);
                if (m_liveReloadFingerprint == null)
                {
                    m_liveReloadFingerprint = fingerprint;
                    return;
                }

                if (string.Equals(m_liveReloadFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    return;
                }

                var snapshot = await m_liveReload.ReloadAsync(cancellationToken);
                if (snapshot == null)
                {
                    throw new InvalidOperationException("LiveReload.ReloadAsync returned no snapshot.");
                }

                var afterReloadFingerprint = await WaitForStableLiveReloadFingerprintAsync(cancellationToken);
                if (!string.Equals(fingerprint, afterReloadFingerprint, StringComparison.Ordinal))
                {
                    m_liveReloadCheckQueued = true;
                    return;
                }

                if (await ApplyLiveReloadSnapshotAsync(snapshot, cancellationToken))
                {
                    m_liveReloadFingerprint = afterReloadFingerprint;
                    try
                    {
                        LiveReloaded?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine($"ReportViewer.LiveReloaded: {exception.GetType().Name} - {exception.Message}");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                RaiseLiveReloadError(exception);
            }
            finally
            {
                m_liveReloadCheckInProgress = false;
                if (m_liveReloadCheckQueued)
                {
                    m_liveReloadCheckQueued = false;
                    QueueLiveReloadCheck();
                }
            }
        }

        private async Task<string> WaitForStableLiveReloadFingerprintAsync(CancellationToken cancellationToken)
        {
            var fingerprint = await ComputeLiveReloadFingerprintAsync(cancellationToken);
            for (var attempt = 0; attempt < m_liveReload.MaxStableReadAttempts; attempt++)
            {
                if (m_liveReload.StabilityDelay > TimeSpan.Zero)
                {
                    await Task.Delay(m_liveReload.StabilityDelay, cancellationToken);
                }

                var nextFingerprint = await ComputeLiveReloadFingerprintAsync(cancellationToken);
                if (string.Equals(fingerprint, nextFingerprint, StringComparison.Ordinal))
                {
                    return fingerprint;
                }

                fingerprint = nextFingerprint;
            }

            throw new IOException("Report files are still changing; live reload will retry.");
        }

        private Task<string> ComputeLiveReloadFingerprintAsync(CancellationToken cancellationToken)
        {
            var reportPath = m_liveReload.ReportDefinitionPath;
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                reportPath = LocalReport.ReportPath;
            }

            return ReportFileFingerprint.ComputeAsync(
                reportPath,
                m_liveReload.DataFilePaths ?? Array.Empty<string>(),
                cancellationToken,
                m_liveReload.MaxStableReadAttempts).AsTask();
        }

        private async Task<bool> ApplyLiveReloadSnapshotAsync(
            ReportViewerReloadSnapshot snapshot,
            CancellationToken cancellationToken)
        {
            if (ProcessingMode != ProcessingMode.Local)
            {
                throw new InvalidOperationException("Live reload is supported only for LocalReport.");
            }

            if (snapshot.ReportDefinition == null || snapshot.ReportDefinition.Length == 0)
            {
                throw new ArgumentException("The live reload report definition is empty.", nameof(snapshot));
            }

            if (snapshot.DataSources == null)
            {
                throw new ArgumentException("The live reload data sources are missing.", nameof(snapshot));
            }

            if (!CancelAllRenderingRequests())
            {
                throw new InvalidOperationException("The current report rendering operation did not finish before live reload.");
            }

            var candidate = new LocalReport();
            var candidateInfo = new ReportInfo(candidate, new ServerReport());
            ReportInfo previousInfo = null;
            try
            {
                using (var definition = new MemoryStream(snapshot.ReportDefinition, writable: false))
                {
                    candidate.LoadReportDefinition(definition);
                }

                foreach (var dataSource in snapshot.DataSources)
                {
                    if (dataSource == null)
                    {
                        throw new ArgumentException("The live reload data source collection contains null.", nameof(snapshot));
                    }

                    candidate.DataSources.Add(dataSource);
                }

                if (snapshot.Parameters != null && snapshot.Parameters.Count > 0)
                {
                    candidate.SetParameters(snapshot.Parameters);
                }

                previousInfo = m_reportHierarchy.ReplaceTop(candidateInfo);
                var completion = new TaskCompletionSource<RenderingCompleteEventArgs>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                RenderingCompleteEventHandler onRenderingComplete = (sender, args) =>
                    completion.TrySetResult(args);
                ReportErrorEventHandler onReportError = (sender, args) =>
                    completion.TrySetResult(new RenderingCompleteEventArgs(null, args.Exception));
                RenderingComplete += onRenderingComplete;
                ReportError += onReportError;

                try
                {
                    RefreshReport();
                    var result = await completion.Task.WaitAsync(cancellationToken);
                    if (result.Exception != null)
                    {
                        throw result.Exception;
                    }

                    previousInfo.Dispose();
                    previousInfo = null;
                    return true;
                }
                finally
                {
                    RenderingComplete -= onRenderingComplete;
                    ReportError -= onReportError;
                }
            }
            catch
            {
                if (previousInfo != null)
                {
                    if (ReferenceEquals(m_reportHierarchy.Peek(), candidateInfo))
                    {
                        m_reportHierarchy.ReplaceTop(previousInfo);
                    }

                    candidateInfo.Dispose();
                    try
                    {
                        RefreshReport();
                    }
                    catch (Exception restoreException)
                    {
                        Debug.WriteLine($"ReportViewer live reload restore: {restoreException.GetType().Name} - {restoreException.Message}");
                    }
                }
                else
                {
                    candidateInfo.Dispose();
                }

                throw;
            }
        }

        private void RaiseLiveReloadError(Exception exception)
        {
            try
            {
                LiveReloadError?.Invoke(this, new ReportViewerLiveReloadErrorEventArgs(exception));
            }
            catch (Exception eventException)
            {
                Debug.WriteLine($"ReportViewer.LiveReloadError: {eventException.GetType().Name} - {eventException.Message}");
            }
        }

        private void StopLiveReload()
        {
            m_liveReloadCancellation?.Cancel();
            m_liveReloadCancellation?.Dispose();
            m_liveReloadCancellation = null;
            m_liveReloadFingerprint = null;
            m_liveReloadCheckQueued = false;
            m_liveReloadCheckInProgress = false;

            if (m_liveReloadPollTimer != null)
            {
                m_liveReloadPollTimer.Stop();
                m_liveReloadPollTimer.Tick -= OnLiveReloadPollTick;
                m_liveReloadPollTimer.Dispose();
                m_liveReloadPollTimer = null;
            }

            if (m_liveReloadDebounceTimer != null)
            {
                m_liveReloadDebounceTimer.Stop();
                m_liveReloadDebounceTimer.Tick -= OnLiveReloadDebounceTick;
                m_liveReloadDebounceTimer.Dispose();
                m_liveReloadDebounceTimer = null;
            }

            foreach (var watcher in m_liveReloadWatchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Changed -= OnLiveReloadFileEvent;
                watcher.Created -= OnLiveReloadFileEvent;
                watcher.Deleted -= OnLiveReloadFileEvent;
                watcher.Renamed -= OnLiveReloadFileRenamed;
                watcher.Dispose();
            }

            m_liveReloadWatchers.Clear();
        }

        private static int ToTimerInterval(TimeSpan interval)
        {
            var milliseconds = interval.TotalMilliseconds;
            return (int)Math.Clamp(milliseconds, 1, int.MaxValue);
        }

        protected override void Dispose(bool disposing)
        {
            m_disposing = true;
            StopLiveReload();
            ClearPendingPrint();
            if (!CancelAllRenderingRequests())
            {
                // Do not dispose the report hierarchy while the worker can still access it.
                CancelRendering(-1);
            }

            if (disposing)
            {
                m_autoRefreshTimer.Stop();
                m_autoRefreshTimer.Tick -= OnRefresh;
                m_autoRefreshTimer.Dispose();
                m_liveReload.Changed -= OnLiveReloadOptionsChanged;

                if (m_asyncWaitControlTimer != null)
                {
                    m_asyncWaitControlTimer.Stop();
                    m_asyncWaitControlTimer.Tick -= OnWaitPanelTimerTick;
                    m_asyncWaitControlTimer.Dispose();
                    m_asyncWaitControlTimer = null;
                }

                lock (m_pendingAsyncInvokes)
                {
                    m_pendingAsyncInvokes.Clear();
                }
            }

            m_reportHierarchy.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            paramsSplitContainer.SuspendLayout();
            try
            {
                base.OnLayout(e);
                OnPreferredPromptAreaHeightChanged(this, EventArgs.Empty);
            }
            finally
            {
                paramsSplitContainer.ResumeLayout();
            }
        }

        private void OnPreferredPromptAreaHeightChanged(object sender, EventArgs e)
        {
            if (!m_userChangedSplitter)
            {
                int val = paramsSplitContainer.Height - paramsSplitContainer.SplitterWidth - paramsSplitContainer.Panel2MinSize;
                int num = Math.Min(rsParams.PreferredHeight, val);
                if (num >= paramsSplitContainer.Panel1MinSize)
                {
                    paramsSplitContainer.SplitterDistance = num;
                }
            }
        }

        private void OnParamsSplitterMoving(object sender, EventArgs e)
        {
            m_userChangedSplitter = true;
        }

        private void OnPromptAreaCollapse(object sender, EventArgs e)
        {
            PromptAreaCollapsed = paramsSplitContainer.Collapsed;
        }

        private void OnDocumentMapCollapse(object sender, EventArgs e)
        {
            DocumentMapCollapsed = dmSplitContainer.Collapsed;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!base.DesignMode)
            {
                ApplySplitterResources(allResources: true);
                LoadPreferencesAtStartup();
            }
            base.OnLoad(e);
            ConfigureLiveReload();
        }

        private void LoadPreferencesAtStartup()
        {
            if (m_preferencesLoaded)
            {
                return;
            }

            m_preferencesLoaded = true;
            if (!File.Exists(PreferencesFilePath))
            {
                return;
            }

            bool previousSuppression = m_preferencePersistenceSuppressed;
            m_preferencePersistenceSuppressed = true;
            try
            {
                ApplyPreferences(ReportViewerPreferences.Read(PreferencesFilePath));
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ReportViewer.LoadPreferences: {exception.GetType().Name} - {exception.Message}");
            }
            finally
            {
                m_preferencePersistenceSuppressed = previousSuppression;
            }
        }

        private void PersistThemePreference()
        {
            if (m_preferencePersistenceSuppressed || m_disposing)
            {
                return;
            }

            try
            {
                SavePreferences();
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ReportViewer.SavePreferences: {exception.GetType().Name} - {exception.Message}");
            }
        }

        private void ApplySplitterResources(bool allResources)
        {
            if (allResources)
            {
                paramsSplitContainer.Splitter.AccessibleName = ReportPreviewStrings.ShowParamsAccessibleName;
                paramsSplitContainer.Splitter.ButtonAreaAccessibleName = ReportPreviewStrings.ShowParamsHotAreaAccessibleName;
                dmSplitContainer.Splitter.AccessibleName = ReportPreviewStrings.DocMapAccessibleName;
                dmSplitContainer.Splitter.ButtonAreaAccessibleName = ReportPreviewStrings.DocMapHotAreaAccessibleName;
            }
            paramsSplitContainer.ToolTip = LocalizationHelper.Current.ParameterAreaButtonToolTip;
            dmSplitContainer.ToolTip = LocalizationHelper.Current.DocumentMapButtonToolTip;
        }

        public void SetDisplayMode(DisplayMode mode)
        {
            try
            {
                if (!CancelAllRenderingRequests())
                {
                    UpdateUIState(new InvalidOperationException("The report rendering operation did not finish after cancellation."));
                    return;
                }
                m_viewMode = mode;
                ZoomPercent = 100;
                ZoomMode = ((m_viewMode != DisplayMode.PrintLayout) ? ZoomMode.Percent : ZoomMode.FullPage);
                CurrentReport.CurrentPage = 1;
                var storedPrintSettingsApplied = mode == DisplayMode.PrintLayout && ApplyStoredPrintSettingsIfAvailable();
                RenderForPreview(
                    new PostRenderArgs(isDifferentReport: true, isPartialRendering: false),
                    invalidateCache: storedPrintSettingsApplied);
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        private bool ApplyStoredPrintSettingsIfAvailable()
        {
            if (CustomPrintDialog != null || !File.Exists(PrintSettingFilePath))
            {
                return false;
            }

            try
            {
                var storedPrintSettings = LoadPrinterSettingsFromFile();
                if (storedPrintSettings?.CPageSettings == null)
                {
                    return false;
                }

                var pageSettings = storedPrintSettings.GetPageSettings();
                if (pageSettings == null)
                {
                    return false;
                }

                PrinterSettings printerSettings = null;
                try
                {
                    printerSettings = storedPrintSettings.GetPrinterSettings();
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"ApplyStoredPrintSettingsIfAvailable: Could not restore printer settings: {exception.GetType().Name} - {exception.Message}");
                }

                CustomPrintDialog = storedPrintSettings;
                if (printerSettings != null)
                {
                    PrinterSettings = printerSettings;
                }

                // CurrentReportPageSetting clears the report-derived page
                // settings source. The next PrintLayout render therefore uses
                // the persisted custom margins instead of the RDLC margins.
                CurrentReportPageSetting = pageSettings;
                MetricEnabled = true;
                return true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"ApplyStoredPrintSettingsIfAvailable: Failed to apply stored settings: {exception.GetType().Name} - {exception.Message}");
                return false;
            }
        }

        private void OnViewButtonClick(object sender, EventArgs e)
        {
            if (this.ViewButtonClick != null)
            {
                CancelEventArgs cancelEventArgs = new CancelEventArgs();
                this.ViewButtonClick(this, cancelEventArgs);
                if (cancelEventArgs.Cancel)
                {
                    return;
                }
            }
            RenderReportWithNewParameters(1, null);
        }

        public DialogResult ExportDialog(RenderingExtension extension)
        {
            return ExportDialog(extension, null, null);
        }

        public DialogResult ExportDialog(RenderingExtension extension, string deviceInfo)
        {
            return ExportDialog(extension, deviceInfo, null);
        }

        public DialogResult ExportDialog(RenderingExtension extension, string deviceInfo, string fileName)
        {
            if (!ReportViewerStatus.DoesStateAllowExport(m_lastUIState))
            {
                throw new InvalidOperationException();
            }
            if (extension == null)
            {
                throw new ArgumentNullException("extension");
            }
            bool flag = false;
            RenderingExtension[] array = Report.ListRenderingExtensions();
            for (int i = 0; i < array.Length; i++)
            {
                if (string.Equals(array[i].Name, extension.Name, StringComparison.Ordinal))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                throw new ArgumentOutOfRangeException("extension");
            }

            // Export must use the same persisted page settings as PrintLayout.
            // The toolbar normally passes null, but callers may provide the
            // renderer's default device info, which would otherwise reintroduce
            // the RDLC margins and ignore CPageSettings.
            ApplyStoredPrintSettingsIfAvailable();
            if (CustomPrintDialog?.CPageSettings != null)
            {
                deviceInfo = CreateExportDeviceInfo(CustomPrintDialog);
            }
            else if (string.IsNullOrWhiteSpace(deviceInfo))
            {
                deviceInfo = CreateExportDeviceInfo();
            }

            ExportDialog exportDialog = new ExportDialog(this, extension, deviceInfo, fileName);
            exportDialog.Closed += ExportDialogClosed;
            exportDialog.Font = Font;
            return exportDialog.ShowDialog(this);
        }

        public Task<ReportExportResult> ExportToFileAsync(
            RenderingExtension extension,
            string filePath,
            string deviceInfo = null,
            CancellationToken cancellationToken = default,
            IProgress<ReportRenderProgress> progress = null)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            return ExportToFileAsyncCore(extension.Name, filePath, deviceInfo, cancellationToken, progress);
        }

        public Task<ReportExportResult> ExportToFileAsync(
            string format,
            string filePath,
            string deviceInfo = null,
            CancellationToken cancellationToken = default,
            IProgress<ReportRenderProgress> progress = null)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("An export format is required.", nameof(format));
            }

            return ExportToFileAsyncCore(format, filePath, deviceInfo, cancellationToken, progress);
        }

        private async Task<ReportExportResult> ExportToFileAsyncCore(
            string format,
            string filePath,
            string deviceInfo,
            CancellationToken cancellationToken,
            IProgress<ReportRenderProgress> progress)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("An export output path is required.", nameof(filePath));
            }
            if (!ReportViewerStatus.DoesStateAllowExport(m_lastUIState))
            {
                throw new InvalidOperationException("The report is not ready for export.");
            }

            RenderingExtension extension = FindRenderingExtension(format);
            var exportArgs = new ReportExportEventArgs(extension)
            {
                DeviceInfo = deviceInfo
            };
            ReportExport?.Invoke(this, exportArgs);
            if (exportArgs.Cancel)
            {
                throw new OperationCanceledException("The export was cancelled.", cancellationToken);
            }

            ApplyStoredPrintSettingsIfAvailable();
            if (CustomPrintDialog?.CPageSettings != null)
            {
                deviceInfo = CreateExportDeviceInfo(CustomPrintDialog);
            }
            else if (string.IsNullOrWhiteSpace(exportArgs.DeviceInfo))
            {
                deviceInfo = CreateExportDeviceInfo();
            }
            else
            {
                deviceInfo = exportArgs.DeviceInfo;
            }

            string fullPath = Path.GetFullPath(filePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("The export path must include a file name.", nameof(filePath));
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] bytes = await Report.RenderAsync(
                    extension.Name,
                    deviceInfo,
                    PageCountMode.Actual,
                    cancellationToken,
                    progress);
                cancellationToken.ThrowIfCancellationRequested();
                if (bytes == null || bytes.Length == 0)
                {
                    throw new IOException("The export produced an empty file.");
                }

                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, fullPath, overwrite: true);
                return new ReportExportResult(extension.Name, fullPath, bytes.LongLength);
            }
            catch (Exception exception)
            {
                m_lastError = exception;
                OnError(exception);
                throw;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private RenderingExtension FindRenderingExtension(string format)
        {
            RenderingExtension[] extensions = Report.ListRenderingExtensions();
            if (extensions != null)
            {
                foreach (RenderingExtension extension in extensions)
                {
                    if (string.Equals(extension.Name, format, StringComparison.OrdinalIgnoreCase))
                    {
                        return extension;
                    }
                }
            }

            throw new ArgumentException($"The report does not support export format '{format}'.", nameof(format));
        }

        private void OnPageSetup(object sender, EventArgs e)
        {
            PageSetupDialog();
        }


        public DialogResult PageSetupDialog()
        {
            if (!PrinterSettings.IsValid)
            {
                DisplayErrorMsgBox(new InvalidPrinterException(new PrinterSettings()), LocalizationHelper.Current.MessageBoxTitle);
                return DialogResult.Abort;
            }
            var previousPageSettings = (PageSettings)PageSettings.Clone();
            var storedPrintSettings = LoadPrinterSettingsFromFile();
            var customPageSetting = storedPrintSettings?.CPageSettings?.Clone()
                ?? (storedPrintSettings == null
                    ? new CustomPageSetting(previousPageSettings)
                    : new CustomPageSetting(storedPrintSettings.GetPageSettings()));
            var printerSettings = PrinterSettings;

            try
            {
                if (storedPrintSettings != null)
                {
                    printerSettings = storedPrintSettings.GetPrinterSettings();
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"PageSetupDialog: Could not load stored printer settings: {exception.GetType().Name} - {exception.Message}");
            }

            using var pageSetupDialog = new CustomPageSetupDialog(printerSettings, customPageSetting);
            var result = pageSetupDialog.ShowDialog(this);
            if (result != DialogResult.OK)
            {
                return result;
            }

            var selectedPageSettings = pageSetupDialog.PageSetting.GetPageSettings();
            CurrentReportPageSetting = selectedPageSettings;
            MetricEnabled = true;
            PrinterSettings = pageSetupDialog.PrinterSettings;
            UpdateCustomPrintDialog(pageSetupDialog.PageSetting);

            // Always invalidate and re-render after OK. Exact metric values
            // such as 5.00 mm and 5.01 mm can map to the same GDI margin, but
            // they still produce different renderer device information.
            PageSettingsChanged?.Invoke(this, EventArgs.Empty);
            CurrentReport.FileManager.Clean();
            if (m_viewMode == DisplayMode.PrintLayout)
            {
                RenderForPreview(new PostRenderArgs(isDifferentReport: true, isPartialRendering: false), invalidateCache: false);
            }

            SavePrintSetting();
            return result;
        }

        private void UpdateCustomPrintDialog(CustomPageSetting pageSetting)
        {
            var printDialog = CustomPrintDialog ?? new CustomPrintDialog(PrinterSettings, pageSetting.GetPageSettings());
            printDialog.PrinterName = PrinterSettings.PrinterName;
            printDialog.Copies = PrinterSettings.Copies;
            printDialog.Collate = PrinterSettings.Collate;
            printDialog.PrintRange = PrinterSettings.PrintRange;
            printDialog.PaperSize = pageSetting.PaperSize;
            printDialog.Landscape = pageSetting.Landscape;
            printDialog.CPageSettings = pageSetting.Clone();
            CustomPrintDialog = printDialog;
        }

        private void CancelAutoRefreshTimer()
        {
            m_autoRefreshTimer.Stop();
        }

        private void StartAutoRefreshTimer(int autoRefreshSeconds)
        {
            if (autoRefreshSeconds > 0)
            {
                m_autoRefreshTimer.Interval = autoRefreshSeconds * 1000;
                m_autoRefreshTimer.Start();
            }
        }

        private void SetZoom()
        {
            winRSviewer.SetZoom();
            reportToolBar.SetZoom();
            UpdateUIState(m_lastUIState);
        }

        internal void FireAnAction(Action action, bool shiftKeyDown)
        {
            try
            {
                if (action == null)
                {
                    return;
                }
                bool flag = false;
                if (ActionType.BookmarkLink == action.Type)
                {
                    BookmarkNavigationEventArgs bookmarkNavigationEventArgs = new BookmarkNavigationEventArgs(((BookmarkLinkAction)action).ActionLink);
                    if (this.BookmarkNavigation != null)
                    {
                        this.BookmarkNavigation(this, bookmarkNavigationEventArgs);
                    }
                    if (!bookmarkNavigationEventArgs.Cancel)
                    {
                        string uniqueName;
                        int num = Report.PerformBookmarkNavigation(bookmarkNavigationEventArgs.BookmarkId, out uniqueName);
                        if (num > 0 && (num == CurrentPage || OnPageNavigation(num)))
                        {
                            SetCurrentPage(num, ActionType.BookmarkLink, uniqueName);
                        }
                    }
                }
                else if (ActionType.HyperLink == action.Type)
                {
                    HyperLinkAction hyperLinkAction = (HyperLinkAction)action;
                    HyperlinkEventArgs hyperlinkEventArgs = new HyperlinkEventArgs(hyperLinkAction.Url);
                    if (this.Hyperlink != null)
                    {
                        this.Hyperlink(this, hyperlinkEventArgs);
                    }
                    if (!hyperlinkEventArgs.Cancel)
                    {
                        SpawnHyperLink(hyperLinkAction.Url);
                    }
                }
                else if (ActionType.DrillThrough == action.Type)
                {
                    RenderDrillthrough((DrillthroughAction)action);
                }
                else if (ActionType.Toggle == action.Type)
                {
                    CancelEventArgs cancelEventArgs = new CancelEventArgs();
                    if (this.Toggle != null)
                    {
                        this.Toggle(this, cancelEventArgs);
                    }
                    if (!cancelEventArgs.Cancel)
                    {
                        Report.PerformToggle(action.Id);
                        flag = true;
                        RenderForPreview(new PostRenderArgs(ActionType.Toggle, action.Id), invalidateCache: true);
                    }
                }
                else if (ActionType.Sort == action.Type)
                {
                    SortAction sortAction = (SortAction)action;
                    bool clearSort = (!shiftKeyDown) ? true : false;
                    SortEventArgs sortEventArgs = new SortEventArgs(action.Id, sortAction.Direction, clearSort);
                    if (this.Sort != null)
                    {
                        this.Sort(this, sortEventArgs);
                    }
                    if (!sortEventArgs.Cancel)
                    {
                        string uniqueName2 = null;
                        int num2 = Report.PerformSort(action.Id, sortAction.Direction, clearSort, PageCountMode, out uniqueName2);
                        if (num2 == CurrentPage || OnPageNavigation(num2))
                        {
                            CurrentReport.CurrentPage = num2;
                        }
                        else
                        {
                            int totalPages = Report.GetTotalPages();
                            if (CurrentPage > totalPages)
                            {
                                CurrentReport.CurrentPage = totalPages;
                            }
                        }
                        flag = true;
                        RenderForPreview(new PostRenderArgs(ActionType.Sort, uniqueName2), invalidateCache: true);
                    }
                }
                if (flag)
                {
                    CurrentReport.FileManager.Clean();
                }
            }
            catch (Exception e)
            {
                UpdateUIState(e);
            }
        }

        private void SpawnHyperLink(string url)
        {
            try
            {
                if (url != null && (url.StartsWith(Uri.UriSchemeHttp + Uri.SchemeDelimiter, StringComparison.OrdinalIgnoreCase) || url.StartsWith(Uri.UriSchemeHttps + Uri.SchemeDelimiter, StringComparison.OrdinalIgnoreCase) || url.StartsWith(Uri.UriSchemeMailto + ":", StringComparison.OrdinalIgnoreCase)))
                {
                    Process.Start(new ProcessStartInfo() { FileName = url, UseShellExecute = true });
                }
            }
            catch (Win32Exception ex)
            {
                DisplayErrorMsgBox(ex, LocalizationHelper.Current.HyperlinkErrorTitle);
            }
        }

        internal void UpdateUIState(Exception e)
        {
            if (OnError(e))
            {
                winRSviewer.SetNewPage(null);
            }
            else
            {
                winRSviewer.ShowMessage(e);
            }
            UpdateUIState(UIState.ProcessingFailure);
        }

        private void UpdateUIState(UIState newState)
        {
            if (newState != UIState.ProcessingSuccess)
            {
                CancelAutoRefreshTimer();
            }
            if (newState == UIState.NoReport)
            {
                winRSviewer.SetNewPage(null);
            }
            m_status = new ReportViewerStatus(this, newState, m_searchState != null, ParametersAreaSupported, rsParams.HaveContent, rsDocMap.HasDocMap);
            bool flag = rsDocMap.HasDocMap && DisplayMode == DisplayMode.Normal;
            rsDocMap.Visible = flag;
            dmSplitContainer.Panel1Visible = flag;
            dmSplitContainer.CanCollapse = ShowDocumentMapButton;
            dmSplitContainer.Collapsed = DocumentMapCollapsed;
            bool flag2 = rsParams.HaveContent && ParametersAreaSupported;
            rsParams.Visible = flag2;
            paramsSplitContainer.Panel1Visible = flag2;
            paramsSplitContainer.SplitterVisible = ShowPromptAreaButton;
            paramsSplitContainer.Collapsed = PromptAreaCollapsed;
            rsParams.Enabled = (m_status.CanSubmitPromptAreaValues && m_status.IsPromptingSupported);
            UpdateStatusBar(newState);
            OnStatusChanged(this, EventArgs.Empty);
            m_canRenderForWaitControl = (m_lastUIState == UIState.ProcessingSuccess);
            m_lastUIState = newState;
        }

        private void RenderToGraphics(Graphics g)
        {
            winRSviewer.RenderToGraphics(g, testMode: true);
        }

        void IRenderable.RenderToGraphics(Graphics g)
        {
            RenderToGraphics(g);
        }
    }
}
