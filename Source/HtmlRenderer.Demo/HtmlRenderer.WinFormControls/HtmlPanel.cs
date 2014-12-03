// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using HtmlRenderer.WebDom;
using HtmlRenderer.Css;
using HtmlRenderer.ContentManagers;
using HtmlRenderer.Composers;

using Conv = LayoutFarm.Drawing.Conv;

namespace HtmlRenderer.Demo
{
    /// <summary>
    /// Provides HTML rendering using the text property.<br/>
    /// WinForms control that will render html content in it's client rectangle.<br/>
    /// If <see cref="AutoScroll"/> is true and the layout of the html resulted in its content beyond the client bounds 
    /// of the panel it will show scrollbars (horizontal/vertical) allowing to scroll the content.<br/>
    /// If <see cref="AutoScroll"/> is false html content outside the client bounds will be clipped.<br/>
    /// The control will handle mouse and keyboard events on it to support html text selection, copy-paste and mouse clicks.<br/>
    /// <para>
    /// The major differential to use HtmlPanel or HtmlLabel is size and scrollbars.<br/>
    /// If the size of the control depends on the html content the HtmlLabel should be used.<br/>
    /// If the size is set by some kind of layout then HtmlPanel is more suitable, also shows scrollbars if the html contents is larger than the control client rectangle.<br/>
    /// </para>
    /// <para>
    /// <h4>AutoScroll:</h4>
    /// Allows showing scrollbars if html content is placed outside the visible boundaries of the panel.
    /// </para>
    /// <para>
    /// <h4>LinkClicked event:</h4>
    /// Raised when the user clicks on a link in the html.<br/>
    /// Allows canceling the execution of the link.
    /// </para>
    /// <para>
    /// <h4>StylesheetLoad event:</h4>
    /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
    /// This event allows to provide the stylesheet manually or provide new source (file or uri) to load from.<br/>
    /// If no alternative data is provided the original source will be used.<br/>
    /// </para>
    /// <para>
    /// <h4>ImageLoad event:</h4>
    /// Raised when an image is about to be loaded by file path or URI.<br/>
    /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
    /// </para>
    /// <para>
    /// <h4>RenderError event:</h4>
    /// Raised when an error occurred during html rendering.<br/>
    /// </para>
    /// </summary>
    public class HtmlPanel : ScrollableControl
    {
        HtmlRenderer.WebDom.WebDocument currentDoc;

        MyHtmlIsland myHtmlIsland;
        HtmlInputEventAdapter _htmlEventBridge;
        /// <summary>
        /// the raw base stylesheet data used in the control
        /// </summary>
        string _baseRawCssData;

        /// <summary>
        /// the base stylesheet data used in the control
        /// </summary>
        WebDom.CssActiveSheet _baseCssData;

        Timer timer01 = new Timer();


        ImageContentManager imageContentMan = new ImageContentManager();
        TextContentManager textContentMan = new TextContentManager();

        LayoutFarm.Drawing.Canvas renderCanvas;
        LayoutFarm.Drawing.GraphicPlatform gfxPlatform;
        /// <summary>
        /// Creates a new HtmlPanel and sets a basic css for it's styling.
        /// </summary>
        public HtmlPanel()
        {
            AutoScroll = true;
            BackColor = SystemColors.Window;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);


            this.gfxPlatform = LayoutFarm.Drawing.CurrentGraphicPlatform.P;
            this.renderCanvas = gfxPlatform.CreateCanvas(0, 0, 0, 0, 800, 600);

            //-------------------------------------------------------
            myHtmlIsland = new MyHtmlIsland();
            myHtmlIsland.BaseStylesheet = HtmlRenderer.Composers.CssParserHelper.ParseStyleSheet(null, true);
            myHtmlIsland.Refresh += OnRefresh;
            myHtmlIsland.NeedUpdateDom += new EventHandler<EventArgs>(myHtmlIsland_NeedUpdateDom);
            myHtmlIsland.RequestResource += new EventHandler<HtmlResourceRequestEventArgs>(myHtmlIsland_RequestResource);
            //myHtmlIsland.ScrollChange += OnScrollChange;
            this.imageContentMan.ImageLoadingRequest += OnImageLoad;
            this.textContentMan.StylesheetLoadingRequest += OnStylesheetLoad;
            //-------------------------------------------------------

