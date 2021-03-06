﻿//BSD, 2014-present, WinterDev
//ArthurHub, Jose Manuel Menendez Poo

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
using System.Collections.Generic;
using LayoutFarm.Css;
using PixelFarm.Drawing;

namespace LayoutFarm.HtmlBoxes
{

    public abstract partial class HtmlVisualRoot : IDisposable
    {

        protected IHtmlTextService _textService;
        /// <summary>
        /// the root css box of the parsed html
        /// </summary>
        CssBox _rootBox;
        /// <summary>
        /// The actual size of the rendered html (after layout)
        /// </summary>
        float _actualWidth;
        float _actualHeight;
        float _maxWidth;
        float _maxHeight;


        public float MaxWidth => _maxHeight;
        public abstract void ClearPreviousSelection();
        public abstract void SetSelection(SelectionRange selRange);
        public abstract SelectionRange CurrentSelectionRange { get; }
        public abstract void CopySelection(System.Text.StringBuilder stbuilder);

        public HtmlVisualRoot()
        {
        }

        public IHtmlTextService GetHtmlTextService() => _textService;

#if DEBUG
        public static int dbugCount02 = 0;
#endif
        public CssBox RootCssBox => _rootBox;



        public void SetRootCssBox(CssBox rootCssBox)
        {
            if (_rootBox != null)
            {
                _rootBox = null;
                //---------------------------
                this.OnRootDisposed();
            }
            _rootBox = rootCssBox;
            if (rootCssBox != null)
            {
                this.OnRootCreated(_rootBox);
            }
        }

        public abstract bool RefreshDomIfNeed();

        public bool HasRootBox => _rootBox != null;

        /// <summary>
        /// The actual size of the rendered html (after layout)
        /// </summary>
        public SizeF ActualSize => new SizeF(_actualWidth, _actualHeight);

        public float ActualWidth => _actualWidth;

        public float ActualHeight => _actualHeight;

        public void SetMaxSize(float maxWidth, float maxHeight)
        {
            _maxWidth = maxWidth;
            _maxHeight = maxHeight; //init maxHeight = 0
        }

        int _layoutVersion;
        public int LayoutVersion => _layoutVersion;