            timer01.Interval = 20;//20ms?
            timer01.Tick += (s, e) =>
            {
                if (myHtmlIsland != null)
                {
                    myHtmlIsland.InternalRefreshRequest();
                }
            };

            timer01.Enabled = true;
            //-------------------------------------------
            _htmlEventBridge = new HtmlInputEventAdapter();
            _htmlEventBridge.Bind(myHtmlIsland, gfxPlatform.SampleIFonts);
            //------------------------------------------- 
        }

        void myHtmlIsland_RequestResource(object sender, HtmlResourceRequestEventArgs e)
        {
            this.imageContentMan.AddRequestImage(
                new ImageContentRequest(e.binder, null, this.myHtmlIsland));
        }
        void myHtmlIsland_NeedUpdateDom(object sender, EventArgs e)
        {
            //need updater dom
            var builder = new HtmlRenderer.Composers.RenderTreeBuilder(null);
            builder.RequestStyleSheet += (e2) =>
            {
                var req = new TextLoadRequestEventArgs(e2.Src);
                this.textContentMan.AddStyleSheetRequest(req);
                e2.SetStyleSheet = req.SetStyleSheet;
            };
            var rootBox2 = builder.RefreshCssTree(this.currentDoc, gfxPlatform.SampleIFonts, this.myHtmlIsland);
            this.myHtmlIsland.PerformLayout(gfxPlatform.SampleIGraphics);
        }

        //void RefreshHtmlDomChange()
        //{

        //    var builder = new Composers.RenderTreeBuilder();
        //    builder.RequestStyleSheet += (e) =>
        //    {
        //        var req = new TextLoadRequestEventArgs(e.Src);
        //        this.textContentMan.AddStyleSheetRequest(req);
        //        e.SetStyleSheet = req.SetStyleSheet;
        //    };

        //    var rootBox = builder.RefreshCssTree(this.currentDoc,
        //        LayoutFarm.Drawing.CurrentGraphicPlatform.P.SampleIGraphics,
        //        this.myHtmlIsland);
        //    this.myHtmlIsland.PerformLayout(LayoutFarm.Drawing.CurrentGraphicPlatform.P.SampleIGraphics);
        //}


        ///// <summary>
        ///// Raised when the user clicks on a link in the html.<br/>
        ///// Allows canceling the execution of the link.
        ///// </summary>
        //public event EventHandler<HtmlLinkClickedEventArgs> LinkClicked;

        ///// <summary>
        ///// Raised when an error occurred during html rendering.<br/>
        ///// </summary>
        //public event EventHandler<HtmlRenderErrorEventArgs> RenderError;

        /// <summary>
        /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
        /// This event allows to provide the stylesheet manually or provide new source (file or uri) to load from.<br/>
        /// If no alternative data is provided the original source will be used.<br/>
        /// </summary>
        public event EventHandler<TextLoadRequestEventArgs> StylesheetLoad;

        /// <summary>
        /// Raised when an image is about to be loaded by file path or URI.<br/>
        /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
        /// </summary>
        public event EventHandler<HtmlRenderer.ContentManagers.ImageRequestEventArgs> ImageLoad;

        /// <summary>
        /// Gets or sets a value indicating if anti-aliasing should be avoided for geometry like backgrounds and borders (default - false).
        /// </summary>
        public bool AvoidGeometryAntialias
        {
            get { return myHtmlIsland.AvoidGeometryAntialias; }
            set { myHtmlIsland.AvoidGeometryAntialias = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating if image loading only when visible should be avoided (default - false).<br/>
        /// True - images are loaded as soon as the html is parsed.<br/>
        /// False - images that are not visible because of scroll location are not loaded until they are scrolled to.
        /// </summary>
        /// <remarks>
        /// Images late loading improve performance if the page contains image outside the visible scroll area, especially if there is large 
        /// amount of images, as all image loading is delayed (downloading and loading into memory).<br/>
        /// Late image loading may effect the layout and actual size as image without set size will not have actual size until they are loaded
        /// resulting in layout change during user scroll.<br/>
        /// Early image loading may also effect the layout if image without known size above the current scroll location are loaded as they
        /// will push the html elements down.
        /// </remarks>
        public bool AvoidImagesLateLoading
        {
            get { return myHtmlIsland.AvoidImagesLateLoading; }
            set { myHtmlIsland.AvoidImagesLateLoading = value; }
        }

        /// <summary>
        /// Is content selection is enabled for the rendered html (default - true).<br/>
        /// If set to 'false' the rendered html will be static only with ability to click on links.
        /// </summary>
        [Browsable(true)]
        [DefaultValue(true)]
        [Category("Behavior")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Is content selection is enabled for the rendered html.")]
        public bool IsSelectionEnabled
        {
            get { return myHtmlIsland.IsSelectionEnabled; }
            set { myHtmlIsland.IsSelectionEnabled = value; }
        }

        /// <summary>
        /// Is the build-in context menu enabled and will be shown on mouse right click (default - true)
        /// </summary>
        [Browsable(true)]
        [DefaultValue(true)]
        [Category("Behavior")]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Is the build-in context menu enabled and will be shown on mouse right click.")]
        public bool IsContextMenuEnabled
        {
            get { return myHtmlIsland.IsContextMenuEnabled; }
            set { myHtmlIsland.IsContextMenuEnabled = value; }
        }

        /// <summary>
        /// Set base stylesheet to be used by html rendered in the panel.
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Set base stylesheet to be used by html rendered in the control.")]
        public string BaseStylesheet
        {
            get { return _baseRawCssData; }
            set
            {
                _baseRawCssData = value;
                _baseCssData = HtmlRenderer.Composers.CssParserHelper.ParseStyleSheet(value, true);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the container enables the user to scroll to any controls placed outside of its visible boundaries. 
        /// </summary>
        [Browsable(true)]
        [Description("Sets a value indicating whether the container enables the user to scroll to any controls placed outside of its visible boundaries.")]
        public override bool AutoScroll
        {
            get { return base.AutoScroll; }
            set { base.AutoScroll = value; }
        }

        /// <summary>
        /// Gets or sets the text of this panel
        /// </summary>
        [Browsable(true)]
        [Description("Sets the html of this control.")]
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                if (!IsDisposed)
                {
                    VerticalScroll.Value = VerticalScroll.Minimum;
                    SetHtml(myHtmlIsland, Text, _baseCssData);
                    PerformLayout();
                    Invalidate();
                }
            }
        }
        void SetHtml(MyHtmlIsland htmlIsland, string html, CssActiveSheet cssData)
        {


            var htmldoc = HtmlRenderer.Composers.WebDocumentParser.ParseDocument(new WebDom.Parser.TextSnapshot(html.ToCharArray()));
            var builder = new Composers.RenderTreeBuilder(null);
            builder.RequestStyleSheet += (e) =>
            {
                var req = new TextLoadRequestEventArgs(e.Src);
                this.textContentMan.AddStyleSheetRequest(req);
                e.SetStyleSheet = req.SetStyleSheet;
            };

            //build rootbox from htmldoc
            var rootBox = builder.BuildCssRenderTree(htmldoc,
               gfxPlatform.SampleIGraphics,
                htmlIsland, cssData,
                null);

            htmlIsland.SetHtmlDoc(htmldoc);
            htmlIsland.SetRootCssBox(rootBox, cssData);
        }

        public void LoadHtmlDom(HtmlRenderer.WebDom.WebDocument doc, string defaultCss)
        {
            _baseRawCssData = defaultCss;
            _baseCssData = HtmlRenderer.Composers.CssParserHelper.ParseStyleSheet(defaultCss, true);
            this.currentDoc = doc;

            BuildCssBoxTree(myHtmlIsland, _baseCssData);
            //---------------------
            PerformLayout();
            Invalidate();
        }
        void BuildCssBoxTree(MyHtmlIsland htmlIsland, CssActiveSheet cssData)
        {

            var builder = new Composers.RenderTreeBuilder(null);
            builder.RequestStyleSheet += (e) =>
            {
                var req = new TextLoadRequestEventArgs(e.Src);
                this.textContentMan.AddStyleSheetRequest(req);
                e.SetStyleSheet = req.SetStyleSheet;

            };


            var rootBox = builder.BuildCssRenderTree(this.currentDoc,
                gfxPlatform.SampleIGraphics,
                htmlIsland, cssData,
                null);

            htmlIsland.SetHtmlDoc(this.currentDoc);
            htmlIsland.SetRootCssBox(rootBox, cssData);

        }
        public void ForceRefreshHtmlDomChange(HtmlRenderer.WebDom.WebDocument doc)
        {
            //RefreshHtmlDomChange(_baseCssData);
            myHtmlIsland_NeedUpdateDom(this, EventArgs.Empty);
            this.PaintMe();

        }
        public Boxes.HtmlIsland GetHtmlContainer()
        {
            return this.myHtmlIsland;
        }


        /// <summary>
        /// Get html from the current DOM tree with inline style.
        /// </summary>
        /// <returns>generated html</returns>
        public string GetHtml()
        {
            return myHtmlIsland != null ? myHtmlIsland.GetHtml() : null;
        }


        ///// <summary>
        ///// Adjust the scrollbar of the panel on html element by the given id.<br/>
        ///// The top of the html element rectangle will be at the top of the panel, if there
        ///// is not enough height to scroll to the top the scroll will be at maximum.<br/>
        ///// </summary>
        ///// <param name="elementId">the id of the element to scroll to</param>
        //public void ScrollToElement(string elementId)
        //{
        //    if (_htmlContainer != null)
        //    {
        //        var rect = _htmlContainer.GetElementRectangle(elementId);
        //        if (rect.HasValue)
        //        {
        //            UpdateScroll(Point.Round(rect.Value.Location));
        //            _htmlContainer.HandleMouseMove(this, new MouseEventArgs(MouseButtons, 0, MousePosition.X, MousePosition.Y, 0));
        //        }
        //    }
        //}

        #region Private methods

        /// <summary>
        /// Perform the layout of the html in the control.
        /// </summary>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            PerformHtmlLayout();

            base.OnLayout(levent);

            // to handle if vertical scrollbar is appearing or disappearing
            if (myHtmlIsland != null && Math.Abs(myHtmlIsland.MaxSize.Width - ClientSize.Width) > 0.1)
            {
                PerformHtmlLayout();
                base.OnLayout(levent);
            }
        }

        /// <summary>
        /// Perform html container layout by the current panel client size.
        /// </summary>
        void PerformHtmlLayout()
        {
            if (myHtmlIsland != null)
            {
                myHtmlIsland.MaxSize = new LayoutFarm.Drawing.SizeF(ClientSize.Width, 0);

                //using (var g = CreateGraphics())
                //{
                //    myHtmlIsland.PerformLayout(g);
                //}
                myHtmlIsland.PerformLayout(gfxPlatform.SampleIGraphics);
                var asize = myHtmlIsland.ActualSize;
                AutoScrollMinSize = Size.Round(new SizeF(asize.Width, asize.Height));
            }
        }
        int count01;

        /// <summary>
        /// Perform paint of the html in the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintMe(e);
        }
        void PaintMe()
        {
            PaintMe(null);
        }
        void PaintMe(PaintEventArgs e)
        {
            if (myHtmlIsland != null)
            {

                myHtmlIsland.ScrollOffset = Conv.ToPointF(AutoScrollPosition);
                myHtmlIsland.PhysicalViewportBound = Conv.ToRectF(this.Bounds);



                myHtmlIsland.CheckDocUpdate();
                renderCanvas.ClearSurface();
                myHtmlIsland.PerformPaint(renderCanvas);

                IntPtr hdc = GetDC(this.Handle);
                renderCanvas.RenderTo(hdc, 0, 0, new LayoutFarm.Drawing.Rectangle(0, 0, 800, 600));
                ReleaseDC(this.Handle, hdc);
                // call mouse move to handle paint after scroll or html change affecting mouse cursor.
                //var mp = PointToClient(MousePosition);
                //_htmlContainer.HandleMouseMove(this, new MouseEventArgs(MouseButtons.None, 0, mp.X, mp.Y, 0));
            }

            if (e != null)
            {
                e.Graphics.DrawString(count01.ToString(), this.Font, Brushes.Black, new PointF(0, 0));
            }
            count01++;

        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hdc);

        /// <summary>
        /// Set focus on the control for keyboard scrrollbars handling.
        /// </summary>
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Focus();
        }