        public void PerformLayout(LayoutVisitor lay)
        {
            if (_rootBox == null)
            {
                return;
            }
            //----------------------- 
            //reset
            _actualWidth = _actualHeight = 0;
            // if width is not restricted we set it to large value to get the actual later    
            _rootBox.SetLocation(0, 0);
            _rootBox.SetVisualSize(_maxWidth > 0 ? _maxWidth : CssBoxConstConfig.BOX_MAX_WIDTH, 0);
            CssBox.ValidateComputeValues(_rootBox);
            //----------------------- 
            //LayoutVisitor layoutArgs = new LayoutVisitor(this.GraphicsPlatform, this);
            lay.PushContaingBlock(_rootBox);
            //----------------------- 

            _rootBox.PerformLayout(lay);
            if (_maxWidth <= 0.1)
            {
                // in case the width is not restricted we need to double layout,
                //first will find the width so second can layout by it (center alignment)
                _rootBox.SetVisualWidth((int)Math.Ceiling(_actualWidth));
                _actualWidth = _actualHeight = 0;
                _rootBox.PerformLayout(lay);
            }
            lay.PopContainingBlock();
            //----------------------- 
            //TODO: review here again
            FloatingContextStack floatStack = lay.GetFloatingContextStack();
            List<FloatingContext> totalContexts = floatStack.GetTotalContexts();
            int j = totalContexts.Count;
            for (int i = 0; i < j; ++i)
            {
                FloatingContext floatingContext = totalContexts[i];
                int floatBoxCount = floatingContext.FloatBoxCount;
                if (floatBoxCount == 0) { continue; }
                //-----------------------------------------------------------

                CssBox floatingOwner = floatingContext.Owner;
                float rfx, rfy;
                floatingOwner.GetGlobalLocation(out rfx, out rfy);
                CssBox prevParent = null;
                //TODO: review here again
                float extraAdjustX = 0; //temp fixed
                for (int n = 0; n < floatBoxCount; ++n)
                {
                    float bfx, bfy;
                    CssBox box = floatingContext.GetBox(n);
                    box.GetGlobalLocation(out bfx, out bfy);

                    //diff, find relative offset between floating container and its child
                    float nx = bfx - rfx;
                    float ny = bfy - rfy;
                    if (prevParent != null && prevParent != box.ParentBox)
                    {
                        if (n > 0)
                        {
                            CssBox prevFloatChild = floatingContext.GetBox(n - 1);
                            //TODO: review here again
                            //temp fix
                            extraAdjustX = prevFloatChild.ActualMarginRight + box.ActualMarginLeft;
                            ny += box.ActualMarginTop;
                        }
                    }
                    box.SetLocation(nx + extraAdjustX, ny);
                    prevParent = box.ParentBox;
                    floatingOwner.AppendToAbsoluteLayer(box);
                }
            }

            OnLayoutFinished();
            //----------------------- 
            unchecked { _layoutVersion++; }
            //----------------------- 
        }
        //#if DEBUG
        //        void dbugAddToProperContainer(CssBox box)
        //        {
        //            var rectChild = new RectangleF(box.LocalX, box.LocalY,
        //                box.InnerContentWidth,
        //                box.InnerContentHeight);
        //            CssBox parent = box.ParentBox;
        //            bool found = false;
        //            while (parent != null)
        //            {
        //                var rectParent = new RectangleF(0, 0, parent.VisualWidth, parent.VisualHeight);
        //                if (rectParent.Contains(rectChild))
        //                {
        //                    found = true;
        //                    //add to here
        //                    float bfx, bfy;
        //                    box.GetGlobalLocation(out bfx, out bfy);
        //                    float rfx, rfy;
        //                    parent.GetGlobalLocation(out rfx, out rfy);
        //                    //diff
        //                    float nx = bfx - rfx;
        //                    float ny = bfy - rfy;
        //                    box.SetLocation(nx, ny);
        //                    parent.AppendToAbsoluteLayer(box);
        //                    break;
        //                }
        //                else
        //                {
        //                    rectChild.Offset(parent.LocalX, parent.LocalY);
        //                    parent = parent.ParentBox;
        //                }
        //            }
        //            if (!found)
        //            {
        //                //add to root top 
        //                float bfx, bfy;
        //                box.GetGlobalLocation(out bfx, out bfy);
        //                float rfx, rfy;
        //                _rootBox.GetGlobalLocation(out rfx, out rfy);
        //                //diff
        //                float nx = bfx - rfx;
        //                float ny = bfy - rfy;
        //                box.SetLocation(nx, ny);
        //                _rootBox.AppendToAbsoluteLayer(box);
        //            }
        //        }
        //#endif
        public bool IsInUpdateQueue { get; set; }
        protected virtual void OnLayoutFinished()
        {
        }


#if DEBUG
        public static int dbugPaintN;
#endif


        //------------------------------------------------------------------
        protected abstract void OnRequestImage(ImageBinder binder, object reqFrom);

        internal void RaiseImageRequest(
            ImageBinder binder,
            object reqBy,
            bool _sync = false)
        {
            //async by default

            OnRequestImage(binder, reqBy);
        }

        protected abstract void OnRequestScrollView(CssBox box);
        internal void RequestScrollView(CssBox box)
        {
            OnRequestScrollView(box);
        }
        public abstract void ContainerInvalidateGraphics();
        internal void UpdateSizeIfWiderOrHigher(float newWidth, float newHeight)
        {
            if (newWidth > _actualWidth)
            {
                _actualWidth = newWidth;
            }
            if (newHeight > _actualHeight)
            {
                _actualHeight = newHeight;
            }
        }

        protected virtual void OnRootDisposed()
        {
        }
        protected virtual void OnRootCreated(CssBox root)
        {
        }
        protected virtual void OnAllDisposed()
        {
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void Dispose(bool all)
        {
            try
            {
                if (all)
                {
                    this.OnAllDisposed();
                    //RenderError = null;
                    //StylesheetLoadingRequest = null;
                    //ImageLoadingRequest = null;
                }


                if (_rootBox != null)
                {
                    _rootBox = null;
                    this.OnRootDisposed();
                }

                //if (_selectionHandler != null)
                //    _selectionHandler.Dispose();
                //_selectionHandler = null;
            }
            catch
            { }
        }
    }
}