        /// <summary>
        /// Handle mouse move to handle hover cursor and text selection. 
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            _htmlEventBridge.MouseMove(CreateMouseEventArg(e));
            PaintMe(null);
        }

        /// <summary>
        /// Handle mouse leave to handle cursor change.
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);             

            //if (_htmlContainer != null)
            //    _htmlContainer.HandleMouseLeave(this);
        }

        /// <summary>
        /// Handle mouse down to handle selection. 
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            this._htmlEventBridge.MouseDown(CreateMouseEventArg(e));
            PaintMe(null);
            //this.Invalidate();
        }

        /// <summary>
        /// Handle mouse up to handle selection and link click. 
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            this._htmlEventBridge.MouseUp(CreateMouseEventArg(e));
            PaintMe(null);
            // this.Invalidate();
        }
        LayoutFarm.UI.UIMouseEventArgs CreateMouseEventArg(MouseEventArgs e)
        {
            var mouseE = new LayoutFarm.UI.UIMouseEventArgs();
            mouseE.SetEventInfo(
                e.X, e.Y,
                GetUIMouseButton(e.Button),
                e.Clicks,
                e.Delta);
            return mouseE;
        }

        static LayoutFarm.UI.UIMouseButtons GetUIMouseButton(MouseButtons mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButtons.Right:
                    return LayoutFarm.UI.UIMouseButtons.Right;
                case MouseButtons.Middle:
                    return LayoutFarm.UI.UIMouseButtons.Middle;
                case MouseButtons.None:
                    return LayoutFarm.UI.UIMouseButtons.None;
                default:
                    return LayoutFarm.UI.UIMouseButtons.Left;
            }
        }
        ///// <summary>
        ///// Handle mouse double click to select word under the mouse. 
        ///// </summary>
        //protected override void OnMouseDoubleClick(MouseEventArgs e)
        //{
        //    base.OnMouseDoubleClick(e);
        //    this._htmlEventBridge.MouseDoubleClick(CreateMouseEventArg(e));

        //    //if (_htmlContainer != null)
        //    //    _htmlContainer.HandleMouseDoubleClick(this, e);
        //}

        /// <summary>
        /// Handle key down event for selection, copy and scrollbars handling.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);



            //if (_htmlContainer != null)
            //    _htmlContainer.HandleKeyDown(this, e);
            //if (e.KeyCode == Keys.Up)
            //{
            //    VerticalScroll.Value = Math.Max(VerticalScroll.Value - 70, VerticalScroll.Minimum);
            //    PerformLayout();
            //}
            //else if (e.KeyCode == Keys.Down)
            //{
            //    VerticalScroll.Value = Math.Min(VerticalScroll.Value + 70, VerticalScroll.Maximum);
            //    PerformLayout();
            //}
            //else if (e.KeyCode == Keys.PageDown)
            //{
            //    VerticalScroll.Value = Math.Min(VerticalScroll.Value + 400, VerticalScroll.Maximum);
            //    PerformLayout();
            //}
            //else if (e.KeyCode == Keys.PageUp)
            //{
            //    VerticalScroll.Value = Math.Max(VerticalScroll.Value - 400, VerticalScroll.Minimum);
            //    PerformLayout();
            //}
            //else if (e.KeyCode == Keys.End)
            //{
            //    VerticalScroll.Value = VerticalScroll.Maximum;
            //    PerformLayout();
            //}
            //else if (e.KeyCode == Keys.Home)
            //{
            //    VerticalScroll.Value = VerticalScroll.Minimum;
            //    PerformLayout();
            //}
        }

        ///// <summary>
        ///// Propagate the LinkClicked event from root container.
        ///// </summary>
        //private void OnLinkClicked(object sender, HtmlLinkClickedEventArgs e)
        //{
        //    if (LinkClicked != null)
        //    {
        //        LinkClicked(this, e);
        //    }
        //}

        ///// <summary>
        ///// Propagate the Render Error event from root container.
        ///// </summary>
        //private void OnRenderError(object sender, HtmlRenderErrorEventArgs e)
        //{
        //    //if (RenderError != null)
        //    //{
        //    //    if (InvokeRequired)
        //    //        Invoke(RenderError, this, e);
        //    //    else
        //    //        RenderError(this, e);
        //    //}
        //}

        /// <summary>
        /// Propagate the stylesheet load event from root container.
        /// </summary>
        private void OnStylesheetLoad(object sender, TextLoadRequestEventArgs e)
        {
            if (StylesheetLoad != null)
            {
                StylesheetLoad(this, e);
            }
        }

        /// <summary>
        /// Propagate the image load event from root container.
        /// </summary>
        private void OnImageLoad(object sender, HtmlRenderer.ContentManagers.ImageRequestEventArgs e)
        {
            if (ImageLoad != null)
            {
                ImageLoad(this, e);
            }
        }

        /// <summary>
        /// Handle html renderer invalidate and re-layout as requested.
        /// </summary>
        private void OnRefresh(object sender, HtmlRefreshEventArgs e)
        {
            if (e.Layout)
            {
                if (InvokeRequired)
                    Invoke(new MethodInvoker(PerformLayout));
                else
                    PerformLayout();
            }
            if (InvokeRequired)
                // Invoke(new MethodInvoker(Invalidate));
                Invoke(new MethodInvoker(this.PaintMe));
            else
                this.PaintMe();
            //Invalidate();
        }

        /// <summary>
        /// On html renderer scroll request adjust the scrolling of the panel to the requested location.
        /// </summary>
        private void OnScrollChange(object sender, HtmlScrollEventArgs e)
        {
            UpdateScroll(e.Location);
        }

        /// <summary>
        /// Adjust the scrolling of the panel to the requested location.
        /// </summary>
        /// <param name="location">the location to adjust the scroll to</param>
        private void UpdateScroll(Point location)
        {
            AutoScrollPosition = location;
            myHtmlIsland.ScrollOffset = Conv.ToPoint(AutoScrollPosition);
        }

        /// <summary>
        /// Used to add arrow keys to the handled keys in <see cref="OnKeyDown"/>.
        /// </summary>
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        /// <summary>
        /// Release the html container resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (myHtmlIsland != null)
            {
                //_htmlContainer.LinkClicked -= OnLinkClicked;
                //myHtmlIsland.RenderError -= OnRenderError;
                myHtmlIsland.Refresh -= OnRefresh;
                // myHtmlIsland.ScrollChange -= OnScrollChange;
                this.textContentMan.StylesheetLoadingRequest -= OnStylesheetLoad;
                this.imageContentMan.ImageLoadingRequest -= OnImageLoad;

                myHtmlIsland.Dispose();
                myHtmlIsland = null;
            }
            base.Dispose(disposing);
        }

        #region Hide not relevant properties from designer

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override Font Font
        {
            get { return base.Font; }
            set { base.Font = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { base.ForeColor = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override bool AllowDrop
        {
            get { return base.AllowDrop; }
            set { base.AllowDrop = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override RightToLeft RightToLeft
        {
            get { return base.RightToLeft; }
            set { base.RightToLeft = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public override Cursor Cursor
        {
            get { return base.Cursor; }
            set { base.Cursor = value; }
        }

        /// <summary>
        /// Not applicable.
        /// </summary>
        [Browsable(false)]
        public new bool UseWaitCursor
        {
            get { return base.UseWaitCursor; }
            set { base.UseWaitCursor = value; }
        }

        #endregion

        #endregion
    }
}